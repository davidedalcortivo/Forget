using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;


namespace Forget.Oracle.Strategies
{
    internal sealed partial class SqlBuilderStrategy : BaseSqlBuilderStrategy<SqlDialectStrategy>
    {
        public static SqlBuilderStrategy Instance { get; } = new(Strategies.SqlDialectStrategy.Instance);

        private SqlBuilderStrategy(SqlDialectStrategy strategy) : base(strategy) { }

        public override SqlTemplate GetColumnsSqlBuilder<TEntity>()
        {
            string allTabColumnsTable = SqlDialectStrategy.RenderIdentifier("ALL_TAB_COLUMNS");
            string columnNameColumn = SqlDialectStrategy.RenderIdentifier("COLUMN_NAME");
            string dataTypeNameColumn = SqlDialectStrategy.RenderIdentifier("DATA_TYPE");
            string dataLengthNameColumn = SqlDialectStrategy.RenderIdentifier("DATA_LENGTH");
            string dataPrecisionNameColumn = SqlDialectStrategy.RenderIdentifier("DATA_PRECISION");
            string dataScaleNameColumn = SqlDialectStrategy.RenderIdentifier("DATA_SCALE");
            string charLengthNameColumn = SqlDialectStrategy.RenderIdentifier("CHAR_LENGTH");
            string charUsedNameColumn = SqlDialectStrategy.RenderIdentifier("CHAR_USED");
            string tableNameColumn = SqlDialectStrategy.RenderIdentifier("TABLE_NAME");
            string ownerColumn = SqlDialectStrategy.RenderIdentifier("OWNER");

            StringBuilder conditionBuffer1 = new();
            StringBuilder conditionBuffer2 = new();
            StringBuilder conditionBuffer3 = new();
            StringBuilder conditionBuffer4 = new();
            StringBuilder conditionBuffer5 = new();
            StringBuilder conditionBuffer6 = new();
            StringBuilder conditionBuffer7 = new();

            conditionBuffer1.AppendLine();
            conditionBuffer1.Append("                IF c.");
            conditionBuffer1.Append(dataPrecisionNameColumn);
            conditionBuffer1.Append(" IS NOT NULL AND c.");
            conditionBuffer1.Append(dataScaleNameColumn);
            conditionBuffer1.AppendLine(" IS NOT NULL THEN");
            conditionBuffer1.Append("                    v_type := v_type || '(' || c.");
            conditionBuffer1.Append(dataPrecisionNameColumn);
            conditionBuffer1.Append(" || ',' || c.");
            conditionBuffer1.Append(dataScaleNameColumn);
            conditionBuffer1.AppendLine(" || ')';");
            conditionBuffer1.AppendLine("                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';");

            conditionBuffer2.AppendLine();
            conditionBuffer2.Append("                IF c.");
            conditionBuffer2.Append(dataPrecisionNameColumn);
            conditionBuffer2.AppendLine(" IS NOT NULL THEN");
            conditionBuffer2.Append("                    v_type := v_type || '(' || c.");
            conditionBuffer2.Append(dataPrecisionNameColumn);
            conditionBuffer2.AppendLine(" || ')';");
            conditionBuffer2.AppendLine("                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';");

            conditionBuffer3.AppendLine();
            conditionBuffer3.Append("                IF c.");
            conditionBuffer3.Append(dataTypeNameColumn);
            conditionBuffer3.Append(" = 'NUMBER' AND c.");
            conditionBuffer3.Append(dataScaleNameColumn);
            conditionBuffer3.AppendLine(" IS NOT NULL THEN");
            conditionBuffer3.Append("                    v_type := v_type || '(38,' || c.");
            conditionBuffer3.Append(dataScaleNameColumn);
            conditionBuffer3.AppendLine(" || ')';");
            conditionBuffer3.AppendLine("                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';");

            conditionBuffer4.AppendLine();
            conditionBuffer4.Append("                IF c.");
            conditionBuffer4.Append(dataTypeNameColumn);
            conditionBuffer4.Append(" IN ('CHAR', 'NCHAR') AND c.");
            conditionBuffer4.Append(charLengthNameColumn);
            conditionBuffer4.AppendLine(" IS NOT NULL THEN");
            conditionBuffer4.Append("                    v_type := v_type || '(' || c.");
            conditionBuffer4.Append(charLengthNameColumn);
            conditionBuffer4.AppendLine(" ||");
            conditionBuffer4.AppendLine("                        CASE");
            conditionBuffer4.Append("                            WHEN c.");
            conditionBuffer4.Append(dataTypeNameColumn);
            conditionBuffer4.AppendLine(" <> 'CHAR' THEN ''");
            conditionBuffer4.Append("                            WHEN c.");
            conditionBuffer4.Append(charUsedNameColumn);
            conditionBuffer4.AppendLine(" = 'C' THEN ' CHAR'");
            conditionBuffer4.AppendLine("                            ELSE ' BYTE'");
            conditionBuffer4.AppendLine("                        END || ')';");
            conditionBuffer4.AppendLine("                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';");

            conditionBuffer5.AppendLine();
            conditionBuffer5.Append("                IF c.");
            conditionBuffer5.Append(dataTypeNameColumn);
            conditionBuffer5.Append(" IN ('VARCHAR2', 'NVARCHAR2', 'RAW') AND c.");
            conditionBuffer5.Append(dataLengthNameColumn);
            conditionBuffer5.AppendLine(" IS NOT NULL THEN");
            conditionBuffer5.Append("                    v_type := v_type || '(' || c.");
            conditionBuffer5.Append(dataLengthNameColumn);
            conditionBuffer5.AppendLine(" || ')';");
            conditionBuffer5.AppendLine("                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';");

            conditionBuffer6.AppendLine();
            conditionBuffer6.AppendLine("                IF 1 = 1 THEN");
            conditionBuffer6.AppendLine("                    v_type := v_type;");
            conditionBuffer6.AppendLine("                    v_sql := 'SELECT ''X'' FROM DUAL WHERE CAST(NULL AS ' || v_type || ') IS NULL';");

            conditionBuffer7.AppendLine();
            conditionBuffer7.AppendLine("                IF 1 = 1 THEN");
            conditionBuffer7.AppendLine("                    v_type := v_type;");
            conditionBuffer7.AppendLine("                    v_sql := 'SELECT ''X'' FROM DUAL WHERE TO_' || v_type || '(NULL) IS NULL';");

            StringBuilder[] conditionBuffers = [
                conditionBuffer1,
                conditionBuffer2,
                conditionBuffer3,
                conditionBuffer4,
                conditionBuffer5,
                conditionBuffer6,
                conditionBuffer7
            ];

            string[] vTypes = [
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_type := 'CAST({} AS ' || v_type || ')';",
                "                    v_type := 'TO_' || v_type || '({})';"
            ];

            StringBuilder sqlBuffer = new();

            sqlBuffer.AppendLine("DECLARE");
            sqlBuffer.AppendLine("    v_sql VARCHAR2(4000);");
            sqlBuffer.AppendLine("    v_type VARCHAR2(4000);");
            sqlBuffer.AppendLine("    v_ok BOOLEAN;");
            sqlBuffer.AppendLine("    v_json CLOB;");
            sqlBuffer.AppendLine("BEGIN");
            sqlBuffer.AppendLine("    v_json := '[';");
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("    FOR c IN (");
            sqlBuffer.AppendLine("        SELECT");
            sqlBuffer.Append("            ");
            sqlBuffer.Append(columnNameColumn);
            sqlBuffer.AppendLine(",");
            sqlBuffer.Append("            ");
            sqlBuffer.Append(dataTypeNameColumn);
            sqlBuffer.AppendLine(",");
            sqlBuffer.Append("            ");
            sqlBuffer.Append(dataLengthNameColumn);
            sqlBuffer.AppendLine(",");
            sqlBuffer.Append("            ");
            sqlBuffer.Append(dataPrecisionNameColumn);
            sqlBuffer.AppendLine(",");
            sqlBuffer.Append("            ");
            sqlBuffer.Append(dataScaleNameColumn);
            sqlBuffer.AppendLine(",");
            sqlBuffer.Append("            ");
            sqlBuffer.Append(charLengthNameColumn);
            sqlBuffer.AppendLine(",");
            sqlBuffer.Append("            ");
            sqlBuffer.AppendLine(charUsedNameColumn);
            sqlBuffer.AppendLine("        FROM");
            sqlBuffer.Append("            ");
            sqlBuffer.Append(allTabColumnsTable);
            sqlBuffer.AppendWhereClause($"{ownerColumn} = {SqlDialectStrategy.Placeholder}" + Environment.NewLine + $"            AND {tableNameColumn} = {SqlDialectStrategy.Placeholder}" + Environment.NewLine + $"            AND REGEXP_LIKE({columnNameColumn}, '{IdentifierHelper.CharsetPattern}')", "        ");
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("    )");
            sqlBuffer.AppendLine("    LOOP");
            sqlBuffer.AppendLine("        v_ok := FALSE;");
            sqlBuffer.AppendLine();

            for (int i = 0; i < conditionBuffers.Length; i++)
            {
                sqlBuffer.AppendLine("        IF NOT v_ok THEN");
                sqlBuffer.AppendLine("            BEGIN");
                sqlBuffer.Append("                v_type := c.");
                sqlBuffer.Append(dataTypeNameColumn);
                sqlBuffer.AppendLine(";");
                sqlBuffer.Append(conditionBuffers[i]);
                sqlBuffer.AppendLine();
                sqlBuffer.AppendLine("                    DECLARE");
                sqlBuffer.AppendLine("                        v_dummy VARCHAR2(1);");
                sqlBuffer.AppendLine("                    BEGIN");
                sqlBuffer.AppendLine("                        EXECUTE IMMEDIATE v_sql INTO v_dummy;");
                sqlBuffer.AppendLine("                    END;");
                sqlBuffer.AppendLine();
                sqlBuffer.AppendLine(vTypes[i]);
                sqlBuffer.AppendLine("                    v_ok := TRUE;");
                sqlBuffer.AppendLine("                END IF;");
                sqlBuffer.AppendLine();
                sqlBuffer.AppendLine("            EXCEPTION");
                sqlBuffer.AppendLine("                WHEN OTHERS THEN");
                sqlBuffer.AppendLine("                    NULL;");
                sqlBuffer.AppendLine("            END;");
                sqlBuffer.AppendLine("        END IF;");
                sqlBuffer.AppendLine();
            }

            sqlBuffer.AppendLine("        IF DBMS_LOB.GETLENGTH(v_json) > 1 THEN");
            sqlBuffer.AppendLine("            v_json := v_json || ',';");
            sqlBuffer.AppendLine("        END IF;");
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("        v_json := v_json ||");
            sqlBuffer.AppendLine("            '{' ||");
            sqlBuffer.Append("                '\"");
            sqlBuffer.Append(nameof(DbColumnInfo.Name));
            sqlBuffer.Append("\": \"' || c.");
            sqlBuffer.Append(columnNameColumn);
            sqlBuffer.AppendLine(" || '\",' ||");
            sqlBuffer.Append("                '\"");
            sqlBuffer.Append(nameof(DbColumnInfo.DataType));
            sqlBuffer.Append("\": \"' || c.");
            sqlBuffer.Append(dataTypeNameColumn);
            sqlBuffer.AppendLine(" || '\",' ||");
            sqlBuffer.Append("                '\"");
            sqlBuffer.Append(nameof(DbColumnInfo.CastExpression));
            sqlBuffer.AppendLine("\": ' || CASE WHEN v_ok THEN '\"' || v_type || '\"' ELSE 'null' END || ',' ||");
            sqlBuffer.Append("                '\"");
            sqlBuffer.Append(nameof(DbColumnInfo.IsCastable));
            sqlBuffer.AppendLine("\": ' || CASE WHEN v_ok THEN 'true' ELSE 'false' END ||");
            sqlBuffer.AppendLine("            '}';");
            sqlBuffer.AppendLine("    END LOOP;");
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("    v_json := v_json || ']';");
            sqlBuffer.AppendLine("    :result := v_json;");
            sqlBuffer.AppendLine("END;");

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate UpsertSqlBuilder<TEntity>() where TEntity : class
        {
            return BuildUpsertSql<TEntity>(false, false);
        }

        public override SqlTemplate UpdateRangeSqlBuilder<TEntity>() where TEntity : class
        {
            return BuildUpsertSql<TEntity>(true, true);
        }

        public override SqlTemplate InsertRangeSqlBuilder<TEntity>() where TEntity : class
        {
            string tableName = EntityInfoCache<TEntity>.TableName;
            string schemaName = EntityInfoCache<TEntity>.SchemaName ?? SqlDialectStrategy.DefaultSchemaName;
            ImmutableArray<PropertyInfo> insertProperties = EntityInfoCache<TEntity>.InsertProperties;

            StringBuilder sqlBuffer = new();

            sqlBuffer.Append("INSERT INTO ");
            sqlBuffer.Append(SqlDialectStrategy.RenderIdentifier(schemaName));
            sqlBuffer.Append('.');
            sqlBuffer.Append(SqlDialectStrategy.RenderIdentifier(tableName));
            sqlBuffer.Append(" (");
            sqlBuffer.AppendColumns<TEntity>(SqlDialectStrategy, insertProperties, null, "    ", false, false);
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine(")");
            sqlBuffer.Append(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate UpsertRangeSqlBuilder<TEntity>() where TEntity : class
        {
            return BuildUpsertSql<TEntity>(true, false);
        }

        public override SqlTemplate ExistsSqlBuilder<TEntity>() where TEntity : class
        {
            StringBuilder sqlBuffer = new();

            sqlBuffer.AppendLine("SELECT");
            sqlBuffer.AppendLine("    CASE");
            sqlBuffer.AppendLine("        WHEN EXISTS (");
            sqlBuffer.AppendLine("            SELECT");
            sqlBuffer.Append("                1");
            sqlBuffer.AppendFromTable<TEntity>(SqlDialectStrategy, "            ", null);
            sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
            sqlBuffer.AppendLine("        )");
            sqlBuffer.AppendLine("        THEN");
            sqlBuffer.AppendLine("            1");
            sqlBuffer.AppendLine("        ELSE");
            sqlBuffer.AppendLine("            0");
            sqlBuffer.AppendLine("    END");
            sqlBuffer.AppendLine("FROM");
            sqlBuffer.Append("    DUAL");
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate AvgSqlBuilder<TEntity>() where TEntity : class
        {
            StringBuilder sqlBuffer = new();

            sqlBuffer.AppendLine("SELECT");
            sqlBuffer.AppendLine("    TO_CHAR(");
            sqlBuffer.Append("        AVG(");
            sqlBuffer.Append(SqlDialectStrategy.Placeholder);
            sqlBuffer.AppendLine("),");
            sqlBuffer.AppendLine("        'TM',");
            sqlBuffer.AppendLine("        'NLS_NUMERIC_CHARACTERS=''.,'''");
            sqlBuffer.Append("    )");
            sqlBuffer.AppendFromTable<TEntity>(SqlDialectStrategy, string.Empty, null);
            sqlBuffer.Append(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }
    }
}
