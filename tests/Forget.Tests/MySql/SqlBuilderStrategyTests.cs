namespace Forget.Tests.MySql
{
    /// <summary>
    /// Locks the exact SQL text produced by MySql's <c>SqlBuilderStrategy</c>: <c>LIMIT</c> instead of
    /// <c>OFFSET/FETCH</c> or <c>TOP</c>, <c>ON DUPLICATE KEY UPDATE</c> for upsert (no <c>MERGE</c>, no locking
    /// hints), a plain <c>EXISTS(...)</c> with no <c>CAST</c>/<c>DUAL</c> wrapper, and column-type discovery that is
    /// simply not implemented (<see cref="GetColumns_IsNotImplemented"/>) unlike Oracle's.
    /// <para>
    /// Placeholder positions are filled with distinct marker tokens rather than realistic SQL fragments: these
    /// tests lock where a builder places its placeholders and what literal text surrounds them, not what a real
    /// predicate/value list looks like.
    /// </para>
    /// </summary>
    public class SqlBuilderStrategyTests
    {
        private static readonly Forget.MySql.Strategies.SqlBuilderStrategy _strategy = Forget.MySql.Strategies.SqlBuilderStrategy.Instance;

        private static string Golden(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        [Fact]
        public void GetColumns_IsNotImplemented()
        {
            string sql = _strategy.GetColumnsSqlBuilder<Widget>().Render();

            Assert.Equal(string.Empty, sql);
        }

        [Fact]
        public void GetAll_SelectsAllColumnsWithWhereSlot()
        {
            string sql = _strategy.GetAllSqlBuilder<Widget>().Render("<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    `Id`,",
                "    `Name`,",
                "    `Nickname`,",
                "    `Quantity`,",
                "    `IsActive`,",
                "    `Price`",
                "FROM",
                "    `dbo`.`Widget`<<WHERE>>;"), sql);
        }

        [Fact]
        public void GetFirst_UsesLimitAfterWhereSlot()
        {
            string sql = _strategy.GetFirstSqlBuilder<Widget>().Render("<<WHERE>>", "<<TAKE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    `Id`,",
                "    `Name`,",
                "    `Nickname`,",
                "    `Quantity`,",
                "    `IsActive`,",
                "    `Price`",
                "FROM",
                "    `dbo`.`Widget`<<WHERE>>",
                "LIMIT",
                "    <<TAKE>>;"), sql);
        }

        [Fact]
        public void GetById_FiltersOnIdentifierColumn()
        {
            string sql = _strategy.GetByIdSqlBuilder<Widget>().Render("<<ID>>");

            Assert.Equal(Golden(
                "SELECT",
                "    `Id`,",
                "    `Name`,",
                "    `Nickname`,",
                "    `Quantity`,",
                "    `IsActive`,",
                "    `Price`",
                "FROM",
                "    `dbo`.`Widget`",
                "WHERE",
                "    `Id` = <<ID>>;"), sql);
        }

        [Fact]
        public void Update_SetsColumnsBySchemaQualifiedTable()
        {
            string sql = _strategy.UpdateSqlBuilder<Widget>().Render("<<SET>>");

            Assert.Equal(Golden(
                "UPDATE `dbo`.`Widget`",
                "SET",
                "<<SET>>;"), sql);
        }

        [Fact]
        public void Insert_ListsColumnsThenSingleValuesRow()
        {
            string sql = _strategy.InsertSqlBuilder<Widget>().Render("<<VALUES>>");

            Assert.Equal(Golden(
                "INSERT INTO `dbo`.`Widget` (",
                "    `Id`,",
                "    `Name`,",
                "    `Nickname`,",
                "    `Quantity`,",
                "    `IsActive`,",
                "    `Price`",
                ")",
                "VALUES (",
                "<<VALUES>>",
                ");"), sql);
        }

        [Fact]
        public void Delete_HasWhereSlotRightAfterTableName()
        {
            string sql = _strategy.DeleteSqlBuilder<Widget>().Render("<<WHERE>>");

            Assert.Equal(Golden("DELETE FROM `dbo`.`Widget`<<WHERE>>;"), sql);
        }

        [Fact]
        public void Upsert_UsesOnDuplicateKeyUpdate()
        {
            string sql = _strategy.UpsertSqlBuilder<Widget>().Render("<<VALUES>>");

            Assert.Equal(Golden(
                "INSERT INTO `dbo`.`Widget` (",
                "    `Id`,",
                "    `Name`,",
                "    `Nickname`,",
                "    `Quantity`,",
                "    `IsActive`,",
                "    `Price`",
                ")",
                "VALUES (",
                "<<VALUES>>",
                ") AS `new`",
                "ON DUPLICATE KEY UPDATE",
                "    `Name` = `new`.`Name`,",
                "    `Nickname` = `new`.`Nickname`,",
                "    `Quantity` = `new`.`Quantity`,",
                "    `IsActive` = `new`.`IsActive`,",
                "    `Price` = `new`.`Price`;"), sql);
        }

        [Fact]
        public void GetByIdRange_FiltersWithIn()
        {
            string sql = _strategy.GetByIdRangeSqlBuilder<Widget>().Render("<<IDS>>");

            Assert.Equal(Golden(
                "SELECT",
                "    `Id`,",
                "    `Name`,",
                "    `Nickname`,",
                "    `Quantity`,",
                "    `IsActive`,",
                "    `Price`",
                "FROM",
                "    `dbo`.`Widget`",
                "WHERE",
                "    `Id` IN <<IDS>>;"), sql);
        }

        [Fact]
        public void UpdateRange_JoinsAgainstRowValuesThenSets()
        {
            string sql = _strategy.UpdateRangeSqlBuilder<Widget>().Render("<<ROWS>>");

            Assert.Equal(Golden(
                "UPDATE `dbo`.`Widget` AS `target`",
                "INNER JOIN (",
                "<<ROWS>>",
                ") AS `source` (`Id`, `Name`, `Nickname`, `Quantity`, `IsActive`, `Price`)",
                "ON",
                "    `target`.`Id` = `source`.`Id`",
                "SET",
                "    `target`.`Name` = `source`.`Name`,",
                "    `target`.`Nickname` = `source`.`Nickname`,",
                "    `target`.`Quantity` = `source`.`Quantity`,",
                "    `target`.`IsActive` = `source`.`IsActive`,",
                "    `target`.`Price` = `source`.`Price`;"), sql);
        }

        [Fact]
        public void InsertRange_ListsColumnsThenMultipleValuesRows()
        {
            string sql = _strategy.InsertRangeSqlBuilder<Widget>().Render("<<ROWS>>");

            Assert.Equal(Golden(
                "INSERT INTO `dbo`.`Widget` (",
                "    `Id`,",
                "    `Name`,",
                "    `Nickname`,",
                "    `Quantity`,",
                "    `IsActive`,",
                "    `Price`",
                ")",
                "VALUES",
                "<<ROWS>>;"), sql);
        }

        [Fact]
        public void DeleteRange_FiltersWithIn()
        {
            string sql = _strategy.DeleteRangeSqlBuilder<Widget>().Render("<<IDS>>");

            Assert.Equal(Golden(
                "DELETE FROM `dbo`.`Widget`",
                "WHERE",
                "    `Id` IN <<IDS>>;"), sql);
        }

        [Fact]
        public void UpsertRange_UsesOnDuplicateKeyUpdate()
        {
            string sql = _strategy.UpsertRangeSqlBuilder<Widget>().Render("<<ROWS>>");

            Assert.Equal(Golden(
                "INSERT INTO `dbo`.`Widget` (",
                "    `Id`,",
                "    `Name`,",
                "    `Nickname`,",
                "    `Quantity`,",
                "    `IsActive`,",
                "    `Price`",
                ")",
                "VALUES",
                "<<ROWS>>",
                "AS `new`",
                "ON DUPLICATE KEY UPDATE",
                "    `Name` = `new`.`Name`,",
                "    `Nickname` = `new`.`Nickname`,",
                "    `Quantity` = `new`.`Quantity`,",
                "    `IsActive` = `new`.`IsActive`,",
                "    `Price` = `new`.`Price`;"), sql);
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
                "            `dbo`.`Widget`<<WHERE>>",
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
                "    `dbo`.`Widget`<<WHERE>>;"), sql);
        }

        [Fact]
        public void Avg_CastsResultToChar()
        {
            string sql = _strategy.AvgSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    CAST(AVG(<<COL>>) AS CHAR)",
                "FROM",
                "    `dbo`.`Widget`<<WHERE>>;"), sql);
        }

        [Fact]
        public void Sum_UsesPlainSum()
        {
            string sql = _strategy.SumSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    SUM(<<COL>>)",
                "FROM",
                "    `dbo`.`Widget`<<WHERE>>;"), sql);
        }

        [Fact]
        public void Min_UsesPlainMin()
        {
            string sql = _strategy.MinSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    MIN(<<COL>>)",
                "FROM",
                "    `dbo`.`Widget`<<WHERE>>;"), sql);
        }

        [Fact]
        public void Max_UsesPlainMax()
        {
            string sql = _strategy.MaxSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    MAX(<<COL>>)",
                "FROM",
                "    `dbo`.`Widget`<<WHERE>>;"), sql);
        }
    }
}
