using Forget.Core.Models;
using Forget.MySql.Extensions;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// Forget quotes every table, schema and column name, doubling the closing quote character, so a table whose names
    /// the database only accepts quoted works through every operation: each one builds its SQL in its own way, so each
    /// one is exercised.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class OddNameIntegrationTests
    {
        private readonly MySqlFixture _fixture;

        public OddNameIntegrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static OddNameRow Row(int id, string tag) => new()
        {
            Id = id,
            LineName = $"{tag} line",
            City = $"{tag} città",
            Code = id % 3,
            SecondValue = id * 10,
            Flag = id % 2,
            Backslash = $"{tag}\\{id}",
            Symbols = $"{tag}@:?${id}",
            DotName = $"{tag}.{id}",
            Quoted = $"{tag}`{id}"
        };

        [Fact]
        public async Task SingleRowOperations_WorkOnATableWithOddNames()
        {
            OddNameRow row = Row(10, "single");

            await _fixture.Connection.InsertAsync(row, cancellationToken: Ct);
            Assert.Equivalent(row, await _fixture.Connection.GetByIdAsync<OddNameRow>(10, cancellationToken: Ct));

            row.City = "updated città";
            row.Symbols = "updated@:?$";
            await _fixture.Connection.UpdateAsync(row, cancellationToken: Ct);
            Assert.Equivalent(row, await _fixture.Connection.GetByIdAsync<OddNameRow>(10, cancellationToken: Ct));

            await _fixture.Connection.UpdateAsync<OddNameRow>(new { City = "partial", Backslash = "partial\\" }, x => x.Id == 10, cancellationToken: Ct);
            OddNameRow? partial = await _fixture.Connection.GetByIdAsync<OddNameRow>(10, cancellationToken: Ct);
            Assert.Equal(("partial", "partial\\"), (partial!.City, partial.Backslash));

            row.LineName = "upserted";
            await _fixture.Connection.UpsertAsync(row, cancellationToken: Ct);
            Assert.Equal("upserted", (await _fixture.Connection.GetByIdAsync<OddNameRow>(10, cancellationToken: Ct))!.LineName);

            await _fixture.Connection.DeleteAsync<OddNameRow>(10, cancellationToken: Ct);
            Assert.Null(await _fixture.Connection.GetByIdAsync<OddNameRow>(10, cancellationToken: Ct));
        }

        [Fact]
        public async Task RangeOperations_WorkOnATableWithOddNames()
        {
            OddNameRow[] rows = [Row(20, "range"), Row(21, "range"), Row(22, "range")];

            await _fixture.Connection.InsertRangeAsync(rows, cancellationToken: Ct);
            Assert.Equivalent(rows, await _fixture.Connection.GetByIdRangeAsync<OddNameRow, int>([20, 21, 22], cancellationToken: Ct));

            foreach (OddNameRow row in rows)
                row.Symbols = $"updated@:?${row.Id}";

            await _fixture.Connection.UpdateRangeAsync(rows, cancellationToken: Ct);
            Assert.Equivalent(rows, await _fixture.Connection.GetByIdRangeAsync<OddNameRow, int>([20, 21, 22], cancellationToken: Ct));

            OddNameRow[] upserts = [Row(22, "upsert"), Row(23, "upsert")];
            await _fixture.Connection.UpsertRangeAsync(upserts, cancellationToken: Ct);
            Assert.Equivalent(upserts, await _fixture.Connection.GetByIdRangeAsync<OddNameRow, int>([22, 23], cancellationToken: Ct));

            Assert.Equal(4, await _fixture.Connection.DeleteRangeAsync<OddNameRow, int>([20, 21, 22, 23], cancellationToken: Ct));
            Assert.Empty(await _fixture.Connection.GetByIdRangeAsync<OddNameRow, int>([20, 21, 22, 23], cancellationToken: Ct));
        }

        [Fact]
        public async Task FiltersSortingPagingAndAggregates_WorkOnATableWithOddNames()
        {
            OddNameRow[] rows = [.. Enumerable.Range(40, 5).Select(id => Row(id, "query"))];
            await _fixture.Connection.InsertRangeAsync(rows, cancellationToken: Ct);
            int[] codes = [0, 1];

            IReadOnlyList<OddNameRow> byExpression = await _fixture.Connection.GetAllAsync<OddNameRow>(
                x => x.Id >= 40 && x.Id <= 44 && codes.Contains(x.Code) && x.City.EndsWith("città") && x.Symbols.Contains("@:?$") && x.Flag >= 0,
                sortDescriptors: [new SortDescriptor<OddNameRow>(x => x.SecondValue, SortDirection.Descending)], cancellationToken: Ct);
            Assert.Equal([43, 42, 40], byExpression.Select(x => x.Id));

            FilterGroup<OddNameRow> filter = new([
                new FilterDescriptor<OddNameRow>("Id", new[] { 40, 41, 42, 43, 44 }, ComparisonOperator.In),
                new FilterDescriptor<OddNameRow>("Backslash", "query\\42", ComparisonOperator.NotEqual)
            ]);
            IReadOnlyList<OddNameRow> byFilter = await _fixture.Connection.GetAllAsync(filter, cancellationToken: Ct);
            Assert.Equal([40, 41, 43, 44], byFilter.Select(x => x.Id).Order());

            IReadOnlyList<OddNameRow> page = await _fixture.Connection.GetPageAsync<OddNameRow>(x => x.Id >= 40 && x.Id <= 44,
                sortDescriptors: [new SortDescriptor<OddNameRow>(x => x.DotName)], skip: 1, take: 2, cancellationToken: Ct);
            Assert.Equal([41, 42], page.Select(x => x.Id));

            Assert.Equal(5, await _fixture.Connection.CountAsync<OddNameRow>(x => x.Id >= 40 && x.Id <= 44, cancellationToken: Ct));
            Assert.True(await _fixture.Connection.ExistsAsync<OddNameRow>(x => x.LineName == "query line" && x.Quoted == "query`43", cancellationToken: Ct));
            Assert.Equal(2100m, await _fixture.Connection.SumAsync<OddNameRow>(nameof(OddNameRow.SecondValue), x => x.Id >= 40 && x.Id <= 44, cancellationToken: Ct));
            Assert.Equal(420m, await _fixture.Connection.AvgAsync<OddNameRow>(nameof(OddNameRow.SecondValue), x => x.Id >= 40 && x.Id <= 44, cancellationToken: Ct));
            Assert.Equal(400, await _fixture.Connection.MinAsync<OddNameRow, int>(x => x.SecondValue, x => x.Id >= 40 && x.Id <= 44, cancellationToken: Ct));
            Assert.Equal(440, await _fixture.Connection.MaxAsync<OddNameRow, int>(x => x.SecondValue, x => x.Id >= 40 && x.Id <= 44, cancellationToken: Ct));

            Assert.Equal(5, await _fixture.Connection.DeleteAsync<OddNameRow>(x => x.Id >= 40 && x.Id <= 44 && x.DotName.StartsWith("query."), cancellationToken: Ct));
        }
    }
}
