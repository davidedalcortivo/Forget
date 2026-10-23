using Forget.Core.Models;
using Forget.PostgreSql.Extensions;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// Every parameter Forget binds in one command must have its own name. A multi-row write names a parameter after the
    /// property and the row, and a list is expanded by Dapper, which appends its own numbers to the parameter's name; so
    /// <c>Address</c> in row 20 and <c>Address2</c> in row 0, or the first element of the list in <c>p1</c> and the twelfth
    /// filter value, used to get the same name. The later value then replaced the earlier one, without an error: a row
    /// was written with another row's value, or a filter compared a column with the wrong value.
    /// </summary>
    [Collection(PostgreSqlCollection.Name)]
    public class ParameterNameIntegrationTests
    {
        private const int Rows = 25;

        private readonly PostgreSqlFixture _fixture;

        public ParameterNameIntegrationTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static ParameterNameRow[] RowsFrom(int firstId, string tag) =>
            [.. Enumerable.Range(0, Rows).Select(i => new ParameterNameRow { Id = firstId + i, Address = $"{tag}-A{i}", Address2 = $"{tag}-B{i}" })];

        private async Task AssertStoredAsync(ParameterNameRow[] expected)
        {
            int first = expected[0].Id;
            int last = expected[^1].Id;
            IReadOnlyList<ParameterNameRow> stored = await _fixture.Connection.GetAllAsync<ParameterNameRow>(x => x.Id >= first && x.Id <= last, cancellationToken: Ct);

            Assert.Equal(expected.Length, stored.Count);

            foreach (ParameterNameRow row in stored)
            {
                ParameterNameRow wanted = expected.Single(x => x.Id == row.Id);
                Assert.Equal((wanted.Address, wanted.Address2), (row.Address, row.Address2));
            }
        }

        [Fact]
        public async Task InsertRangeAsync_WithPropertiesWhoseNamesDifferByTrailingDigits_WritesEveryRowItsOwnValues()
        {
            ParameterNameRow[] rows = RowsFrom(100, "insert");

            Assert.Equal(Rows * 3, _fixture.Connection.InsertRangeCommands(rows).Single().Parameters!.ParameterNames.Count());

            await _fixture.Connection.InsertRangeAsync(rows, cancellationToken: Ct);

            await AssertStoredAsync(rows);
        }

        [Fact]
        public async Task UpdateRangeAsync_WithPropertiesWhoseNamesDifferByTrailingDigits_WritesEveryRowItsOwnValues()
        {
            await _fixture.Connection.InsertRangeAsync(RowsFrom(200, "before"), batchSize: 10, cancellationToken: Ct);
            ParameterNameRow[] rows = RowsFrom(200, "update");

            await _fixture.Connection.UpdateRangeAsync(rows, cancellationToken: Ct);

            await AssertStoredAsync(rows);
        }

        [Fact]
        public async Task UpsertRangeAsync_WithPropertiesWhoseNamesDifferByTrailingDigits_WritesEveryRowItsOwnValues()
        {
            await _fixture.Connection.InsertRangeAsync(RowsFrom(300, "before")[..12], batchSize: 10, cancellationToken: Ct);
            ParameterNameRow[] rows = RowsFrom(300, "upsert");

            await _fixture.Connection.UpsertRangeAsync(rows, cancellationToken: Ct);

            await AssertStoredAsync(rows);
        }

        [Fact]
        public async Task GetAllAsync_WithAListAndMoreThanTenOtherFilterValues_ComparesEachColumnWithItsOwnValue()
        {
            await _fixture.Connection.InsertRangeAsync(RowsFrom(400, "filter")[..5], cancellationToken: Ct);

            // The list is the second value and is expanded into one parameter per element (by Dapper, except on PostgreSQL,
            // which binds it as an array); ten more values follow, so the last one is the twelfth.
            List<FilterDescriptor<ParameterNameRow>> filters =
            [
                new("Id", 400, ComparisonOperator.GreaterThanOrEqual),
                new("Id", new[] { 400, 401 }, ComparisonOperator.In)
            ];

            for (int i = 1; i <= 10; i++)
                filters.Add(new("Id", -i, ComparisonOperator.NotEqual));

            IReadOnlyList<ParameterNameRow> found = await _fixture.Connection.GetAllAsync(new FilterGroup<ParameterNameRow>(filters), cancellationToken: Ct);

            Assert.Equal([400, 401], found.Select(x => x.Id).Order());
        }

        [Fact]
        public async Task GetAllAsync_WithAContainsAndMoreThanTenOtherValuesInThePredicate_ComparesEachColumnWithItsOwnValue()
        {
            await _fixture.Connection.InsertRangeAsync(RowsFrom(500, "predicate")[..5], cancellationToken: Ct);
            int[] ids = [500, 501];

            IReadOnlyList<ParameterNameRow> found = await _fixture.Connection.GetAllAsync<ParameterNameRow>(x =>
                x.Id >= 500 && ids.Contains(x.Id) &&
                x.Id != -1 && x.Id != -2 && x.Id != -3 && x.Id != -4 && x.Id != -5 &&
                x.Id != -6 && x.Id != -7 && x.Id != -8 && x.Id != -9 && x.Id != -10, cancellationToken: Ct);

            Assert.Equal([500, 501], found.Select(x => x.Id).Order());
        }
    }
}
