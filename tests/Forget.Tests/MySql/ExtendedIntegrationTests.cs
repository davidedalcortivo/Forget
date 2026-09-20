using Forget.Core.Models;
using Forget.MySql.Extensions;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// Covers the rest of the public API surface that <see cref="CrudIntegrationTests"/> doesn't touch:
    /// first/single/page retrieval, range operations, existence checks, aggregates, and the bulk
    /// update-by-predicate/delete-by-predicate overloads. Each of these builds SQL that differs from one dialect to
    /// the next (paging, existence, aggregates), so the generated text alone is not enough: it has to run.
    /// <para>
    /// Tests stay isolated on the shared container by using a disjoint <c>Id</c> range each.
    /// </para>
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class ExtendedIntegrationTests
    {
        private readonly MySqlFixture _fixture;

        public ExtendedIntegrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task GetFirstAsync_WithPredicateAndSort_ReturnsTheFirstRowInSortOrder()
        {
            for (int i = 0; i < 3; i++)
                await _fixture.Connection.InsertAsync(new Widget { Id = 200 + i, Name = $"First{i}", IsActive = true, Price = 1m }, cancellationToken: Ct);

            Widget fetched = await _fixture.Connection.GetFirstAsync<Widget>(w => w.Id >= 200 && w.Id <= 202, sortDescriptors: [new SortDescriptor<Widget>(w => w.Id, SortDirection.Descending)], cancellationToken: Ct);

            Assert.Equal(202, fetched.Id);
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_WithPredicate_ReturnsNullWhenNothingMatches()
        {
            Widget? fetched = await _fixture.Connection.GetFirstOrDefaultAsync<Widget>(w => w.Id == 999_001, cancellationToken: Ct);

            Assert.Null(fetched);
        }

        [Fact]
        public async Task GetFirstOrDefaultAsync_WithPredicate_ReturnsAMatch()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 100, Name = "First candidate", IsActive = true, Price = 1m }, cancellationToken: Ct);

            Widget? fetched = await _fixture.Connection.GetFirstOrDefaultAsync<Widget>(w => w.Id == 100, cancellationToken: Ct);

            Assert.NotNull(fetched);
            Assert.Equal("First candidate", fetched!.Name);
        }

        [Fact]
        public async Task GetSingleAsync_WithPredicate_ReturnsTheOnlyMatch()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 101, Name = "Unique", IsActive = true, Price = 1m }, cancellationToken: Ct);

            Widget fetched = await _fixture.Connection.GetSingleAsync<Widget>(w => w.Id == 101, cancellationToken: Ct);

            Assert.Equal("Unique", fetched.Name);
        }

        [Fact]
        public async Task GetSingleAsync_WhenSeveralRowsMatch_Throws()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 210, Name = "Twin", IsActive = true, Price = 1m }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new Widget { Id = 211, Name = "Twin", IsActive = true, Price = 1m }, cancellationToken: Ct);

            await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Connection.GetSingleAsync<Widget>(w => w.Name == "Twin", cancellationToken: Ct));
        }

        [Fact]
        public async Task GetSingleOrDefaultAsync_WithPredicate_ReturnsNullWhenNothingMatches()
        {
            Widget? fetched = await _fixture.Connection.GetSingleOrDefaultAsync<Widget>(w => w.Id == 999_002, cancellationToken: Ct);

            Assert.Null(fetched);
        }

        [Fact]
        public async Task GetPageAsync_WithSkipAndTake_ReturnsTheRequestedSlice()
        {
            for (int i = 0; i < 5; i++)
                await _fixture.Connection.InsertAsync(new Widget { Id = 110 + i, Name = $"Page{i}", IsActive = true, Price = 1m }, cancellationToken: Ct);

            IReadOnlyList<Widget> page = await _fixture.Connection.GetPageAsync(w => w.Id >= 110 && w.Id < 115, sortDescriptors: [new SortDescriptor<Widget>(w => w.Id)], skip: 1, take: 2, cancellationToken: Ct);

            Assert.Equal([111, 112], page.Select(w => w.Id));
        }

        [Fact]
        public async Task GetByIdRangeAsync_ReturnsRowsForEachRequestedId()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 120, Name = "R120", IsActive = true, Price = 1m }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new Widget { Id = 121, Name = "R121", IsActive = true, Price = 1m }, cancellationToken: Ct);

            IReadOnlyList<Widget> rows = await _fixture.Connection.GetByIdRangeAsync<Widget>(new[] { 120, 121, 999_003 }, cancellationToken: Ct);

            Assert.Equal(2, rows.Count);
            Assert.Contains(rows, w => w.Id == 120);
            Assert.Contains(rows, w => w.Id == 121);
        }

        [Fact]
        public async Task UpdateRangeAsync_UpdatesEveryEntityInTheBatch()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 130, Name = "Old130", IsActive = false, Price = 1m }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new Widget { Id = 131, Name = "Old131", IsActive = false, Price = 1m }, cancellationToken: Ct);

            Widget[] updates =
            [
                new() { Id = 130, Name = "New130", IsActive = true, Price = 2m },
                new() { Id = 131, Name = "New131", IsActive = true, Price = 2m },
            ];
            int affected = await _fixture.Connection.UpdateRangeAsync(updates, cancellationToken: Ct);

            Widget? w130 = await _fixture.Connection.GetByIdAsync<Widget>(130, cancellationToken: Ct);
            Widget? w131 = await _fixture.Connection.GetByIdAsync<Widget>(131, cancellationToken: Ct);

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
            await _fixture.Connection.InsertRangeAsync(toDelete, cancellationToken: Ct);

            int affected = await _fixture.Connection.DeleteRangeAsync(toDelete, cancellationToken: Ct);

            Assert.Equal(2, affected);
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(140, cancellationToken: Ct));
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(141, cancellationToken: Ct));
        }

        [Fact]
        public async Task DeleteRangeAsync_ByIds_RemovesEveryRowWithThoseIds()
        {
            await _fixture.Connection.InsertRangeAsync([new Widget { Id = 220, Name = "D220", IsActive = true, Price = 1m }, new Widget { Id = 221, Name = "D221", IsActive = true, Price = 1m }, new Widget { Id = 222, Name = "D222", IsActive = true, Price = 1m }], cancellationToken: Ct);

            int affected = await _fixture.Connection.DeleteRangeAsync<Widget>(new[] { 220, 221 }, cancellationToken: Ct);

            Assert.Equal(2, affected);
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(220, cancellationToken: Ct));
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(221, cancellationToken: Ct));
            Assert.NotNull(await _fixture.Connection.GetByIdAsync<Widget>(222, cancellationToken: Ct));
        }

        [Fact]
        public async Task ExistsAsync_WithPredicate_ReflectsWhetherAMatchingRowExists()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 150, Name = "Exists150", IsActive = true, Price = 1m }, cancellationToken: Ct);

            bool existsTrue = await _fixture.Connection.ExistsAsync<Widget>(w => w.Id == 150, cancellationToken: Ct);
            bool existsFalse = await _fixture.Connection.ExistsAsync<Widget>(w => w.Id == 999_004, cancellationToken: Ct);

            Assert.True(existsTrue);
            Assert.False(existsFalse);
        }

        [Fact]
        public async Task CountAsync_WithPredicate_CountsOnlyMatchingRows()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 160, Name = "Count160", IsActive = true, Price = 1m }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new Widget { Id = 161, Name = "Count161", IsActive = false, Price = 1m }, cancellationToken: Ct);

            long activeCount = await _fixture.Connection.CountAsync<Widget>(w => w.IsActive && (w.Id == 160 || w.Id == 161), cancellationToken: Ct);

            Assert.Equal(1, activeCount);
        }

        [Fact]
        public async Task Aggregates_SumAvgMinMax_ComputeOverMatchingRows()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 170, Name = "Agg170", IsActive = true, Price = 10m }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new Widget { Id = 171, Name = "Agg171", IsActive = true, Price = 20m }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new Widget { Id = 172, Name = "Agg172", IsActive = true, Price = 30m }, cancellationToken: Ct);

            decimal? sum = await _fixture.Connection.SumAsync<Widget>(w => w.Price, w => w.Id >= 170 && w.Id <= 172, cancellationToken: Ct);
            decimal? avg = await _fixture.Connection.AvgAsync<Widget>(w => w.Price, w => w.Id >= 170 && w.Id <= 172, cancellationToken: Ct);
            decimal? min = await _fixture.Connection.MinAsync<Widget, decimal>(w => w.Price, w => w.Id >= 170 && w.Id <= 172, cancellationToken: Ct);
            decimal? max = await _fixture.Connection.MaxAsync<Widget, decimal>(w => w.Price, w => w.Id >= 170 && w.Id <= 172, cancellationToken: Ct);

            Assert.Equal(60m, sum);
            Assert.Equal(20m, avg);
            Assert.Equal(10m, min);
            Assert.Equal(30m, max);
        }

        [Fact]
        public async Task AvgAsync_WithAFractionalResult_ReturnsTheAverageAsADecimal()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 230, Name = "Avg230", IsActive = true, Price = 1m }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new Widget { Id = 231, Name = "Avg231", IsActive = true, Price = 1m }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new Widget { Id = 232, Name = "Avg232", IsActive = true, Price = 2m }, cancellationToken: Ct);

            decimal? avg = await _fixture.Connection.AvgAsync<Widget>(w => w.Price, w => w.Id >= 230 && w.Id <= 232, cancellationToken: Ct);

            // 4 / 3: every engine returns its own number of decimals, the value must survive the trip either way.
            Assert.Equal(1.3333m, Math.Round(avg!.Value, 4));
        }
        [Fact]
        public async Task UpdateAsync_WithValuesAndPredicate_UpdatesOnlyMatchingRows()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 180, Name = "Bulk180", IsActive = false, Price = 1m }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new Widget { Id = 181, Name = "Bulk181", IsActive = false, Price = 1m }, cancellationToken: Ct);

            int affected = await _fixture.Connection.UpdateAsync<Widget>(new { IsActive = true }, w => w.Id == 180, cancellationToken: Ct);

            Widget? w180 = await _fixture.Connection.GetByIdAsync<Widget>(180, cancellationToken: Ct);
            Widget? w181 = await _fixture.Connection.GetByIdAsync<Widget>(181, cancellationToken: Ct);

            Assert.Equal(1, affected);
            Assert.True(w180!.IsActive);
            Assert.False(w181!.IsActive);
        }

        [Fact]
        public async Task DeleteAsync_WithPredicate_DeletesOnlyMatchingRows()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 190, Name = "DelPred190", IsActive = true, Price = 1m }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new Widget { Id = 191, Name = "DelPred191", IsActive = true, Price = 1m }, cancellationToken: Ct);

            int affected = await _fixture.Connection.DeleteAsync<Widget>(w => w.Id == 190, cancellationToken: Ct);

            Assert.Equal(1, affected);
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(190, cancellationToken: Ct));
            Assert.NotNull(await _fixture.Connection.GetByIdAsync<Widget>(191, cancellationToken: Ct));
        }
    }
}
