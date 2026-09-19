using Forget.Oracle.Extensions;
using Oracle.ManagedDataAccess.Client;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// An entity maps the columns it declares, not necessarily the whole table. The multi-row statements need the
    /// column types the discovery block reads from the data dictionary, and that used to require the entity to map
    /// every column of the table.
    /// </summary>
    [Collection(OracleCollection.Name)]
    public class PartialMappingIntegrationTests
    {
        private readonly OracleFixture _fixture;

        public PartialMappingIntegrationTests(OracleFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private async Task<string> LegacyOfAsync(int id)
        {
            await using OracleCommand command = _fixture.Connection.CreateCommand();
            command.BindByName = true;
            command.CommandText = "SELECT \"Legacy\" FROM \"PartialRow\" WHERE \"Id\" = :Id";
            command.Parameters.Add("Id", id);

            return (string)(await command.ExecuteScalarAsync(Ct))!;
        }

        [Fact]
        public async Task InsertRangeAsync_WritesTheMappedColumnsAndLetsTheOthersTakeTheirDefault()
        {
            await _fixture.Connection.InsertRangeAsync([new PartialRow { Id = 10, Qty = 3 }, new PartialRow { Id = 11, Qty = 5 }], cancellationToken: Ct);

            IReadOnlyList<PartialRow?> fetched = await _fixture.Connection.GetByIdRangeAsync<PartialRow>(new[] { 10, 11 }, cancellationToken: Ct);

            Assert.Equal([3, 5], fetched.Select(r => r!.Qty).ToList());
            Assert.Equal("legacy", await LegacyOfAsync(10));
            Assert.Equal("legacy", await LegacyOfAsync(11));
        }

        [Fact]
        public async Task UpdateRangeAsync_WorksWhenTheTableHasColumnsTheEntityDoesNotMap()
        {
            await _fixture.Connection.InsertRangeAsync([new PartialRow { Id = 20, Qty = 1 }, new PartialRow { Id = 21, Qty = 2 }], cancellationToken: Ct);

            await _fixture.Connection.UpdateRangeAsync([new PartialRow { Id = 20, Qty = 10 }, new PartialRow { Id = 21, Qty = 20 }], cancellationToken: Ct);
            IReadOnlyList<PartialRow?> fetched = await _fixture.Connection.GetByIdRangeAsync<PartialRow>(new[] { 20, 21 }, cancellationToken: Ct);

            Assert.Equal([10, 20], fetched.Select(r => r!.Qty).ToList());
            Assert.Equal("legacy", await LegacyOfAsync(20));
            Assert.Equal("legacy", await LegacyOfAsync(21));
        }

        [Fact]
        public async Task UpsertRangeAsync_WorksWhenTheTableHasColumnsTheEntityDoesNotMap()
        {
            await _fixture.Connection.InsertRangeAsync([new PartialRow { Id = 30, Qty = 1 }], cancellationToken: Ct);

            await _fixture.Connection.UpsertRangeAsync([new PartialRow { Id = 30, Qty = 10 }, new PartialRow { Id = 31, Qty = 20 }], cancellationToken: Ct);
            IReadOnlyList<PartialRow?> fetched = await _fixture.Connection.GetByIdRangeAsync<PartialRow>(new[] { 30, 31 }, cancellationToken: Ct);

            Assert.Equal([10, 20], fetched.Select(r => r!.Qty).ToList());
            Assert.Equal("legacy", await LegacyOfAsync(30));
            Assert.Equal("legacy", await LegacyOfAsync(31));
        }
    }
}
