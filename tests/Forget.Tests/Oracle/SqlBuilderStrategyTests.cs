namespace Forget.Tests.Oracle
{
    /// <summary>
    /// Locks the exact SQL text produced by Oracle's <c>SqlBuilderStrategy</c> — <c>FETCH FIRST ... ROWS ONLY</c>
    /// pagination, <c>MERGE INTO ... USING ... ON (...) WHEN MATCHED/NOT MATCHED</c> for every upsert-shaped
    /// operation (there is no native <c>ON CONFLICT</c>/<c>ON DUPLICATE KEY</c> equivalent), <c>SELECT ... FROM
    /// DUAL</c> wherever a real table isn't otherwise on the FROM clause, <c>TO_CHAR(..., 'TM', 'NLS_NUMERIC_...')</c>
    /// for <c>Avg</c> so the result isn't locale-formatted, no trailing <c>;</c> (Oracle's terminator is just a
    /// newline — a driver-supplied trailing semicolon breaks ODP.NET), and the PL/SQL block
    /// <see cref="GetColumns_EmitsPlSqlBlockThatDiscoversColumnCastExpressions"/> uses to discover, at runtime, the
    /// exact <c>CAST</c>/<c>TO_x</c> expression each column needs — the mechanism behind the multi-row insert
    /// discussed for this provider.
    /// <para>
    /// Placeholder positions are filled with distinct marker tokens rather than realistic SQL fragments — these
    /// tests lock where a builder places its placeholders and what literal text surrounds them, not what a real
    /// predicate/value list looks like.
    /// </para>
    /// </summary>
    public class SqlBuilderStrategyTests
    {
        private static readonly Forget.Oracle.Strategies.SqlBuilderStrategy _strategy = Forget.Oracle.Strategies.SqlBuilderStrategy.Instance;

        private static string Golden(params string[] lines)
        {
            return string.Join(Environment.NewLine, lines) + Environment.NewLine;
        }

        [Fact]
        public void GetColumns_EmitsPlSqlBlockThatDiscoversColumnCastExpressions()
        {
            string sql = _strategy.GetColumnsSqlBuilder<Widget>().Render("<<OWNER>>", "<<TABLE>>");

            Assert.Equal(Golden(
                "DECLARE",
                "    v_sql VARCHAR2(4000);",
                "    v_type VARCHAR2(4000);",
                "    v_ok BOOLEAN;",
                "    v_json CLOB;",
                "BEGIN",
                "    v_json := '[';",
                "",
                "    FOR c IN (",
                "        SELECT",
                "            \"COLUMN_NAME\",",
                "            \"DATA_TYPE\",",
                "            \"DATA_LENGTH\",",
                "            \"DATA_PRECISION\",",
                "            \"DATA_SCALE\",",
                "            \"CHAR_LENGTH\",",
                "            \"CHAR_USED\"",
                "        FROM",
                "            \"ALL_TAB_COLUMNS\"",
                "        WHERE",
                "            \"OWNER\" = <<OWNER>>",
                "            AND \"TABLE_NAME\" = <<TABLE>>",
                "            AND REGEXP_LIKE(\"COLUMN_NAME\", '^[A-Za-z][A-Za-z0-9_]*$')",
                "    )",
                "    LOOP",
                "        v_ok := FALSE;",
                "",
                "        IF NOT v_ok THEN",
                "            BEGIN",
                "                v_type := c.\"DATA_TYPE\";",
                "",
                "                IF c.\"DATA_PRECISION\" IS NOT NULL AND c.\"DATA_SCALE\" IS NOT NULL THEN",
                "                    v_type := v_type || '(' || c.\"DATA_PRECISION\" || ',' || c.\"DATA_SCALE\" || ')';",
                "                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';",
                "",
                "                    DECLARE",
                "                        v_dummy VARCHAR2(1);",
                "                    BEGIN",
                "                        EXECUTE IMMEDIATE v_sql INTO v_dummy;",
                "                    END;",
                "",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_ok := TRUE;",
                "                END IF;",
                "",
                "            EXCEPTION",
                "                WHEN OTHERS THEN",
                "                    NULL;",
                "            END;",
                "        END IF;",
                "",
                "        IF NOT v_ok THEN",
                "            BEGIN",
                "                v_type := c.\"DATA_TYPE\";",
                "",
                "                IF c.\"DATA_PRECISION\" IS NOT NULL THEN",
                "                    v_type := v_type || '(' || c.\"DATA_PRECISION\" || ')';",
                "                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';",
                "",
                "                    DECLARE",
                "                        v_dummy VARCHAR2(1);",
                "                    BEGIN",
                "                        EXECUTE IMMEDIATE v_sql INTO v_dummy;",
                "                    END;",
                "",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_ok := TRUE;",
                "                END IF;",
                "",
                "            EXCEPTION",
                "                WHEN OTHERS THEN",
                "                    NULL;",
                "            END;",
                "        END IF;",
                "",
                "        IF NOT v_ok THEN",
                "            BEGIN",
                "                v_type := c.\"DATA_TYPE\";",
                "",
                "                IF c.\"DATA_TYPE\" = 'NUMBER' AND c.\"DATA_SCALE\" IS NOT NULL THEN",
                "                    v_type := v_type || '(38,' || c.\"DATA_SCALE\" || ')';",
                "                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';",
                "",
                "                    DECLARE",
                "                        v_dummy VARCHAR2(1);",
                "                    BEGIN",
                "                        EXECUTE IMMEDIATE v_sql INTO v_dummy;",
                "                    END;",
                "",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_ok := TRUE;",
                "                END IF;",
                "",
                "            EXCEPTION",
                "                WHEN OTHERS THEN",
                "                    NULL;",
                "            END;",
                "        END IF;",
                "",
                "        IF NOT v_ok THEN",
                "            BEGIN",
                "                v_type := c.\"DATA_TYPE\";",
                "",
                "                IF c.\"DATA_TYPE\" IN ('CHAR', 'NCHAR') AND c.\"CHAR_LENGTH\" IS NOT NULL THEN",
                "                    v_type := v_type || '(' || c.\"CHAR_LENGTH\" ||",
                "                        CASE",
                "                            WHEN c.\"DATA_TYPE\" <> 'CHAR' THEN ''",
                "                            WHEN c.\"CHAR_USED\" = 'C' THEN ' CHAR'",
                "                            ELSE ' BYTE'",
                "                        END || ')';",
                "                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';",
                "",
                "                    DECLARE",
                "                        v_dummy VARCHAR2(1);",
                "                    BEGIN",
                "                        EXECUTE IMMEDIATE v_sql INTO v_dummy;",
                "                    END;",
                "",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_ok := TRUE;",
                "                END IF;",
                "",
                "            EXCEPTION",
                "                WHEN OTHERS THEN",
                "                    NULL;",
                "            END;",
                "        END IF;",
                "",
                "        IF NOT v_ok THEN",
                "            BEGIN",
                "                v_type := c.\"DATA_TYPE\";",
                "",
                "                IF c.\"DATA_TYPE\" IN ('VARCHAR2', 'NVARCHAR2', 'RAW') AND c.\"DATA_LENGTH\" IS NOT NULL THEN",
                "                    v_type := v_type || '(' || c.\"DATA_LENGTH\" || ')';",
                "                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';",
                "",
                "                    DECLARE",
                "                        v_dummy VARCHAR2(1);",
                "                    BEGIN",
                "                        EXECUTE IMMEDIATE v_sql INTO v_dummy;",
                "                    END;",
                "",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_ok := TRUE;",
                "                END IF;",
                "",
                "            EXCEPTION",
                "                WHEN OTHERS THEN",
                "                    NULL;",
                "            END;",
                "        END IF;",
                "",
                "        IF NOT v_ok THEN",
                "            BEGIN",
                "                v_type := c.\"DATA_TYPE\";",
                "",
                "                IF 1 = 1 THEN",
                "                    v_type := v_type;",
                "                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';",
                "",
                "                    DECLARE",
                "                        v_dummy VARCHAR2(1);",
                "                    BEGIN",
                "                        EXECUTE IMMEDIATE v_sql INTO v_dummy;",
                "                    END;",
                "",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_ok := TRUE;",
                "                END IF;",
                "",
                "            EXCEPTION",
                "                WHEN OTHERS THEN",
                "                    NULL;",
                "            END;",
                "        END IF;",
                "",
                "        IF NOT v_ok THEN",
                "            BEGIN",
                "                v_type := c.\"DATA_TYPE\";",
                "",
                "                IF 1 = 1 THEN",
                "                    v_type := v_type;",
                "                    v_sql := 'SELECT ''X'' FROM DUAL WHERE TO_' || v_type || '(NULL) IS NULL';",
                "",
                "                    DECLARE",
                "                        v_dummy VARCHAR2(1);",
                "                    BEGIN",
                "                        EXECUTE IMMEDIATE v_sql INTO v_dummy;",
                "                    END;",
                "",
                "                    v_type := 'TO_' || v_type || '({})';",
                "                    v_ok := TRUE;",
                "                END IF;",
                "",
                "            EXCEPTION",
                "                WHEN OTHERS THEN",
                "                    NULL;",
                "            END;",
                "        END IF;",
                "",
                "        IF DBMS_LOB.GETLENGTH(v_json) > 1 THEN",
                "            v_json := v_json || ',';",
                "        END IF;",
                "",
                "        v_json := v_json ||",
                "            '{' ||",
                "                '\"Name\": \"' || c.\"COLUMN_NAME\" || '\",' ||",
                "                '\"DataType\": \"' || c.\"DATA_TYPE\" || '\",' ||",
                "                '\"CastExpression\": ' || CASE WHEN v_ok THEN '\"' || v_type || '\"' ELSE 'null' END || ',' ||",
                "                '\"IsCastable\": ' || CASE WHEN v_ok THEN 'true' ELSE 'false' END ||",
                "            '}';",
                "    END LOOP;",
                "",
                "    v_json := v_json || ']';",
                "    :result := v_json;",
                "END;"), sql);
        }

        [Fact]
        public void GetAll_SelectsAllColumnsWithWhereSlotAndNoTrailingSemicolon()
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
                "    \"dbo\".\"Widget\"<<WHERE>>"), sql);
        }

        [Fact]
        public void GetFirst_UsesFetchFirstRowsOnly()
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
                "FETCH FIRST",
                "    <<TAKE>> ROWS ONLY"), sql);
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
                "    \"Id\" = <<ID>>"), sql);
        }

        [Fact]
        public void Update_SetsColumnsBySchemaQualifiedTable()
        {
            string sql = _strategy.UpdateSqlBuilder<Widget>().Render("<<SET>>");

            Assert.Equal(Golden(
                "UPDATE \"dbo\".\"Widget\"",
                "SET",
                "<<SET>>"), sql);
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
                ")"), sql);
        }

        [Fact]
        public void Delete_HasWhereSlotRightAfterTableName()
        {
            string sql = _strategy.DeleteSqlBuilder<Widget>().Render("<<WHERE>>");

            Assert.Equal(Golden("DELETE FROM \"dbo\".\"Widget\"<<WHERE>>"), sql);
        }

        [Fact]
        public void Upsert_UsesMergeFromDualWithNullSafeMatch()
        {
            string sql = _strategy.UpsertSqlBuilder<Widget>().Render("<<VALUES>>");

            Assert.Equal(Golden(
                "MERGE INTO \"dbo\".\"Widget\" \"TARGET\"",
                "USING (",
                "    SELECT",
                "<<VALUES>>",
                "    FROM",
                "        DUAL",
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
                "    )"), sql);
        }

        [Fact]
        public void GetByIdRange_FiltersWithIn()
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
                "    \"Id\" IN <<IDS>>"), sql);
        }

        [Fact]
        public void UpdateRange_UsesMergeWithoutNullSafeMatchOnPlainKey()
        {
            string sql = _strategy.UpdateRangeSqlBuilder<Widget>().Render("<<ROWS>>");

            Assert.Equal(Golden(
                "MERGE INTO \"dbo\".\"Widget\" \"TARGET\"",
                "USING (",
                "<<ROWS>>",
                ") \"SOURCE\"",
                "ON (",
                "    \"TARGET\".\"Id\" = \"SOURCE\".\"Id\"",
                ")",
                "WHEN MATCHED THEN",
                "    UPDATE SET",
                "        \"TARGET\".\"Name\" = \"SOURCE\".\"Name\",",
                "        \"TARGET\".\"Nickname\" = \"SOURCE\".\"Nickname\",",
                "        \"TARGET\".\"Quantity\" = \"SOURCE\".\"Quantity\",",
                "        \"TARGET\".\"IsActive\" = \"SOURCE\".\"IsActive\",",
                "        \"TARGET\".\"Price\" = \"SOURCE\".\"Price\""), sql);
        }

        [Fact]
        public void InsertRange_HasNoValuesKeywordOfItsOwn()
        {
            // Unlike the other three providers, Oracle has no multi-row VALUES(...),(...) syntax: the row batch
            // placeholder stands in for a whole "SELECT ... FROM DUAL UNION ALL ..." block built elsewhere
            // (Forget.Oracle.Strategies.DbCommandStrategy.BuildUpsertRangeCommands), not a VALUES clause.
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
                "<<ROWS>>"), sql);
        }

        [Fact]
        public void DeleteRange_FiltersWithIn()
        {
            string sql = _strategy.DeleteRangeSqlBuilder<Widget>().Render("<<IDS>>");

            Assert.Equal(Golden(
                "DELETE FROM \"dbo\".\"Widget\"",
                "WHERE",
                "    \"Id\" IN <<IDS>>"), sql);
        }

        [Fact]
        public void UpsertRange_UsesMergeFromRowsWithNullSafeMatch()
        {
            string sql = _strategy.UpsertRangeSqlBuilder<Widget>().Render("<<ROWS>>");

            Assert.Equal(Golden(
                "MERGE INTO \"dbo\".\"Widget\" \"TARGET\"",
                "USING (",
                "<<ROWS>>",
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
                "    )"), sql);
        }

        [Fact]
        public void Exists_SelectsCaseFromDual()
        {
            string sql = _strategy.ExistsSqlBuilder<Widget>().Render("<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    CASE",
                "        WHEN EXISTS (",
                "            SELECT",
                "                1",
                "            FROM",
                "                \"dbo\".\"Widget\"<<WHERE>>",
                "        )",
                "        THEN",
                "            1",
                "        ELSE",
                "            0",
                "    END",
                "FROM",
                "    DUAL"), sql);
        }

        [Fact]
        public void Count_UsesPlainCount()
        {
            string sql = _strategy.CountSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    COUNT(<<COL>>)",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>"), sql);
        }

        [Fact]
        public void Avg_FormatsResultAsInvariantDecimalString()
        {
            string sql = _strategy.AvgSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    TO_CHAR(",
                "        AVG(<<COL>>),",
                "        'TM',",
                "        'NLS_NUMERIC_CHARACTERS=''.,'''",
                "    )",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>"), sql);
        }

        [Fact]
        public void Sum_UsesPlainSum()
        {
            string sql = _strategy.SumSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    SUM(<<COL>>)",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>"), sql);
        }

        [Fact]
        public void Min_UsesPlainMin()
        {
            string sql = _strategy.MinSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    MIN(<<COL>>)",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>"), sql);
        }

        [Fact]
        public void Max_UsesPlainMax()
        {
            string sql = _strategy.MaxSqlBuilder<Widget>().Render("<<COL>>", "<<WHERE>>");

            Assert.Equal(Golden(
                "SELECT",
                "    MAX(<<COL>>)",
                "FROM",
                "    \"dbo\".\"Widget\"<<WHERE>>"), sql);
        }
    }
}
