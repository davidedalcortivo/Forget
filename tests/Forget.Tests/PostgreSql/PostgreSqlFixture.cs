using Npgsql;
using Testcontainers.PostgreSql;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// Starts one real PostgreSql container (via Testcontainers) for the lifetime of a test class, and creates the
    /// <c>dbo."Widget"</c> table the integration tests run against. Shared across every test in the class rather
    /// than started per test, since spinning up a container is the expensive part.
    /// </summary>
    public sealed class PostgreSqlFixture : IAsyncLifetime
    {
        private PostgreSqlContainer _container = null!;

        public NpgsqlConnection Connection { get; private set; } = null!;

        public string ConnectionString { get; private set; } = null!;

        /// <summary>The fixture's connection string with a search_path (<c>tenant_a</c>, <c>tenant_b</c>).</summary>
        public string ConnectionStringFor(string searchPath) => new NpgsqlConnectionStringBuilder(ConnectionString) { SearchPath = searchPath }.ConnectionString;

        public async ValueTask InitializeAsync()
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .Build();

            await _container.StartAsync();

            ConnectionString = _container.GetConnectionString();
            Connection = new NpgsqlConnection(ConnectionString);
            await Connection.OpenAsync();

            await using NpgsqlCommand command = Connection.CreateCommand();
            command.CommandText = """
                CREATE SCHEMA IF NOT EXISTS dbo;
                CREATE TABLE dbo."Widget" (
                    "Id" INTEGER PRIMARY KEY,
                    "Name" TEXT NOT NULL,
                    "Nickname" TEXT NULL,
                    "Quantity" INTEGER NULL,
                    "IsActive" BOOLEAN NOT NULL,
                    "Price" NUMERIC(18,2) NOT NULL
                );

                CREATE TABLE dbo."PartialRow" (
                    "Id" INTEGER PRIMARY KEY,
                    "Legacy" TEXT NOT NULL DEFAULT 'legacy',
                    "Qty" INTEGER NOT NULL,
                    "Audit" TIMESTAMP NULL
                );

                CREATE TABLE dbo."UpsertProduct" (
                    "Id" INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    "Sku" TEXT NOT NULL UNIQUE,
                    "Name" TEXT NOT NULL,
                    "Stock" INTEGER NOT NULL
                );

                CREATE TABLE dbo."TypeMatrix" (
                    "Id" INTEGER PRIMARY KEY,
                    "BigValue" BIGINT NOT NULL,
                    "SmallValue" SMALLINT NOT NULL,
                    "DoubleValue" DOUBLE PRECISION NOT NULL,
                    "SingleValue" REAL NOT NULL,
                    "DecimalValue" NUMERIC(28,10) NOT NULL,
                    "Flag" BOOLEAN NOT NULL,
                    "Label" TEXT NOT NULL,
                    "Moment" TIMESTAMP(6) NOT NULL,
                    "OffsetMoment" TIMESTAMPTZ(6) NOT NULL,
                    "CalendarDay" DATE NOT NULL,
                    "TimeOfDay" TIME(6) NOT NULL,
                    "Identifier" UUID NOT NULL UNIQUE,
                    "Kind" INTEGER NOT NULL,
                    "Payload" BYTEA NOT NULL,
                    "NullableBig" BIGINT NULL,
                    "NullableMoment" TIMESTAMP(6) NULL,
                    "NullableGuid" UUID NULL,
                    "NullableKind" INTEGER NULL,
                    "NullableLabel" TEXT NULL
                );

                CREATE TABLE dbo."EnumIdRow" ("Id" INTEGER PRIMARY KEY, "Name" TEXT NOT NULL);
                CREATE TABLE dbo."GuidIdRow" ("Id" UUID PRIMARY KEY, "Name" TEXT NOT NULL);
                CREATE TABLE dbo."StringIdRow" ("Code" TEXT PRIMARY KEY, "Name" TEXT NOT NULL);
                CREATE TABLE dbo."BinaryIdRow" ("Id" BYTEA PRIMARY KEY, "Name" TEXT NOT NULL);
                CREATE TABLE dbo."NumericRow" ("Id" INTEGER PRIMARY KEY, "DoubleValue" DOUBLE PRECISION NOT NULL, "SingleValue" REAL NOT NULL);
                CREATE TABLE dbo."ParameterNameRow" ("Id" INTEGER PRIMARY KEY, "Address" TEXT NOT NULL, "Address2" TEXT NOT NULL);
                CREATE TABLE dbo."ByteListRow" ("Id" INTEGER PRIMARY KEY, "Code" SMALLINT NOT NULL, "Status" SMALLINT NOT NULL);
                INSERT INTO dbo."ByteListRow" VALUES (1, 1, 1), (2, 2, 2), (3, 3, 3);
                CREATE TABLE dbo."AggregateRow" ("Id" INTEGER PRIMARY KEY, "Quantity" INTEGER NOT NULL, "Total" BIGINT NOT NULL, "Ratio" DOUBLE PRECISION NOT NULL);
                INSERT INTO dbo."AggregateRow" VALUES (1, 10, 100, 0.5), (2, 20, 200, 1.5), (3, 30, 300, 2.5);
                CREATE SCHEMA "Odd ""Schema";
                CREATE TABLE "Odd ""Schema"."Order ""Details" (
                    "Id" INTEGER PRIMARY KEY,
                    "Line Name" TEXT NOT NULL,
                    "Città" TEXT NOT NULL,
                    "Code#$" INTEGER NOT NULL,
                    "2nd-Value" INTEGER NOT NULL,
                    "_Flag" INTEGER NOT NULL,
                    "Back\slash" TEXT NOT NULL,
                    "Email@Work:Port?Cost$" TEXT NOT NULL,
                    "Dot.Name" TEXT NOT NULL,
                    "Quote""Name" TEXT NOT NULL
                );

                -- A schema per tenant, chosen with the search_path. The columns are in a different order in each, so a
                -- statement built from one tenant's columns would write the other's in the wrong place.
                CREATE SCHEMA tenant_a;
                CREATE SCHEMA tenant_b;
                CREATE TABLE tenant_a."TenantRow" ("Id" INTEGER PRIMARY KEY, "Name" TEXT NOT NULL, "Amount" INTEGER NOT NULL);
                INSERT INTO tenant_a."TenantRow" VALUES (1, 'tenant_a', 10), (2, 'tenant_a', 10);
                CREATE TABLE tenant_b."TenantRow" ("Id" INTEGER PRIMARY KEY, "Amount" INTEGER NOT NULL, "Name" TEXT NOT NULL);
                INSERT INTO tenant_b."TenantRow" VALUES (1, 20, 'tenant_b'), (2, 20, 'tenant_b');
                """;
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Every PostgreSql integration test class shares one <see cref="PostgreSqlFixture"/>, so one container serves them
    /// all instead of one per class. The classes of a collection run one after the other, which is what keeps the
    /// number of containers alive at the same time at one per provider; tests stay apart by using their own tables
    /// or disjoint <c>Id</c> ranges.
    /// </summary>
    [CollectionDefinition(Name)]
    public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>
    {
        public const string Name = "PostgreSql";
    }
}
