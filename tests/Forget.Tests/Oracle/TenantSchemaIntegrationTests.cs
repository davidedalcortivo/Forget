using Forget.Oracle.Extensions;
using Oracle.ManagedDataAccess.Client;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// An entity without a schema in <c>[Table]</c> is left unqualified, so Oracle resolves it in the current schema of
    /// the session, as it resolves hand-written SQL: the user's, or the one set with
    /// <c>ALTER SESSION SET CURRENT_SCHEMA</c>.
    /// Forget used to qualify it with the user of the first connection that reached it, for the whole process.
    /// </summary>
    [Collection(OracleCollection.Name)]
    public class TenantSchemaIntegrationTests
    {
        private readonly OracleFixture _fixture;

        public TenantSchemaIntegrationTests(OracleFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        // A session of its own, out of the pool, whose current schema is "tenant_b".
        private async Task<OracleConnection> OpenTenantAsync()
        {
            OracleConnection tenant = new(new OracleConnectionStringBuilder(_fixture.ConnectionString) { Pooling = false }.ConnectionString);
            await tenant.OpenAsync(Ct);

            await using OracleCommand command = tenant.CreateCommand();
            command.CommandText = "ALTER SESSION SET CURRENT_SCHEMA = \"tenant_b\"";
            await command.ExecuteNonQueryAsync(Ct);

            return tenant;
        }

        [Fact]
        public async Task AnEntityWithoutASchema_IsReadFromTheCurrentSchemaOfTheSession()
        {
            await using OracleConnection tenant = await OpenTenantAsync();

            Assert.Equal("dbo", (await _fixture.Connection.GetByIdAsync<TenantRow>(1, cancellationToken: Ct))!.Name);
            Assert.Equal("tenant_b", (await tenant.GetByIdAsync<TenantRow>(1, cancellationToken: Ct))!.Name);
        }

        [Fact]
        public async Task InsertRangeAsync_ReadsTheColumnsOfTheTableInTheCurrentSchema()
        {
            await using OracleConnection tenant = await OpenTenantAsync();

            // A multi-row insert reads the column types from the catalog first, for the table of the current schema; the table
            // is only in "tenant_b", so looking it up anywhere else finds no columns.
            await tenant.InsertRangeAsync([new TenantOnlyRow { Id = 1, Name = "written to tenant_b", Amount = 1 }, new TenantOnlyRow { Id = 2, Name = "written to tenant_b", Amount = 2 }], cancellationToken: Ct);

            Assert.Equal(2, await tenant.CountAsync<TenantOnlyRow>(cancellationToken: Ct));
        }
    }
}
