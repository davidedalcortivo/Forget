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

                CREATE TABLE dbo."EnumKeyed" ("Id" INTEGER PRIMARY KEY, "Name" TEXT NOT NULL);
                CREATE TABLE dbo."GuidKeyed" ("Id" UUID PRIMARY KEY, "Name" TEXT NOT NULL);
                CREATE TABLE dbo."StringKeyed" ("Code" TEXT PRIMARY KEY, "Name" TEXT NOT NULL);
                CREATE TABLE dbo."BinaryKeyed" ("Id" BYTEA PRIMARY KEY, "Name" TEXT NOT NULL);
                CREATE TABLE dbo."NumericRow" ("Id" INTEGER PRIMARY KEY, "DoubleValue" DOUBLE PRECISION NOT NULL, "SingleValue" REAL NOT NULL);
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
