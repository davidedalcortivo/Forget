using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// Starts one real SqlServer container (via Testcontainers) for the lifetime of a test class, and creates the
    /// <c>dbo.Widget</c> table the integration tests run against. No explicit schema creation is needed: every
    /// SqlServer database already has a <c>dbo</c> schema by default, matching
    /// <see cref="Widget"/>'s <c>[Table(Schema = "dbo")]</c>.
    /// </summary>
    public sealed class SqlServerFixture : IAsyncLifetime
    {
        private MsSqlContainer _container = null!;

        public SqlConnection Connection { get; private set; } = null!;

        public string ConnectionString { get; private set; } = null!;

        public async ValueTask InitializeAsync()
        {
            _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
                .Build();

            await _container.StartAsync();

            ConnectionString = _container.GetConnectionString();
            Connection = new SqlConnection(ConnectionString);
            await Connection.OpenAsync();

            await using SqlCommand command = Connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE dbo.Widget (
                    Id INT PRIMARY KEY,
                    Name NVARCHAR(255) NOT NULL,
                    Nickname NVARCHAR(255) NULL,
                    Quantity INT NULL,
                    IsActive BIT NOT NULL,
                    Price DECIMAL(18,2) NOT NULL
                );

                CREATE TABLE dbo.PartialRow (
                    Id INT PRIMARY KEY,
                    Legacy NVARCHAR(50) NOT NULL DEFAULT 'legacy',
                    Qty INT NOT NULL,
                    Audit DATETIME2 NULL
                );

                CREATE TABLE dbo.UpsertProduct (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    Sku NVARCHAR(50) NOT NULL UNIQUE,
                    Name NVARCHAR(100) NOT NULL,
                    Stock INT NOT NULL
                );

                CREATE TABLE dbo.TypeMatrix (
                    Id INT PRIMARY KEY,
                    BigValue BIGINT NOT NULL,
                    SmallValue SMALLINT NOT NULL,
                    DoubleValue FLOAT NOT NULL,
                    SingleValue REAL NOT NULL,
                    DecimalValue DECIMAL(28,10) NOT NULL,
                    Flag BIT NOT NULL,
                    Label NVARCHAR(200) NOT NULL,
                    Moment DATETIME2(6) NOT NULL,
                    OffsetMoment DATETIMEOFFSET(6) NOT NULL,
                    CalendarDay DATE NOT NULL,
                    TimeOfDay TIME(6) NOT NULL,
                    Identifier UNIQUEIDENTIFIER NOT NULL UNIQUE,
                    Kind INT NOT NULL,
                    Payload VARBINARY(200) NOT NULL,
                    NullableBig BIGINT NULL,
                    NullableMoment DATETIME2(6) NULL,
                    NullableGuid UNIQUEIDENTIFIER NULL,
                    NullableKind INT NULL,
                    NullableLabel NVARCHAR(200) NULL
                );
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
    /// Every SqlServer integration test class shares one <see cref="SqlServerFixture"/>, so one container serves them
    /// all instead of one per class. The classes of a collection run one after the other, which is what keeps the
    /// number of containers alive at the same time at one per provider; tests stay apart by using their own tables
    /// or disjoint <c>Id</c> ranges.
    /// </summary>
    [CollectionDefinition(Name)]
    public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
    {
        public const string Name = "SqlServer";
    }
}
