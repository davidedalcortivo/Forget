using Forget.PostgreSql.Extensions;
using Npgsql;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// An entity without <c>[Table(Schema = ...)]</c> is left unqualified, so PostgreSQL resolves it through the
    /// search_path, as it resolves hand-written SQL. Forget used to write "public" for every connection, so with a schema per
    /// tenant chosen by the search_path it read and wrote public's table, or failed when public had none.
    /// </summary>
    [Collection(PostgreSqlCollection.Name)]
    public class TenantSchemaIntegrationTests
    {
        private readonly PostgreSqlFixture _fixture;

        public TenantSchemaIntegrationTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task AnEntityWithoutASchema_IsReadFromTheSchemaOfTheSearchPath()
        {
            await using NpgsqlConnection tenantA = new(_fixture.ConnectionStringFor("tenant_a"));
            await using NpgsqlConnection tenantB = new(_fixture.ConnectionStringFor("tenant_b"));

            Assert.Equal("tenant_a", (await tenantA.GetByIdAsync<TenantRow>(1, cancellationToken: Ct))!.Name);
            Assert.Equal("tenant_b", (await tenantB.GetByIdAsync<TenantRow>(1, cancellationToken: Ct))!.Name);
        }

        [Fact]
        public async Task UpdateRangeAsync_OnTenantsWhoseColumnsAreInADifferentOrder_WritesEachValueInItsColumn()
        {
            await using NpgsqlConnection tenantA = new(_fixture.ConnectionStringFor("tenant_a"));
            await using NpgsqlConnection tenantB = new(_fixture.ConnectionStringFor("tenant_b"));

            // UpdateRange builds its rows from the table's columns, read from the catalog and cached per connection.
            await tenantA.UpdateRangeAsync([new TenantRow { Id = 2, Name = "updated a", Amount = 11 }], cancellationToken: Ct);
            await tenantB.UpdateRangeAsync([new TenantRow { Id = 2, Name = "updated b", Amount = 21 }], cancellationToken: Ct);

            TenantRow a = (await tenantA.GetByIdAsync<TenantRow>(2, cancellationToken: Ct))!;
            TenantRow b = (await tenantB.GetByIdAsync<TenantRow>(2, cancellationToken: Ct))!;
            Assert.Equal(("updated a", 11), (a.Name, a.Amount));
            Assert.Equal(("updated b", 21), (b.Name, b.Amount));
        }
    }
}
