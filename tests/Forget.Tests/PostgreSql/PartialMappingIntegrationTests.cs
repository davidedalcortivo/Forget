using Forget.PostgreSql.Extensions;
using Npgsql;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// An entity maps the columns it declares, not necessarily the whole table. PostgreSQL's <c>UpdateRange</c>
    /// builds each row as a value of the table's own row type, which needs a value for every physical column in
    /// order; the columns the entity does not map are filled with <c>NULL</c>, which the <c>UPDATE</c> never reads.
    /// </summary>
    [Collection(PostgreSqlCollection.Name)]
    public class PartialMappingIntegrationTests
    {
        private readonly PostgreSqlFixture _fixture;

        public PartialMappingIntegrationTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private async Task<string> LegacyOfAsync(int id)
        {
            await using NpgsqlCommand command = _fixture.Connection.CreateCommand();
            command.CommandText = "SELECT \"Legacy\" FROM dbo.\"PartialRow\" WHERE \"Id\" = @Id";
            command.Parameters.AddWithValue("Id", id);

            return (string)(await command.ExecuteScalarAsync(Ct))!;
        }

        [Fact]
        public async Task InsertRangeAsync_WritesTheMappedColumnsAndLetsTheOthersTakeTheirDefault()
        {
            await _fixture.Connection.InsertRangeAsync([new PartialRow { Id = 10, Qty = 3 }, new PartialRow { Id = 11, Qty = 5 }], cancellationToken: Ct);

            IReadOnlyList<PartialRow> fetched = await _fixture.Connection.GetByIdRangeAsync<PartialRow>(new[] { 10, 11 }, cancellationToken: Ct);

            Assert.Equal([3, 5], fetched.OrderBy(r => r.Id).Select(r => r.Qty).ToList());
            Assert.Equal("legacy", await LegacyOfAsync(10));
            Assert.Equal("legacy", await LegacyOfAsync(11));
        }

        [Fact]
        public async Task UpdateRangeAsync_WorksWhenTheTableHasColumnsTheEntityDoesNotMap()
        {
            await _fixture.Connection.InsertRangeAsync([new PartialRow { Id = 20, Qty = 1 }, new PartialRow { Id = 21, Qty = 2 }], cancellationToken: Ct);

            int affected = await _fixture.Connection.UpdateRangeAsync([new PartialRow { Id = 20, Qty = 10 }, new PartialRow { Id = 21, Qty = 20 }], cancellationToken: Ct);
            IReadOnlyList<PartialRow> fetched = await _fixture.Connection.GetByIdRangeAsync<PartialRow>(new[] { 20, 21 }, cancellationToken: Ct);

            Assert.Equal(2, affected);
            Assert.Equal([10, 20], fetched.OrderBy(r => r.Id).Select(r => r.Qty).ToList());
            Assert.Equal("legacy", await LegacyOfAsync(20));
            Assert.Equal("legacy", await LegacyOfAsync(21));
        }

        [Fact]
        public async Task UpsertRangeAsync_WorksWhenTheTableHasColumnsTheEntityDoesNotMap()
        {
            await _fixture.Connection.InsertRangeAsync([new PartialRow { Id = 30, Qty = 1 }], cancellationToken: Ct);

            await _fixture.Connection.UpsertRangeAsync([new PartialRow { Id = 30, Qty = 10 }, new PartialRow { Id = 31, Qty = 20 }], cancellationToken: Ct);
            IReadOnlyList<PartialRow> fetched = await _fixture.Connection.GetByIdRangeAsync<PartialRow>(new[] { 30, 31 }, cancellationToken: Ct);

            Assert.Equal([10, 20], fetched.OrderBy(r => r.Id).Select(r => r.Qty).ToList());
            Assert.Equal("legacy", await LegacyOfAsync(30));
            Assert.Equal("legacy", await LegacyOfAsync(31));
        }
    }
}
