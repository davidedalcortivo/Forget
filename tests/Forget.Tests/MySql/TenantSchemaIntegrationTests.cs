using Forget.MySql.Extensions;
using MySqlConnector;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// An entity without a schema in <c>[Table]</c> is left unqualified, so MySql resolves it in the database of the
    /// connection, as it resolves hand-written SQL. Forget used to qualify it with the database of the first connection
    /// that reached it, for the whole process, so with a database per tenant every tenant read and wrote the first one's.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class TenantSchemaIntegrationTests
    {
        private readonly MySqlFixture _fixture;

        public TenantSchemaIntegrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task AnEntityWithoutASchema_IsReadFromTheDatabaseOfTheConnection()
        {
            await using MySqlConnection tenantA = new(_fixture.ConnectionStringFor("tenant_a"));
            await using MySqlConnection tenantB = new(_fixture.ConnectionStringFor("tenant_b"));

            Assert.Equal("tenant_a", (await tenantA.GetByIdAsync<TenantRow>(1, cancellationToken: Ct))!.Name);
            Assert.Equal("tenant_b", (await tenantB.GetByIdAsync<TenantRow>(1, cancellationToken: Ct))!.Name);
        }

        [Fact]
        public async Task AnEntityWithoutASchema_IsWrittenToTheDatabaseOfTheConnection()
        {
            await using MySqlConnection tenantA = new(_fixture.ConnectionStringFor("tenant_a"));
            await using MySqlConnection tenantB = new(_fixture.ConnectionStringFor("tenant_b"));

            await tenantB.InsertAsync(new TenantRow { Id = 2, Name = "written to tenant_b", Amount = 1 }, cancellationToken: Ct);

            Assert.NotNull(await tenantB.GetByIdAsync<TenantRow>(2, cancellationToken: Ct));
            Assert.Null(await tenantA.GetByIdAsync<TenantRow>(2, cancellationToken: Ct));
        }
    }
}
