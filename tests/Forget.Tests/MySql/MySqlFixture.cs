using MySqlConnector;
using Testcontainers.MySql;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// Starts one real MySql container (via Testcontainers) for the lifetime of a test class, and creates the
    /// <c>Widget</c> table the integration tests run against. The container's database is named <c>dbo</c> so it
    /// matches <see cref="Widget"/>'s <c>[Table(Schema = "dbo")]</c> — MySql treats "schema" and
    /// "database" as the same thing.
    /// </summary>
    public sealed class MySqlFixture : IAsyncLifetime
    {
        private MySqlContainer _container = null!;

        public MySqlConnection Connection { get; private set; } = null!;

        public string ConnectionString { get; private set; } = null!;

        public async ValueTask InitializeAsync()
        {
            _container = new MySqlBuilder("mysql:8.4")
                .WithDatabase("dbo")
                .Build();

            await _container.StartAsync();

            ConnectionString = _container.GetConnectionString();
            Connection = new MySqlConnection(ConnectionString);
            await Connection.OpenAsync();

            await using MySqlCommand command = Connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE `Widget` (
                    `Id` INT PRIMARY KEY,
                    `Name` VARCHAR(255) NOT NULL,
                    `Nickname` VARCHAR(255) NULL,
                    `Quantity` INT NULL,
                    `IsActive` TINYINT(1) NOT NULL,
                    `Price` DECIMAL(18,2) NOT NULL
                );
                """;
            await command.ExecuteNonQueryAsync();

            await using MySqlCommand typeMatrixCommand = Connection.CreateCommand();
            typeMatrixCommand.CommandText = """
                CREATE TABLE `TypeMatrix` (
                    `Id` INT PRIMARY KEY,
                    `BigValue` BIGINT NOT NULL,
                    `SmallValue` SMALLINT NOT NULL,
                    `DoubleValue` DOUBLE NOT NULL,
                    `SingleValue` FLOAT NOT NULL,
                    `DecimalValue` DECIMAL(28,10) NOT NULL,
                    `Flag` TINYINT(1) NOT NULL,
                    `Label` VARCHAR(200) CHARACTER SET utf8mb4 NOT NULL,
                    `Moment` DATETIME(6) NOT NULL,
                    `CalendarDay` DATE NOT NULL,
                    `TimeOfDay` TIME(6) NOT NULL,
                    `Identifier` CHAR(36) NOT NULL UNIQUE,
                    `Kind` INT NOT NULL,
                    `Payload` VARBINARY(200) NOT NULL,
                    `NullableBig` BIGINT NULL,
                    `NullableMoment` DATETIME(6) NULL,
                    `NullableGuid` CHAR(36) NULL,
                    `NullableKind` INT NULL,
                    `NullableLabel` VARCHAR(200) CHARACTER SET utf8mb4 NULL
                );
                """;
            await typeMatrixCommand.ExecuteNonQueryAsync();

            await using MySqlCommand upsertCommand = Connection.CreateCommand();
            upsertCommand.CommandText = """
                CREATE TABLE `UpsertProduct` (
                    `Id` INT AUTO_INCREMENT PRIMARY KEY,
                    `Sku` VARCHAR(50) NOT NULL UNIQUE,
                    `Name` VARCHAR(100) NOT NULL,
                    `Stock` INT NOT NULL
                );
                """;
            await upsertCommand.ExecuteNonQueryAsync();

            await using MySqlCommand keyedCommand = Connection.CreateCommand();
            keyedCommand.CommandText = """
                CREATE TABLE `StringKeyed` (
                    `Code` VARCHAR(50) PRIMARY KEY,
                    `Name` VARCHAR(100) NOT NULL
                );

                CREATE TABLE `BinaryKeyed` (
                    `Id` VARBINARY(16) PRIMARY KEY,
                    `Name` VARCHAR(100) NOT NULL
                );

                CREATE TABLE `NumericRow` (
                    `Id` INT PRIMARY KEY,
                    `DoubleValue` DOUBLE NOT NULL,
                    `SingleValue` FLOAT NOT NULL
                );
                """;
            await keyedCommand.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Every MySql integration test class shares one <see cref="MySqlFixture"/>, so one container serves them all
    /// instead of one per class. The classes of a collection run one after the other, which is what keeps the
    /// number of containers alive at the same time at one per provider; tests stay apart by using their own tables
    /// or disjoint <c>Id</c> ranges.
    /// </summary>
    [CollectionDefinition(Name)]
    public sealed class MySqlCollection : ICollectionFixture<MySqlFixture>
    {
        public const string Name = "MySql";
    }
}
