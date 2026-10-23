namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// Locks the exact SQL text produced by PostgreSql's <c>SqlBuilderStrategy</c> — <c>LIMIT</c> pagination,
    /// <c>= ANY(...)</c> instead of <c>IN</c> for range filters, <c>ON CONFLICT (...) DO UPDATE SET</c> with the
    /// <c>EXCLUDED</c> pseudo-table for upsert, the <c>(row).column</c> composite-type unpacking
    /// <c>UpdateRangeSqlBuilder</c> uses to destructure a <c>VALUES</c> row, and the <c>::text</c> cast operator for
    /// <c>Avg</c> instead of a <c>CAST</c> function call.
    /// <para>
    /// Placeholder positions are filled with distinct marker tokens rather than realistic SQL fragments — these
    /// tests lock where a builder places its placeholders and what literal text surrounds them, not what a real
    /// predicate/value list looks like.
    /// </para>
    /// </summary>
    public class SqlBuilderStrategyTests
    {
        private static readonly Forget.PostgreSql.Strategies.SqlBuilderStrategy _strategy = Forget.PostgreSql.Strategies.SqlBuilderStrategy.Instance;

        private static string Golden(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        [Fact]
        public void GetColumns_QueriesPgCatalogForLiveColumnMetadata()
        {
            string sql = _strategy.GetColumnsSqlBuilder<Widget>().Render("<<SCHEMA>>", "<<TABLE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    \"a\".\"attname\" AS \"Name\",",
                "    \"a\".\"attnum\" AS \"OrdinalPosition\"",
                "FROM",
                "    \"pg_attribute\" AS \"a\"",
                "WHERE",
                "    \"a\".\"attrelid\" = to_regclass(COALESCE(quote_ident(<<SCHEMA>>) || '.', '') || quote_ident(<<TABLE>>))",
                "    AND \"a\".\"attnum\" > 0",
                "    AND NOT \"a\".\"attisdropped\"",
                "ORDER BY",
                "    \"a\".\"attnum\";"), sql);
        }

        [Fact]
        public void GetAll_SelectsAllColumnsWithWhereSlot()
        {
            string sql = _strategy.GetAllSqlBuilder<Widget>().Render("<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    \"Id\",",
                "    \"Name\",",
                "    \"Nickname\",",
                "    \"Quantity\",",
                "    \"IsActive\",",
                "    \"Price\"",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>;"), sql);
        }

        [Fact]
        public void GetFirst_UsesLimitAfterWhereSlot()
        {
            string sql = _strategy.GetFirstSqlBuilder<Widget>().Render("<<WHERE>>", "<<TAKE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    \"Id\",",
                "    \"Name\",",
                "    \"Nickname\",",
                "    \"Quantity\",",
                "    \"IsActive\",",
                "    \"Price\"",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>",
                "LIMIT",
                "    <<TAKE>>;"), sql);
        }

        [Fact]
        public void GetById_FiltersOnIdentifierColumn()
        {
            string sql = _strategy.GetByIdSqlBuilder<Widget>().Render("<<ID>>");

            Assert.Equal(Golden(
                "SELECT",
                "    \"Id\",",
                "    \"Name\",",
                "    \"Nickname\",",
                "    \"Quantity\",",
                "    \"IsActive\",",
                "    \"Price\"",
                "FROM",
                "    \"dbo\".\"Widget\"",
                "WHERE",
                "    \"Id\" = <<ID>>;"), sql);
        }

        [Fact]
        public void Update_SetsColumnsBySchemaQualifiedTable()
        {
            string sql = _strategy.UpdateSqlBuilder<Widget>().Render("<<SET>>");

            Assert.Equal(Golden(
                "UPDATE \"dbo\".\"Widget\"",
                "SET",
                "<<SET>>;"), sql);
        }

        [Fact]
        public void Insert_ListsColumnsThenSingleValuesRow()
        {
            string sql = _strategy.InsertSqlBuilder<Widget>().Render("<<VALUES>>");

            Assert.Equal(Golden(
                "INSERT INTO \"dbo\".\"Widget\" (",
                "    \"Id\",",
                "    \"Name\",",
                "    \"Nickname\",",
                "    \"Quantity\",",
                "    \"IsActive\",",
                "    \"Price\"",
                ")",
                "VALUES (",
                "<<VALUES>>",
                ");"), sql);
        }

        [Fact]
        public void Delete_HasWhereSlotRightAfterTableName()
        {
            string sql = _strategy.DeleteSqlBuilder<Widget>().Render("<<WHERE>>");

            Assert.Equal(Golden("DELETE FROM \"dbo\".\"Widget\"<<WHERE>>;"), sql);
        }

        [Fact]
        public void Upsert_UsesOnConflictDoUpdateWithExcluded()
        {
            string sql = _strategy.UpsertSqlBuilder<Widget>().Render("<<VALUES>>");

            Assert.Equal(Golden(
                "INSERT INTO \"dbo\".\"Widget\" (",
                "    \"Id\",",
                "    \"Name\",",
                "    \"Nickname\",",
                "    \"Quantity\",",
                "    \"IsActive\",",
                "    \"Price\"",
                ")",
                "VALUES (",
                "<<VALUES>>",
                ")",
                "ON CONFLICT (\"Id\") DO UPDATE SET",
                "    \"Name\" = EXCLUDED.\"Name\",",
                "    \"Nickname\" = EXCLUDED.\"Nickname\",",
                "    \"Quantity\" = EXCLUDED.\"Quantity\",",
                "    \"IsActive\" = EXCLUDED.\"IsActive\",",
                "    \"Price\" = EXCLUDED.\"Price\";"), sql);
        }

        [Fact]
        public void GetByIdRange_FiltersWithEqualsAny()
        {
            string sql = _strategy.GetByIdRangeSqlBuilder<Widget>().Render("<<IDS>>");

            Assert.Equal(Golden(
                "SELECT",
                "    \"Id\",",
                "    \"Name\",",
                "    \"Nickname\",",
                "    \"Quantity\",",
                "    \"IsActive\",",
                "    \"Price\"",
                "FROM",
                "    \"dbo\".\"Widget\"",
                "WHERE",
                "    \"Id\" = ANY(<<IDS>>);"), sql);
        }

        [Fact]
        public void UpdateRange_UnpacksRowCompositeType()
        {
            string sql = _strategy.UpdateRangeSqlBuilder<Widget>().Render("<<ROWS>>");

            Assert.Equal(Golden(
                "UPDATE \"dbo\".\"Widget\" AS \"target\"",
                "SET",
                "    \"Name\" = \"source\".\"Name\",",
                "    \"Nickname\" = \"source\".\"Nickname\",",
                "    \"Quantity\" = \"source\".\"Quantity\",",
                "    \"IsActive\" = \"source\".\"IsActive\",",
                "    \"Price\" = \"source\".\"Price\"",
                "FROM (",
                "    SELECT",
                "        (\"data\".\"row\").\"Id\",",
                "        (\"data\".\"row\").\"Name\",",
                "        (\"data\".\"row\").\"Nickname\",",
                "        (\"data\".\"row\").\"Quantity\",",
                "        (\"data\".\"row\").\"IsActive\",",
                "        (\"data\".\"row\").\"Price\"",
                "    FROM (",
                "        VALUES",
                "<<ROWS>>",
                "    ) AS \"data\" (\"row\")",
                ") AS \"source\"",
                "WHERE",
                "    \"target\".\"Id\" = \"source\".\"Id\";"), sql);
        }

        [Fact]
        public void InsertRange_ListsColumnsThenMultipleValuesRows()
        {
            string sql = _strategy.InsertRangeSqlBuilder<Widget>().Render("<<ROWS>>");

            Assert.Equal(Golden(
                "INSERT INTO \"dbo\".\"Widget\" (",
                "    \"Id\",",
                "    \"Name\",",
                "    \"Nickname\",",
                "    \"Quantity\",",
                "    \"IsActive\",",
                "    \"Price\"",
                ")",
                "VALUES",
                "<<ROWS>>;"), sql);
        }

        [Fact]
        public void DeleteRange_FiltersWithEqualsAny()
        {
            string sql = _strategy.DeleteRangeSqlBuilder<Widget>().Render("<<IDS>>");

            Assert.Equal(Golden(
                "DELETE FROM \"dbo\".\"Widget\"",
                "WHERE",
                "    \"Id\" = ANY(<<IDS>>);"), sql);
        }

        [Fact]
        public void UpsertRange_UsesOnConflictDoUpdateWithExcluded()
        {
            string sql = _strategy.UpsertRangeSqlBuilder<Widget>().Render("<<ROWS>>");

            Assert.Equal(Golden(
                "INSERT INTO \"dbo\".\"Widget\" (",
                "    \"Id\",",
                "    \"Name\",",
                "    \"Nickname\",",
                "    \"Quantity\",",
                "    \"IsActive\",",
                "    \"Price\"",
                ")",
                "VALUES",
                "<<ROWS>>",
                "ON CONFLICT (\"Id\") DO UPDATE SET",
                "    \"Name\" = EXCLUDED.\"Name\",",
                "    \"Nickname\" = EXCLUDED.\"Nickname\",",
                "    \"Quantity\" = EXCLUDED.\"Quantity\",",
                "    \"IsActive\" = EXCLUDED.\"IsActive\",",
                "    \"Price\" = EXCLUDED.\"Price\";"), sql);
        }

        [Fact]
        public void Exists_UsesPlainExistsWithNoCast()
        {
            string sql = _strategy.ExistsSqlBuilder<Widget>().Render("<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    EXISTS (",
                "        SELECT",
                "            1",
                "        FROM",
                "            \"dbo\".\"Widget\"<<WHERE>>",
                "    );"), sql);
        }

        [Fact]
        public void Count_UsesPlainCount()
        {
            string sql = _strategy.CountSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    COUNT(<<COL>>)",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>;"), sql);
        }

        [Fact]
        public void Avg_CastsResultUsingTextOperator()
        {
            string sql = _strategy.AvgSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    AVG(<<COL>>)::text",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>;"), sql);
        }

        [Fact]
        public void Sum_UsesPlainSum()
        {
            string sql = _strategy.SumSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    SUM(<<COL>>)",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>;"), sql);
        }

        [Fact]
        public void Min_UsesPlainMin()
        {
            string sql = _strategy.MinSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    MIN(<<COL>>)",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>;"), sql);
        }

        [Fact]
        public void Max_UsesPlainMax()
        {
            string sql = _strategy.MaxSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    MAX(<<COL>>)",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>;"), sql);
        }
    }
}
