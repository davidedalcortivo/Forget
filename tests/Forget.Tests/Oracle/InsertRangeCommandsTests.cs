using Dapper;
using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Oracle.Extensions;
using Forget.Oracle.Strategies;
using Oracle.ManagedDataAccess.Client;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// Exercises the mechanism behind Oracle's multi-row insert/upsert: since Oracle has no
    /// <c>VALUES (...), (...), (...)</c> syntax, <c>BuildUpsertRangeCommands</c> emits one
    /// <c>SELECT ... FROM DUAL UNION ALL SELECT ... FROM DUAL ...</c> block per batch, casting each bound parameter
    /// to the column's real type using the <c>CastExpression</c> discovered by
    /// <see cref="SqlBuilderStrategyTests.GetColumns_EmitsPlSqlBlockThatDiscoversColumnCastExpressions"/>'s PL/SQL
    /// block — this is what avoids <c>ORA-01790</c> (datatype mismatch) on the <c>UNION ALL</c>.
    /// <para>
    /// No live Oracle connection is needed: the connection is only used to derive a cache key
    /// (<see cref="Forget.Core.Abstractions.Strategies.ISqlDialectStrategy.GetConnectionId"/> merely parses the connection string), so the column-cast
    /// metadata that would normally come from that PL/SQL block against a real schema is seeded directly into
    /// <see cref="DbColumnInfoCache{TEntity}"/> here instead.
    /// </para>
    /// </summary>
    public class InsertRangeCommandsTests
    {
        private static Dictionary<string, DbColumnInfo> BuildColumns()
        {
            return new()
            {
                ["Id"] = new DbColumnInfo { Name = "Id", DataType = "NUMBER", CastExpression = "CAST({} AS NUMBER(10))", IsCastable = true },
                ["Name"] = new DbColumnInfo { Name = "Name", DataType = "VARCHAR2", CastExpression = null, IsCastable = false },
                ["Nickname"] = new DbColumnInfo { Name = "Nickname", DataType = "VARCHAR2", CastExpression = null, IsCastable = false },
                ["Quantity"] = new DbColumnInfo { Name = "Quantity", DataType = "NUMBER", CastExpression = "CAST({} AS NUMBER(10))", IsCastable = true },
                ["IsActive"] = new DbColumnInfo { Name = "IsActive", DataType = "NUMBER", CastExpression = "CAST({} AS NUMBER(1))", IsCastable = true },
                ["Price"] = new DbColumnInfo { Name = "Price", DataType = "NUMBER", CastExpression = "CAST({} AS NUMBER(18,2))", IsCastable = true },
            };
        }

        private static string Row(int index)
        {
            return $"SELECT CAST(:Id{index} AS NUMBER(10)) AS \"Id\", :Name{index} AS \"Name\", :Nickname{index} AS \"Nickname\", " +
                   $"CAST(:Quantity{index} AS NUMBER(10)) AS \"Quantity\", CAST(:IsActive{index} AS NUMBER(1)) AS \"IsActive\", " +
                   $"CAST(:Price{index} AS NUMBER(18,2)) AS \"Price\" FROM DUAL";
        }

        private static string Golden(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        [Fact]
        public void InsertRangeCommands_BatchesRowsAndAppliesColumnCastExpressions()
        {
            // A fresh, never-opened connection is enough: BuildUpsertRangeCommands only reads its connection
            // string to build a cache key, it never queries the database.
            OracleConnection connection = new("Data Source=insert-range-test;User Id=test;Password=test;");

            string connectionId = SqlDialectStrategy.Instance.GetConnectionId(connection);
            DbColumnInfoCache<Widget>.Add(connectionId, BuildColumns());
            SqlBuilderCache<Widget, SqlBuilderStrategy>.Initialize(SqlBuilderStrategy.Instance);

            Widget[] widgets =
            [
                new() { Id = 1, Name = "A", Nickname = null, Quantity = 5, IsActive = 1, Price = 9.99m },
                new() { Id = 2, Name = "B", Nickname = "b-nick", Quantity = null, IsActive = 0, Price = 19.99m },
                new() { Id = 3, Name = "C", Nickname = "c-nick", Quantity = 7, IsActive = 1, Price = 29.99m },
            ];

            IReadOnlyList<DbCommandInfo> commands = connection.InsertRangeCommands(widgets, batchSize: 2);

            Assert.Equal(2, commands.Count);

            Assert.Equal(Golden(
                "INSERT INTO \"dbo\".\"Widget\" (",
                "    \"Id\",",
                "    \"Name\",",
                "    \"Nickname\",",
                "    \"Quantity\",",
                "    \"IsActive\",",
                "    \"Price\"",
                ")",
                Row(0),
                "UNION ALL",
                Row(1)), commands[0].Sql);

            DynamicParameters batch0 = commands[0].Parameters!;
            Assert.Equal(1, batch0.Get<int>("Id0"));
            Assert.Equal("A", batch0.Get<string>("Name0"));
            Assert.Null(batch0.Get<string?>("Nickname0"));
            Assert.Equal(5, batch0.Get<int>("Quantity0"));
            Assert.Equal(1, batch0.Get<int>("IsActive0"));
            Assert.Equal(9.99m, batch0.Get<decimal>("Price0"));
            Assert.Equal(2, batch0.Get<int>("Id1"));
            Assert.Equal("B", batch0.Get<string>("Name1"));
            Assert.Equal("b-nick", batch0.Get<string>("Nickname1"));
            Assert.Null(batch0.Get<int?>("Quantity1"));
            Assert.Equal(0, batch0.Get<int>("IsActive1"));
            Assert.Equal(19.99m, batch0.Get<decimal>("Price1"));

            Assert.Equal(Golden(
                "INSERT INTO \"dbo\".\"Widget\" (",
                "    \"Id\",",
                "    \"Name\",",
                "    \"Nickname\",",
                "    \"Quantity\",",
                "    \"IsActive\",",
                "    \"Price\"",
                ")",
                Row(2)), commands[1].Sql);

            DynamicParameters batch1 = commands[1].Parameters!;
            Assert.Equal(3, batch1.Get<int>("Id2"));
            Assert.Equal("C", batch1.Get<string>("Name2"));
            Assert.Equal("c-nick", batch1.Get<string>("Nickname2"));
            Assert.Equal(7, batch1.Get<int>("Quantity2"));
            Assert.Equal(1, batch1.Get<int>("IsActive2"));
            Assert.Equal(29.99m, batch1.Get<decimal>("Price2"));
        }

        [Fact]
        public void UpsertRangeCommands_IndentsRowsWithinMergeUsingClause()
        {
            OracleConnection connection = new("Data Source=upsert-range-test;User Id=test;Password=test;");

            string connectionId = SqlDialectStrategy.Instance.GetConnectionId(connection);
            DbColumnInfoCache<Widget>.Add(connectionId, BuildColumns());
            SqlBuilderCache<Widget, SqlBuilderStrategy>.Initialize(SqlBuilderStrategy.Instance);

            Widget[] widgets = [new() { Id = 1, Name = "A", Nickname = null, Quantity = 5, IsActive = 1, Price = 9.99m }];

            IReadOnlyList<DbCommandInfo> commands = connection.UpsertRangeCommands(widgets, batchSize: 500);

            DbCommandInfo item = Assert.Single(commands);

            Assert.Equal(Golden(
                "MERGE INTO \"dbo\".\"Widget\" \"TARGET\"",
                "USING (",
                "    " + Row(0),
                ") \"SOURCE\"",
                "ON (",
                "    (",
                "        \"TARGET\".\"Id\" = \"SOURCE\".\"Id\"",
                "        OR (\"TARGET\".\"Id\" IS NULL AND \"SOURCE\".\"Id\" IS NULL)",
                "    )",
                ")",
                "WHEN MATCHED THEN",
                "    UPDATE SET",
                "        \"TARGET\".\"Name\" = \"SOURCE\".\"Name\",",
                "        \"TARGET\".\"Nickname\" = \"SOURCE\".\"Nickname\",",
                "        \"TARGET\".\"Quantity\" = \"SOURCE\".\"Quantity\",",
                "        \"TARGET\".\"IsActive\" = \"SOURCE\".\"IsActive\",",
                "        \"TARGET\".\"Price\" = \"SOURCE\".\"Price\"",
                "WHEN NOT MATCHED THEN",
                "    INSERT (",
                "        \"Id\",",
                "        \"Name\",",
                "        \"Nickname\",",
                "        \"Quantity\",",
                "        \"IsActive\",",
                "        \"Price\"",
                "    )",
                "    VALUES (",
                "        \"SOURCE\".\"Id\",",
                "        \"SOURCE\".\"Name\",",
                "        \"SOURCE\".\"Nickname\",",
                "        \"SOURCE\".\"Quantity\",",
                "        \"SOURCE\".\"IsActive\",",
                "        \"SOURCE\".\"Price\"",
                "    )"), item.Sql);
        }
    }
}
