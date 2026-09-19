using Forget.SqlServer.Strategies;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// Locks the exact SQL text produced by SqlServer's <see cref="SqlBuilderStrategy"/> — the T-SQL specific shapes
    /// (<c>TOP</c>, <c>MERGE</c>-style upsert via <c>UPDLOCK/HOLDLOCK</c> and <c>IF @@ROWCOUNT</c>, table-valued
    /// <c>VALUES</c> constructors for range operations, <c>CAST(... AS BIT)</c> for <c>Exists</c>) that make this
    /// provider's builder genuinely different from the others, not just differently quoted.
    /// <para>
    /// Placeholder positions are filled with distinct marker tokens (<c>&lt;&lt;WHERE&gt;&gt;</c>, etc.) rather than
    /// realistic SQL fragments: these tests lock where a builder places its placeholders and what literal text
    /// surrounds them, not what a real predicate/value list looks like — that is covered by
    /// <see cref="Core.ExpressionTranslatorTests"/> and <see cref="Core.FilterNodeTranslatorTests"/>.
    /// </para>
    /// </summary>
    public class SqlBuilderStrategyTests
    {
        private static readonly SqlBuilderStrategy _strategy = SqlBuilderStrategy.Instance;

        private static string Golden(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        [Fact]
        public void GetAll_SelectsAllColumnsWithWhereSlot()
        {
            string sql = _strategy.GetAllSqlBuilder<Widget>().Render("<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    [Id],",
                "    [Name],",
                "    [Nickname],",
                "    [Quantity],",
                "    [IsActive],",
                "    [Price]",
                "FROM",
                "    [dbo].[Widget]<<WHERE>>;"), sql);
        }

        [Fact]
        public void GetFirst_UsesTopWithRowCountAndWhereSlots()
        {
            string sql = _strategy.GetFirstSqlBuilder<Widget>().Render("<<TOP>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT TOP (<<TOP>>)",
                "    [Id],",
                "    [Name],",
                "    [Nickname],",
                "    [Quantity],",
                "    [IsActive],",
                "    [Price]",
                "FROM",
                "    [dbo].[Widget]<<WHERE>>;"), sql);
        }

        [Fact]
        public void GetById_FiltersOnIdentifierColumn()
        {
            string sql = _strategy.GetByIdSqlBuilder<Widget>().Render("<<ID>>");

            Assert.Equal(Golden(
                "SELECT",
                "    [Id],",
                "    [Name],",
                "    [Nickname],",
                "    [Quantity],",
                "    [IsActive],",
                "    [Price]",
                "FROM",
                "    [dbo].[Widget]",
                "WHERE",
                "    [Id] = <<ID>>;"), sql);
        }

        [Fact]
        public void Update_SetsColumnsBySchemaQualifiedTable()
        {
            string sql = _strategy.UpdateSqlBuilder<Widget>().Render("<<SET>>");

            Assert.Equal(Golden(
                "UPDATE [dbo].[Widget]",
                "SET",
                "<<SET>>;"), sql);
        }

        [Fact]
        public void Insert_ListsColumnsThenSingleValuesRow()
        {
            string sql = _strategy.InsertSqlBuilder<Widget>().Render("<<VALUES>>");

            Assert.Equal(Golden(
                "INSERT INTO [dbo].[Widget] (",
                "    [Id],",
                "    [Name],",
                "    [Nickname],",
                "    [Quantity],",
                "    [IsActive],",
                "    [Price]",
                ")",
                "VALUES (",
                "<<VALUES>>",
                ");"), sql);
        }

        [Fact]
        public void Delete_HasWhereSlotRightAfterTableName()
        {
            string sql = _strategy.DeleteSqlBuilder<Widget>().Render("<<WHERE>>");

            Assert.Equal(Golden("DELETE FROM [dbo].[Widget]<<WHERE>>;"), sql);
        }

        [Fact]
        public void Upsert_UpdatesUnderLockThenConditionallyInserts()
        {
            string sql = _strategy.UpsertSqlBuilder<Widget>().Render("<<SET>>", "<<WHERE>>", "<<VALUES>>");

            Assert.Equal(Golden(
                "UPDATE [dbo].[Widget]",
                "WITH (UPDLOCK, HOLDLOCK)",
                "SET",
                "<<SET>>",
                "WHERE",
                "<<WHERE>>;",
                "",
                "IF @@ROWCOUNT = 0",
                "BEGIN",
                "    INSERT INTO [dbo].[Widget] (",
                "        [Id],",
                "        [Name],",
                "        [Nickname],",
                "        [Quantity],",
                "        [IsActive],",
                "        [Price]",
                "    )",
                "    VALUES (",
                "<<VALUES>>",
                "    );",
                "END"), sql);
        }

        [Fact]
        public void GetByIdRange_FiltersWithIn()
        {
            string sql = _strategy.GetByIdRangeSqlBuilder<Widget>().Render("<<IDS>>");

            Assert.Equal(Golden(
                "SELECT",
                "    [Id],",
                "    [Name],",
                "    [Nickname],",
                "    [Quantity],",
                "    [IsActive],",
                "    [Price]",
                "FROM",
                "    [dbo].[Widget]",
                "WHERE",
                "    [Id] IN <<IDS>>;"), sql);
        }

        [Fact]
        public void UpdateRange_JoinsAgainstTableValuedConstructor()
        {
            string sql = _strategy.UpdateRangeSqlBuilder<Widget>().Render("<<ROWS>>");

            Assert.Equal(Golden(
                "UPDATE [Target]",
                "SET",
                "    [Name] = [Source].[Name],",
                "    [Nickname] = [Source].[Nickname],",
                "    [Quantity] = [Source].[Quantity],",
                "    [IsActive] = [Source].[IsActive],",
                "    [Price] = [Source].[Price]",
                "FROM",
                "    [dbo].[Widget] AS [Target]",
                "INNER JOIN (",
                "    VALUES",
                "<<ROWS>>",
                ") AS [Source] ([Id], [Name], [Nickname], [Quantity], [IsActive], [Price])",
                "ON",
                "    [Target].[Id] = [Source].[Id];"), sql);
        }

        [Fact]
        public void InsertRange_ListsColumnsThenMultipleValuesRows()
        {
            string sql = _strategy.InsertRangeSqlBuilder<Widget>().Render("<<ROWS>>");

            Assert.Equal(Golden(
                "INSERT INTO [dbo].[Widget] (",
                "    [Id],",
                "    [Name],",
                "    [Nickname],",
                "    [Quantity],",
                "    [IsActive],",
                "    [Price]",
                ")",
                "VALUES",
                "<<ROWS>>;"), sql);
        }

        [Fact]
        public void DeleteRange_FiltersWithIn()
        {
            string sql = _strategy.DeleteRangeSqlBuilder<Widget>().Render("<<IDS>>");

            Assert.Equal(Golden(
                "DELETE FROM [dbo].[Widget]",
                "WHERE",
                "    [Id] IN <<IDS>>;"), sql);
        }

        [Fact]
        public void UpsertRange_UpdatesThenInsertsNonMatchingRows()
        {
            string sql = _strategy.UpsertRangeSqlBuilder<Widget>().Render("<<ROWS1>>", "<<ROWS2>>");

            Assert.Equal(Golden(
                "UPDATE [Target]",
                "SET",
                "    [Name] = [Source].[Name],",
                "    [Nickname] = [Source].[Nickname],",
                "    [Quantity] = [Source].[Quantity],",
                "    [IsActive] = [Source].[IsActive],",
                "    [Price] = [Source].[Price]",
                "FROM",
                "    [dbo].[Widget] AS [Target]",
                "    WITH (UPDLOCK, HOLDLOCK)",
                "INNER JOIN (",
                "    VALUES",
                "<<ROWS1>>",
                ") AS [Source] ([Id], [Name], [Nickname], [Quantity], [IsActive], [Price])",
                "ON",
                "    (",
                "        [Target].[Id] = [Source].[Id]",
                "        OR ([Target].[Id] IS NULL AND [Source].[Id] IS NULL)",
                "    );",
                "",
                "INSERT INTO [dbo].[Widget] (",
                "    [Id],",
                "    [Name],",
                "    [Nickname],",
                "    [Quantity],",
                "    [IsActive],",
                "    [Price]",
                ")",
                "SELECT",
                "    [Id],",
                "    [Name],",
                "    [Nickname],",
                "    [Quantity],",
                "    [IsActive],",
                "    [Price]",
                "FROM (",
                "    VALUES",
                "<<ROWS2>>",
                ") AS [Source] ([Id], [Name], [Nickname], [Quantity], [IsActive], [Price])",
                "WHERE NOT EXISTS (",
                "    SELECT",
                "        1",
                "    FROM",
                "        [dbo].[Widget] AS [Target]",
                "        WITH (UPDLOCK, HOLDLOCK)",
                "    WHERE",
                "        (",
                "            [Target].[Id] = [Source].[Id]",
                "            OR ([Target].[Id] IS NULL AND [Source].[Id] IS NULL)",
                "        )",
                ");"), sql);
        }

        [Fact]
        public void Exists_CastsCaseExpressionToBit()
        {
            string sql = _strategy.ExistsSqlBuilder<Widget>().Render("<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    CAST(",
                "        CASE",
                "            WHEN EXISTS (",
                "                SELECT",
                "                    1",
                "                FROM",
                "                    [dbo].[Widget]<<WHERE>>",
                "            )",
                "            THEN",
                "                1",
                "            ELSE",
                "                0",
                "        END AS BIT",
                "    );"), sql);
        }

        [Fact]
        public void Count_UsesCountBig()
        {
            string sql = _strategy.CountSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    COUNT_BIG(<<COL>>)",
                "FROM",
                "    [dbo].[Widget]<<WHERE>>;"), sql);
        }

        [Fact]
        public void Avg_CastsResultToNVarcharMax()
        {
            string sql = _strategy.AvgSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    CAST(AVG(<<COL>>) AS NVARCHAR(MAX))",
                "FROM",
                "    [dbo].[Widget]<<WHERE>>;"), sql);
        }

        [Fact]
        public void Sum_UsesPlainSum()
        {
            string sql = _strategy.SumSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    SUM(<<COL>>)",
                "FROM",
                "    [dbo].[Widget]<<WHERE>>;"), sql);
        }

        [Fact]
        public void Min_UsesPlainMin()
        {
            string sql = _strategy.MinSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    MIN(<<COL>>)",
                "FROM",
                "    [dbo].[Widget]<<WHERE>>;"), sql);
        }

        [Fact]
        public void Max_UsesPlainMax()
        {
            string sql = _strategy.MaxSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    MAX(<<COL>>)",
                "FROM",
                "    [dbo].[Widget]<<WHERE>>;"), sql);
        }
    }
}
