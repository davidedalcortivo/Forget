using Forget.Oracle.Extensions;
using Oracle.ManagedDataAccess.Client;
using Testcontainers.Oracle;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// Starts one real Oracle container (via the official <c>Testcontainers.Oracle</c> module) for the lifetime
    /// of a test class. Creates a <c>"dbo"</c> user/schema (quoted and lowercase, matching <see cref="Widget"/>'s
    /// <c>[Table(Schema = "dbo")]</c> exactly — Oracle folds unquoted identifiers to uppercase, which would not
    /// match) owning the <c>"Widget"</c> table, then calls the real <c>LoadDbCacheAsync</c> so the column-cast
    /// PL/SQL block
    /// (<see cref="SqlBuilderStrategyTests.GetColumns_EmitsPlSqlBlockThatDiscoversColumnCastExpressions"/>) runs
    /// against an actual schema instead of hand-seeded metadata like in <see cref="InsertRangeCommandsTests"/>.
    /// </summary>
    public sealed class OracleFixture : IAsyncLifetime
    {
        private const string Password = "TestPwd_1";

        private OracleContainer _container = null!;

        public OracleConnection Connection { get; private set; } = null!;

        public string ConnectionString { get; private set; } = null!;

        public async ValueTask InitializeAsync()
        {
            _container = new OracleBuilder("gvenzl/oracle-xe:21.3.0-slim-faststart")
                .WithPassword(Password)
                .Build();

            await _container.StartAsync();

            // The container's own connection string logs in as "oracle" - an application user with no DBA
            // privileges, so it can't CREATE USER. Swap in "system" (whose password is the same ORACLE_PASSWORD),
            // and stay connected as it throughout rather than authenticating as "dbo" directly: Oracle's login
            // negotiation folds an unquoted User Id to uppercase, which would not match the case-sensitive
            // lowercase "dbo" user created below, and getting a quoted User Id right in a connection string is its
            // own can of worms. ALTER SESSION SET CURRENT_SCHEMA lets a DBA-privileged connection create objects
            // owned by another schema without ever logging in as that user.
            string connectionString = _container.GetConnectionString().Replace("User Id=oracle;", "User Id=system;");
            ConnectionString = connectionString;
            Connection = new OracleConnection(ConnectionString);
            await Connection.OpenAsync();

            await ExecuteAsync(Connection, $"CREATE USER \"dbo\" IDENTIFIED BY \"{Password}\"");
            await ExecuteAsync(Connection, "GRANT CONNECT, RESOURCE, UNLIMITED TABLESPACE TO \"dbo\"");
            await ExecuteAsync(Connection, "ALTER SESSION SET CURRENT_SCHEMA = \"dbo\"");

            // IsActive is NUMBER(1), populated from Widget.IsActive (int, not bool - see Widget for why): on the
            // Oracle XE 21.3 image this module defaults to, binding a C# bool against a NUMBER column throws
            // ORA-00932 ("NUMBER expected, got BOOLEAN"), a limitation absent on Oracle 23c.
            await ExecuteAsync(Connection, """
                CREATE TABLE "Widget" (
                    "Id" NUMBER(10) PRIMARY KEY,
                    "Name" VARCHAR2(255) NOT NULL,
                    "Nickname" VARCHAR2(255) NULL,
                    "Quantity" NUMBER(10) NULL,
                    "IsActive" NUMBER(1) NOT NULL,
                    "Price" NUMBER(18,2) NOT NULL
                )
                """);

            await ExecuteAsync(Connection, """
                CREATE TABLE "HandlerRow" (
                    "Id" NUMBER(10) PRIMARY KEY,
                    "Token" RAW(16) NOT NULL,
                    "Note" VARCHAR2(50) NULL
                )
                """);

            await ExecuteAsync(Connection, """
                CREATE TABLE "UpsertProduct" (
                    "Id" NUMBER(10) GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                    "Sku" VARCHAR2(50) NOT NULL UNIQUE,
                    "Name" VARCHAR2(100) NOT NULL,
                    "Stock" NUMBER(10) NOT NULL
                )
                """);

            await ExecuteAsync(Connection, """
                CREATE TABLE "PartialRow" (
                    "Id" NUMBER(10) PRIMARY KEY,
                    "Legacy" VARCHAR2(50) DEFAULT 'legacy' NOT NULL,
                    "Qty" NUMBER(10) NOT NULL,
                    "Audit" DATE NULL,
                    "Back\slash" NUMBER(1) NULL,
                    "Odd$Name" NUMBER(1) NULL
                )
                """);

            await ExecuteAsync(Connection, """
                CREATE TABLE "CastRow" (
                    "Id" NUMBER(10) PRIMARY KEY,
                    "Plain" NUMBER NULL,
                    "Scaled" NUMBER(*,2) NULL,
                    "Exact" NUMBER(18,4) NULL,
                    "Ratio" BINARY_DOUBLE NULL,
                    "Approx" FLOAT NULL,
                    "Body" CLOB NULL,
                    "Payload" BLOB NULL,
                    "Moment" TIMESTAMP(6) NULL,
                    "Day" DATE NULL,
                    "Code" CHAR(5) NULL,
                    "CharSemantic" CHAR(5 CHAR) NULL,
                    "National" NCHAR(5) NULL,
                    "Label" VARCHAR2(50) NULL,
                    "LabelChars" VARCHAR2(50 CHAR) NULL,
                    "Unicode" NVARCHAR2(20) NULL,
                    "Token" RAW(16) NULL
                )
                """);

            await Connection.LoadDbCacheAsync<Widget>();
            await Connection.LoadDbCacheAsync<HandlerRow>();
            await Connection.LoadDbCacheAsync<CastRow>();
        }

        private static async Task ExecuteAsync(OracleConnection connection, string sql)
        {
            await using OracleCommand command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Connection.DisposeAsync();
            await _container.DisposeAsync();
        }
    }

    /// <summary>
    /// Every Oracle integration test class shares one <see cref="OracleFixture"/>, so one container serves them all
    /// instead of one per class (an Oracle container is the heaviest of the four). The classes of a collection run
    /// one after the other, which is what keeps the number of containers alive at the same time at one per provider;
    /// tests stay apart by using their own tables or disjoint <c>Id</c> ranges.
    /// </summary>
    [CollectionDefinition(Name)]
    public sealed class OracleCollection : ICollectionFixture<OracleFixture>
    {
        public const string Name = "Oracle";
    }
}
