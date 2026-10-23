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

        /// <summary>The fixture's connection string with another database (<c>tenant_a</c>, <c>tenant_b</c>).</summary>
        public string ConnectionStringFor(string database) => new MySqlConnectionStringBuilder(ConnectionString) { Database = database }.ConnectionString;

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
                CREATE TABLE `StringIdRow` (
                    `Code` VARCHAR(50) PRIMARY KEY,
                    `Name` VARCHAR(100) NOT NULL
                );

                CREATE TABLE `BinaryIdRow` (
                    `Id` VARBINARY(16) PRIMARY KEY,
                    `Name` VARCHAR(100) NOT NULL
                );

                CREATE TABLE `NumericRow` (
                    `Id` INT PRIMARY KEY,
                    `DoubleValue` DOUBLE NOT NULL,
                    `SingleValue` FLOAT NOT NULL
                );

                CREATE TABLE `ParameterNameRow` (
                    `Id` INT PRIMARY KEY,
                    `Address` VARCHAR(50) NOT NULL,
                    `Address2` VARCHAR(50) NOT NULL
                );

                CREATE TABLE `ByteListRow` (`Id` INT PRIMARY KEY, `Code` TINYINT UNSIGNED NOT NULL, `Status` TINYINT UNSIGNED NOT NULL);
                INSERT INTO `ByteListRow` VALUES (1, 1, 1), (2, 2, 2), (3, 3, 3);

                CREATE TABLE `AggregateRow` (`Id` INT PRIMARY KEY, `Quantity` INT NOT NULL, `Total` BIGINT NOT NULL, `Ratio` DOUBLE NOT NULL);
                INSERT INTO `AggregateRow` VALUES (1, 10, 100, 0.5), (2, 20, 200, 1.5), (3, 30, 300, 2.5);
                """;
            await keyedCommand.ExecuteNonQueryAsync();

            // Only root can create another database; the container gives root the same password as its own user.
            MySqlConnectionStringBuilder root = new(ConnectionString) { UserID = "root" };
            await using MySqlConnection rootConnection = new(root.ConnectionString);
            await rootConnection.OpenAsync();
            await using MySqlCommand oddNameCommand = rootConnection.CreateCommand();
            oddNameCommand.CommandText = $"""
                CREATE DATABASE `odd-schema`;
                GRANT ALL ON `odd-schema`.* TO '{new MySqlConnectionStringBuilder(ConnectionString).UserID}'@'%';
                CREATE TABLE `odd-schema`.`Order ``Details` (
                    `Id` INT PRIMARY KEY,
                    `Line Name` VARCHAR(50) NOT NULL,
                    `Città` VARCHAR(50) NOT NULL,
                    `Code#$` INT NOT NULL,
                    `2nd-Value` INT NOT NULL,
                    `_Flag` INT NOT NULL,
                    `Back\slash` VARCHAR(50) NOT NULL,
                    `Email@Work:Port?Cost$` VARCHAR(50) NOT NULL,
                    `Dot.Name` VARCHAR(50) NOT NULL,
                    `Tick``Name` VARCHAR(50) NOT NULL
                );
                """;
            await oddNameCommand.ExecuteNonQueryAsync();

            // A database per tenant, each with its own TenantRow: a connection reads and writes its own database's table.
            string user = new MySqlConnectionStringBuilder(ConnectionString).UserID;
            await using MySqlCommand tenantCommand = rootConnection.CreateCommand();
            tenantCommand.CommandText = $"""
                CREATE DATABASE `tenant_a`;
                CREATE DATABASE `tenant_b`;
                GRANT ALL ON `tenant_a`.* TO '{user}'@'%';
                GRANT ALL ON `tenant_b`.* TO '{user}'@'%';
                CREATE TABLE `tenant_a`.`TenantRow` (`Id` INT PRIMARY KEY, `Name` VARCHAR(50) NOT NULL, `Amount` INT NOT NULL);
                INSERT INTO `tenant_a`.`TenantRow` VALUES (1, 'tenant_a', 10);
                CREATE TABLE `tenant_b`.`TenantRow` (`Id` INT PRIMARY KEY, `Name` VARCHAR(50) NOT NULL, `Amount` INT NOT NULL);
                INSERT INTO `tenant_b`.`TenantRow` VALUES (1, 'tenant_b', 20);
                """;
            await tenantCommand.ExecuteNonQueryAsync();
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
