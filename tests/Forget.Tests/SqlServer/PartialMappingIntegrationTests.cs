using Forget.SqlServer.Extensions;
using Microsoft.Data.SqlClient;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// An entity maps the columns it declares, not necessarily the whole table. <c>Sum</c> and <c>Avg</c> read the
    /// column types from the database to widen integer columns, and that used to require the entity to map every
    /// column of the table.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public class PartialMappingIntegrationTests
    {
        private readonly SqlServerFixture _fixture;

        public PartialMappingIntegrationTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private async Task<string> LegacyOfAsync(int id)
        {
            await using SqlCommand command = _fixture.Connection.CreateCommand();
            command.CommandText = "SELECT Legacy FROM dbo.PartialRow WHERE Id = @Id";
            command.Parameters.AddWithValue("@Id", id);

            return (string)(await command.ExecuteScalarAsync(Ct))!;
        }

        [Fact]
        public async Task SumAsync_And_AvgAsync_OnAnIntegerColumn_WorkWhenTheTableHasColumnsTheEntityDoesNotMap()
        {
            await _fixture.Connection.InsertRangeAsync([new PartialRow { Id = 10, Qty = 3 }, new PartialRow { Id = 11, Qty = 5 }], cancellationToken: Ct);

            decimal? sum = await _fixture.Connection.SumAsync<PartialRow>("Qty", r => r.Id >= 10 && r.Id <= 11, cancellationToken: Ct);
            decimal? avg = await _fixture.Connection.AvgAsync<PartialRow>("Qty", r => r.Id >= 10 && r.Id <= 11, cancellationToken: Ct);

            Assert.Equal(8m, sum);
            Assert.Equal(4m, avg);
        }

        [Fact]
        public async Task RangeWrites_WriteTheMappedColumnsAndLeaveTheOthersAlone()
        {
            await _fixture.Connection.InsertRangeAsync([new PartialRow { Id = 20, Qty = 1 }, new PartialRow { Id = 21, Qty = 2 }], cancellationToken: Ct);
            await _fixture.Connection.UpdateRangeAsync([new PartialRow { Id = 20, Qty = 10 }], cancellationToken: Ct);
            await _fixture.Connection.UpsertRangeAsync([new PartialRow { Id = 21, Qty = 20 }, new PartialRow { Id = 22, Qty = 30 }], cancellationToken: Ct);

            IReadOnlyList<PartialRow> fetched = await _fixture.Connection.GetByIdRangeAsync<PartialRow>(new[] { 20, 21, 22 }, cancellationToken: Ct);

            Assert.Equal([10, 20, 30], fetched.OrderBy(r => r.Id).Select(r => r.Qty).ToList());
            Assert.Equal("legacy", await LegacyOfAsync(20));
            Assert.Equal("legacy", await LegacyOfAsync(21));
            Assert.Equal("legacy", await LegacyOfAsync(22));
        }
    }
}
