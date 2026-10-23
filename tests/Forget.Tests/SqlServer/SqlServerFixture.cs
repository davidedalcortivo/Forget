using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// Starts one real SqlServer container (via Testcontainers) for the lifetime of a test class, and creates the
    /// <c>dbo.Widget</c> table the integration tests run against. No explicit schema creation is needed: every
    /// SqlServer database already has a <c>dbo</c> schema by default, matching
    /// <see cref="Widget"/>'s <c>Schema = "dbo"</c>.
    /// </summary>
    public sealed class SqlServerFixture : IAsyncLifetime
    {
        private MsSqlContainer _container = null!;

        public SqlConnection Connection { get; private set; } = null!;

        public string ConnectionString { get; private set; } = null!;

        /// <summary>A login whose user has <c>tenant_a</c> as its default schema.</summary>
        public string TenantConnectionString { get; private set; } = null!;

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

                CREATE TABLE dbo.StringIdRow (
                    Code NVARCHAR(50) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL
                );

                CREATE TABLE dbo.BinaryIdRow (
                    Id VARBINARY(16) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL
                );

                CREATE TABLE dbo.NumericRow (
                    Id INT PRIMARY KEY,
                    DoubleValue FLOAT NOT NULL,
                    SingleValue REAL NOT NULL
                );

                CREATE TABLE dbo.ParameterNameRow (
                    Id INT PRIMARY KEY,
                    Address NVARCHAR(50) NOT NULL,
                    Address2 NVARCHAR(50) NOT NULL
                );

                CREATE TABLE dbo.ByteListRow (Id INT PRIMARY KEY, Code TINYINT NOT NULL, Status TINYINT NOT NULL);
                INSERT INTO dbo.ByteListRow VALUES (1, 1, 1), (2, 2, 2), (3, 3, 3);

                CREATE TABLE dbo.AggregateRow (Id INT PRIMARY KEY, Quantity INT NOT NULL, Total BIGINT NOT NULL, Ratio FLOAT NOT NULL);
                INSERT INTO dbo.AggregateRow VALUES (1, 10, 100, 0.5), (2, 20, 200, 1.5), (3, 30, 300, 2.5);

                CREATE TABLE dbo.LargeIntRow (Id INT PRIMARY KEY, Quantity INT NOT NULL, Kind INT NOT NULL);
                INSERT INTO dbo.LargeIntRow VALUES (1, 2000000000, 1), (2, 2000000000, 1), (3, 5, 2);

                CREATE TABLE dbo.RegionalStock (Id INT IDENTITY(1,1) PRIMARY KEY, Sku NVARCHAR(50) NOT NULL, Region NVARCHAR(50) NOT NULL, Stock INT NOT NULL, UNIQUE (Sku, Region));
                """;
            await command.ExecuteNonQueryAsync();

            // CREATE SCHEMA has to be the only statement in its batch.
            await using SqlCommand schemaCommand = Connection.CreateCommand();
            schemaCommand.CommandText = "CREATE SCHEMA [Odd Schema#1]";
            await schemaCommand.ExecuteNonQueryAsync();

            await using SqlCommand oddNameCommand = Connection.CreateCommand();
            oddNameCommand.CommandText = """
                CREATE TABLE [Odd Schema#1].[Order]] Details] (
                    [Id] INT PRIMARY KEY,
                    [Line Name] NVARCHAR(50) NOT NULL,
                    [Città] NVARCHAR(50) NOT NULL,
                    [Code#$] INT NOT NULL,
                    [2nd-Value] INT NOT NULL,
                    [_Flag] INT NOT NULL,
                    [Back\slash] NVARCHAR(50) NOT NULL,
                    [Email@Work:Port?Cost$] NVARCHAR(50) NOT NULL,
                    [Dot.Name] NVARCHAR(50) NOT NULL,
                    [Bracket]]Name] NVARCHAR(50) NOT NULL
                );
                """;
            await oddNameCommand.ExecuteNonQueryAsync();

            // The same table in dbo and in tenant_a, with different rows, a table only in dbo and one only in tenant_a: a user
            // whose default schema is tenant_a reads tenant_a's table, and dbo's when tenant_a has none, as SQL Server
            // resolves a name.
            await using SqlCommand tenantSchemaCommand = Connection.CreateCommand();
            tenantSchemaCommand.CommandText = "CREATE SCHEMA tenant_a";
            await tenantSchemaCommand.ExecuteNonQueryAsync();

            const string tenantPassword = "Tenant#Passw0rd";
            await using SqlCommand tenantCommand = Connection.CreateCommand();
            tenantCommand.CommandText = $"""
                CREATE TABLE dbo.TenantRow (Id INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL, Amount INT NOT NULL);
                INSERT INTO dbo.TenantRow VALUES (1, 'dbo', 10);
                CREATE TABLE tenant_a.TenantRow (Id INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL, Amount INT NOT NULL);
                INSERT INTO tenant_a.TenantRow VALUES (1, 'tenant_a', 20);
                CREATE TABLE dbo.DboOnlyRow (Id INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL, Amount INT NOT NULL);
                INSERT INTO dbo.DboOnlyRow VALUES (1, 'dbo only', 5);
                CREATE TABLE tenant_a.TenantOnlyRow (Id INT PRIMARY KEY, Name NVARCHAR(50) NOT NULL, Amount INT NOT NULL);
                INSERT INTO tenant_a.TenantOnlyRow VALUES (1, 'tenant_a only', 7);

                CREATE LOGIN tenant_login WITH PASSWORD = '{tenantPassword}', CHECK_POLICY = OFF;
                CREATE USER tenant_user FOR LOGIN tenant_login WITH DEFAULT_SCHEMA = tenant_a;
                GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::tenant_a TO tenant_user;
                GRANT SELECT ON SCHEMA::dbo TO tenant_user;
                """;
            await tenantCommand.ExecuteNonQueryAsync();

            TenantConnectionString = new SqlConnectionStringBuilder(ConnectionString) { UserID = "tenant_login", Password = tenantPassword }.ConnectionString;
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
