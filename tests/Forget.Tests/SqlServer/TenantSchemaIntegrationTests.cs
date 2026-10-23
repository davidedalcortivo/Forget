using Forget.SqlServer.Extensions;
using Microsoft.Data.SqlClient;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// An entity without <c>[Table(Schema = ...)]</c> is left unqualified, so SQL Server resolves it as it resolves
    /// hand-written SQL: in the default schema of the connection's user, then in dbo. Forget used to write [dbo] for every
    /// user, so a user whose default schema is tenant_a read and wrote dbo's table.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public class TenantSchemaIntegrationTests
    {
        private readonly SqlServerFixture _fixture;

        public TenantSchemaIntegrationTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task AnEntityWithoutASchema_IsReadFromTheDefaultSchemaOfTheUser()
        {
            await using SqlConnection tenant = new(_fixture.TenantConnectionString);

            Assert.Equal("dbo", (await _fixture.Connection.GetByIdAsync<TenantRow>(1, cancellationToken: Ct))!.Name);
            Assert.Equal("tenant_a", (await tenant.GetByIdAsync<TenantRow>(1, cancellationToken: Ct))!.Name);
        }

        [Fact]
        public async Task TheColumnsOfAnEntityWithoutASchema_AreReadFromTheTableOfTheUser()
        {
            await using SqlConnection tenant = new(_fixture.TenantConnectionString);

            // Sum reads the column types from the catalog first, for the table the user's statement would use; TenantOnlyRow
            // is only in tenant_a, so looking it up anywhere else finds no columns.
            Assert.Equal(10m, await _fixture.Connection.SumAsync<TenantRow>(nameof(TenantRow.Amount), cancellationToken: Ct));
            Assert.Equal(20m, await tenant.SumAsync<TenantRow>(nameof(TenantRow.Amount), cancellationToken: Ct));
            Assert.Equal(7m, await tenant.SumAsync<TenantOnlyRow>(nameof(TenantOnlyRow.Amount), cancellationToken: Ct));
        }

        [Fact]
        public async Task ATableOnlyInDbo_IsFoundForAUserWithAnotherDefaultSchema()
        {
            await using SqlConnection tenant = new(_fixture.TenantConnectionString);

            Assert.Equal("dbo only", (await tenant.GetByIdAsync<DboOnlyRow>(1, cancellationToken: Ct))!.Name);
            Assert.Equal(5m, await tenant.SumAsync<DboOnlyRow>(nameof(DboOnlyRow.Amount), cancellationToken: Ct));
        }
    }
}
