using Forget.Core.Models;
using Forget.PostgreSql.Extensions;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// Covers the rest of the public API surface that <see cref="CrudIntegrationTests"/> doesn't touch —
    /// single/first/page retrieval, range operations, existence checks, aggregates, and the bulk
    /// update-by-predicate/delete-by-predicate overloads. Split into its own class (and its own container) purely
    /// to keep <see cref="CrudIntegrationTests"/> focused on the core round-trip story.
    /// </summary>
    [Collection(PostgreSqlCollection.Name)]
    public class ExtendedIntegrationTests
    {
        private readonly PostgreSqlFixture _fixture;

        public ExtendedIntegrationTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task GetFirstAsync_WithPredicateAndSort_ReturnsTheFirstRowInSortOrder()
        {
            for (int i = 0; i < 3; i++)
                await _fixture.Connection.InsertAsync(new Widget { Id = 200 + i, Name = $"First{i}", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            Widget fetched = await _fixture.Connection.GetFirstAsync<Widget>(w => w.Id >= 200 && w.Id <= 202, sortDescriptors: [new SortDescriptor<Widget>(w => w.Id, SortDirection.Descending)], cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(202, fetched.Id);
        }

        [Fact]
        public async Task GetSingleAsync_WhenSeveralRowsMatch_Throws()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 210, Name = "Twin", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 211, Name = "Twin", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Connection.GetSingleAsync<Widget>(w => w.Name == "Twin", cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task DeleteRangeAsync_ByIds_RemovesEveryRowWithThoseIds()
        {
            await _fixture.Connection.InsertRangeAsync([new Widget { Id = 220, Name = "D220", IsActive = true, Price = 1m }, new Widget { Id = 221, Name = "D221", IsActive = true, Price = 1m }, new Widget { Id = 222, Name = "D222", IsActive = true, Price = 1m }], cancellationToken: TestContext.Current.CancellationToken);

            int affected = await _fixture.Connection.DeleteRangeAsync<Widget>(new[] { 220, 221 }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(2, affected);
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(220, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(221, cancellationToken: TestContext.Current.CancellationToken));
            Assert.NotNull(await _fixture.Connection.GetByIdAsync<Widget>(222, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_WithPredicate_ReturnsNullWhenNothingMatches()
        {
            Widget? fetched = await _fixture.Connection.GetFirstOrDefaultAsync<Widget>(w => w.Id == 999_001, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Null(fetched);
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_WithPredicate_ReturnsAMatch()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 100, Name = "First candidate", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            Widget? fetched = await _fixture.Connection.GetFirstOrDefaultAsync<Widget>(w => w.Id == 100, cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotNull(fetched);
            Assert.Equal("First candidate", fetched!.Name);
        }

        [Fact]
        public async Task GetSingleAsync_WithPredicate_ReturnsTheOnlyMatch()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 101, Name = "Unique", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            Widget fetched = await _fixture.Connection.GetSingleAsync<Widget>(w => w.Id == 101, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("Unique", fetched.Name);
        }

        [Fact]
        public async Task GetSingleOrDefaultAsync_WithPredicate_ReturnsNullWhenNothingMatches()
        {
            Widget? fetched = await _fixture.Connection.GetSingleOrDefaultAsync<Widget>(w => w.Id == 999_002, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Null(fetched);
        }

        [Fact]
        public async Task GetPageAsync_WithSkipAndTake_ReturnsTheRequestedSlice()
        {
            for (int i = 0; i < 5; i++)
                await _fixture.Connection.InsertAsync(new Widget { Id = 110 + i, Name = $"Page{i}", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> page = await _fixture.Connection.GetPageAsync(w => w.Id >= 110 && w.Id < 115, sortDescriptors: [new SortDescriptor<Widget>(w => w.Id)], skip: 1, take: 2, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal([111, 112], page.Select(w => w.Id));
        }

        [Fact]
        public async Task GetByIdRangeAsync_ReturnsRowsForEachRequestedId()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 120, Name = "R120", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 121, Name = "R121", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget?> rows = await _fixture.Connection.GetByIdRangeAsync<Widget>(new[] { 120, 121, 999_003 }, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(2, rows.Count(w => w is not null));
            Assert.Contains(rows, w => w?.Id == 120);
            Assert.Contains(rows, w => w?.Id == 121);
        }

        [Fact]
        public async Task UpdateRangeAsync_UpdatesEveryEntityInTheBatch()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 130, Name = "Old130", IsActive = false, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 131, Name = "Old131", IsActive = false, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            Widget[] updates =
            [
                new() { Id = 130, Name = "New130", IsActive = true, Price = 2m },
                new() { Id = 131, Name = "New131", IsActive = true, Price = 2m },
            ];
            int affected = await _fixture.Connection.UpdateRangeAsync(updates, cancellationToken: TestContext.Current.CancellationToken);

            Widget? w130 = await _fixture.Connection.GetByIdAsync<Widget>(130, cancellationToken: TestContext.Current.CancellationToken);
            Widget? w131 = await _fixture.Connection.GetByIdAsync<Widget>(131, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(2, affected);
            Assert.Equal("New130", w130!.Name);
            Assert.Equal("New131", w131!.Name);
        }

        [Fact]
        public async Task DeleteRangeAsync_RemovesEveryEntityInTheBatch()
        {
            Widget[] toDelete =
            [
                new() { Id = 140, Name = "D140", IsActive = true, Price = 1m },
                new() { Id = 141, Name = "D141", IsActive = true, Price = 1m },
            ];
            await _fixture.Connection.InsertRangeAsync(toDelete, cancellationToken: TestContext.Current.CancellationToken);

            int affected = await _fixture.Connection.DeleteRangeAsync(toDelete, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(2, affected);
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(140, cancellationToken: TestContext.Current.CancellationToken));
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(141, cancellationToken: TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ExistsAsync_WithPredicate_ReflectsWhetherAMatchingRowExists()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 150, Name = "Exists150", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            bool existsTrue = await _fixture.Connection.ExistsAsync<Widget>(w => w.Id == 150, cancellationToken: TestContext.Current.CancellationToken);
            bool existsFalse = await _fixture.Connection.ExistsAsync<Widget>(w => w.Id == 999_004, cancellationToken: TestContext.Current.CancellationToken);

            Assert.True(existsTrue);
            Assert.False(existsFalse);
        }

        [Fact]
        public async Task CountAsync_WithPredicate_CountsOnlyMatchingRows()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 160, Name = "Count160", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 161, Name = "Count161", IsActive = false, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            long activeCount = await _fixture.Connection.CountAsync<Widget>(w => w.IsActive && (w.Id == 160 || w.Id == 161), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, activeCount);
        }

        [Fact]
        public async Task Aggregates_SumAvgMinMax_ComputeOverMatchingRows()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 170, Name = "Agg170", IsActive = true, Price = 10m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 171, Name = "Agg171", IsActive = true, Price = 20m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 172, Name = "Agg172", IsActive = true, Price = 30m }, cancellationToken: TestContext.Current.CancellationToken);

            decimal? sum = await _fixture.Connection.SumAsync<Widget>(w => w.Price, w => w.Id >= 170 && w.Id <= 172, cancellationToken: TestContext.Current.CancellationToken);
            decimal? avg = await _fixture.Connection.AvgAsync<Widget>(w => w.Price, w => w.Id >= 170 && w.Id <= 172, cancellationToken: TestContext.Current.CancellationToken);
            decimal? min = await _fixture.Connection.MinAsync<Widget, decimal>(w => w.Price, w => w.Id >= 170 && w.Id <= 172, cancellationToken: TestContext.Current.CancellationToken);
            decimal? max = await _fixture.Connection.MaxAsync<Widget, decimal>(w => w.Price, w => w.Id >= 170 && w.Id <= 172, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(60m, sum);
            Assert.Equal(20m, avg);
            Assert.Equal(10m, min);
            Assert.Equal(30m, max);
        }

        [Fact]
        public async Task AvgAsync_WithAFractionalResult_ReturnsTheAverageAsADecimal()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 230, Name = "Avg230", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 231, Name = "Avg231", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 232, Name = "Avg232", IsActive = true, Price = 2m }, cancellationToken: TestContext.Current.CancellationToken);

            decimal? avg = await _fixture.Connection.AvgAsync<Widget>(w => w.Price, w => w.Id >= 230 && w.Id <= 232, cancellationToken: TestContext.Current.CancellationToken);

            // 4 / 3: every engine returns its own number of decimals, the value must survive the trip either way.
            Assert.Equal(1.3333m, Math.Round(avg!.Value, 4));
        }
        [Fact]
        public async Task UpdateAsync_WithValuesAndPredicate_UpdatesOnlyMatchingRows()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 180, Name = "Bulk180", IsActive = false, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 181, Name = "Bulk181", IsActive = false, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            int affected = await _fixture.Connection.UpdateAsync<Widget>(new { IsActive = true }, w => w.Id == 180, cancellationToken: TestContext.Current.CancellationToken);

            Widget? w180 = await _fixture.Connection.GetByIdAsync<Widget>(180, cancellationToken: TestContext.Current.CancellationToken);
            Widget? w181 = await _fixture.Connection.GetByIdAsync<Widget>(181, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, affected);
            Assert.True(w180!.IsActive);
            Assert.False(w181!.IsActive);
        }

        [Fact]
        public async Task DeleteAsync_WithPredicate_DeletesOnlyMatchingRows()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 190, Name = "DelPred190", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 191, Name = "DelPred191", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            int affected = await _fixture.Connection.DeleteAsync<Widget>(w => w.Id == 190, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, affected);
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(190, cancellationToken: TestContext.Current.CancellationToken));
            Assert.NotNull(await _fixture.Connection.GetByIdAsync<Widget>(191, cancellationToken: TestContext.Current.CancellationToken));
        }
    }
}
