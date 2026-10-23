using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;


namespace Forget.SqlServer.Strategies
{
    internal sealed partial class SqlBuilderStrategy : BaseSqlBuilderStrategy<SqlDialectStrategy>
    {
        public static SqlBuilderStrategy Instance { get; } = new(Strategies.SqlDialectStrategy.Instance);

        private SqlBuilderStrategy(SqlDialectStrategy strategy) : base(strategy) { }

        public override SqlTemplate GetColumnsSqlBuilder<TEntity>()
        {
            string sysTable = SqlDialectStrategy.RenderIdentifier("sys");
            string columnsTable = $"{sysTable}.{SqlDialectStrategy.RenderIdentifier("columns")}";
            string typesTable = $"{sysTable}.{SqlDialectStrategy.RenderIdentifier("types")}";
            string tablesTable = $"{sysTable}.{SqlDialectStrategy.RenderIdentifier("tables")}";
            string coTable = SqlDialectStrategy.RenderIdentifier("co");
            string tyTable = SqlDialectStrategy.RenderIdentifier("ty");
            string taTable = SqlDialectStrategy.RenderIdentifier("ta");
            string nameColumn = SqlDialectStrategy.RenderIdentifier("name");
            string systemTypeIdColumn = SqlDialectStrategy.RenderIdentifier("system_type_id");
            string userTypeIdColumn = SqlDialectStrategy.RenderIdentifier("user_type_id");
            string objectIdColumn = SqlDialectStrategy.RenderIdentifier("object_id");
            string newLine = Environment.NewLine;

            StringBuilder sqlBuffer = new();

            sqlBuffer.AppendLine("SELECT");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(coTable);
            sqlBuffer.Append('.');
            sqlBuffer.Append(nameColumn);
            sqlBuffer.Append(" AS ");
            sqlBuffer.Append(SqlDialectStrategy.RenderIdentifier(nameof(DbColumnInfo.Name)));
            sqlBuffer.AppendLine(",");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(tyTable);
            sqlBuffer.Append('.');
            sqlBuffer.Append(nameColumn);
            sqlBuffer.Append(" AS ");
            sqlBuffer.AppendLine(SqlDialectStrategy.RenderIdentifier(nameof(DbColumnInfo.DataType)));
            sqlBuffer.AppendLine("FROM");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(columnsTable);
            sqlBuffer.Append(" AS ");
            sqlBuffer.AppendLine(coTable);
            sqlBuffer.AppendLine("INNER JOIN");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(typesTable);
            sqlBuffer.Append(" AS ");
            sqlBuffer.Append(tyTable);
            sqlBuffer.AppendOnClause($"{coTable}.{systemTypeIdColumn} = {tyTable}.{systemTypeIdColumn}" + newLine + $"    AND {tyTable}.{systemTypeIdColumn} = {tyTable}.{userTypeIdColumn}", string.Empty);
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("INNER JOIN");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(tablesTable);
            sqlBuffer.Append(" AS ");
            sqlBuffer.Append(taTable);
            sqlBuffer.AppendOnClause($"{coTable}.{objectIdColumn} = {taTable}.{objectIdColumn}", string.Empty);
            sqlBuffer.AppendWhereClause($"{taTable}.{objectIdColumn} = OBJECT_ID(ISNULL(QUOTENAME({SqlDialectStrategy.Placeholder}) + '.', '') + QUOTENAME({SqlDialectStrategy.Placeholder}))", string.Empty);
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate GetFirstSqlBuilder<TEntity>() where TEntity : class
        {
            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;

            StringBuilder sqlBuffer = new();

            sqlBuffer.Append("SELECT TOP (");
            sqlBuffer.Append(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append(')');
            sqlBuffer.AppendColumns<TEntity>(SqlDialectStrategy, properties, null, "    ", true, false);
            sqlBuffer.AppendFromTable<TEntity>(SqlDialectStrategy, string.Empty, null);
            sqlBuffer.Append(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate UpsertSqlBuilder<TEntity>() where TEntity : class
        {
            string tableName = EntityInfoCache<TEntity>.TableName;
            string? schemaName = EntityInfoCache<TEntity>.SchemaName;
            ImmutableArray<PropertyInfo> insertProperties = EntityInfoCache<TEntity>.InsertProperties;

            string table = SqlDialectStrategy.RenderTable(schemaName, tableName);

            StringBuilder sqlBuffer = new();

            sqlBuffer.Append("UPDATE ");
            sqlBuffer.AppendLine(table);
            sqlBuffer.AppendLine("WITH (UPDLOCK, HOLDLOCK)");
            sqlBuffer.AppendLine("SET");
            sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
            sqlBuffer.AppendLine("WHERE");
            sqlBuffer.Append(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append(SqlDialectStrategy.Terminator);
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("IF @@ROWCOUNT = 0");
            sqlBuffer.AppendLine("BEGIN");
            sqlBuffer.Append("    INSERT INTO ");
            sqlBuffer.Append(table);
            sqlBuffer.Append(" (");
            sqlBuffer.AppendColumns<TEntity>(SqlDialectStrategy, insertProperties, null, "        ", false, false);
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("    )");
            sqlBuffer.AppendLine("    VALUES (");
            sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append("    )");
            sqlBuffer.Append(SqlDialectStrategy.Terminator);
            sqlBuffer.AppendLine("END");

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate UpdateRangeSqlBuilder<TEntity>() where TEntity : class
        {
            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            string idColumn = SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[idProperty.Name]);
            string sourceTable = SqlDialectStrategy.RenderIdentifier("Source");
            string targetTable = SqlDialectStrategy.RenderIdentifier("Target");

            StringBuilder sqlBuffer = new();
            string clause = $"{targetTable}.{idColumn} = {sourceTable}.{idColumn}";

            AppendUpdateRange<TEntity>(sqlBuffer, EntityInfoCache<TEntity>.UpdateProperties, sourceTable, targetTable, clause);
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate UpsertRangeSqlBuilder<TEntity>() where TEntity : class
        {
            string tableName = EntityInfoCache<TEntity>.TableName;
            string? schemaName = EntityInfoCache<TEntity>.SchemaName;
            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;
            ImmutableArray<PropertyInfo> insertProperties = EntityInfoCache<TEntity>.InsertProperties;
            ImmutableArray<PropertyInfo> upsertKeyProperties = EntityInfoCache<TEntity>.UpsertKeyProperties;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            string sourceTable = SqlDialectStrategy.RenderIdentifier("Source");
            string targetTable = SqlDialectStrategy.RenderIdentifier("Target");

            StringBuilder sqlBuffer = new();

            AppendUpdateRange<TEntity>(sqlBuffer, EntityInfoCache<TEntity>.UpsertProperties, sourceTable, targetTable, null);
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("ON");

            for (int i = 0; i < upsertKeyProperties.Length; i++)
            {
                string propertyColumn = SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[upsertKeyProperties[i].Name]);

                sqlBuffer.AppendNullSafeEquals($"{targetTable}.{propertyColumn}", $"{sourceTable}.{propertyColumn}", i, "    ");
            }

            sqlBuffer.Append(SqlDialectStrategy.Terminator);
            sqlBuffer.AppendLine();
            sqlBuffer.Append("INSERT INTO ");
            sqlBuffer.Append(SqlDialectStrategy.RenderTable(schemaName, tableName));
            sqlBuffer.Append(" (");
            sqlBuffer.AppendColumns<TEntity>(SqlDialectStrategy, insertProperties, null, "    ", false, false);
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine(")");
            sqlBuffer.Append("SELECT");
            sqlBuffer.AppendColumns<TEntity>(SqlDialectStrategy, insertProperties, null, "    ", true, false);
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("FROM (");
            sqlBuffer.AppendLine("    VALUES");
            sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append(") AS ");
            sqlBuffer.Append(sourceTable);
            sqlBuffer.Append(" (");
            sqlBuffer.AppendColumns<TEntity>(SqlDialectStrategy, properties, null, string.Empty, false, true);
            sqlBuffer.AppendLine(")");
            sqlBuffer.AppendLine("WHERE NOT EXISTS (");
            sqlBuffer.AppendLine("    SELECT");
            sqlBuffer.Append("        1");
            sqlBuffer.AppendFromTable<TEntity>(SqlDialectStrategy, "    ", targetTable);
            sqlBuffer.AppendLine();
            sqlBuffer.Append("        WITH (UPDLOCK, HOLDLOCK)");
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("    WHERE");

            for (int i = 0; i < upsertKeyProperties.Length; i++)
            {
                string propertyColumn = SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[upsertKeyProperties[i].Name]);

                sqlBuffer.AppendNullSafeEquals($"{targetTable}.{propertyColumn}", $"{sourceTable}.{propertyColumn}", i, "        ");
            }

            sqlBuffer.AppendLine();
            sqlBuffer.Append(')');
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate ExistsSqlBuilder<TEntity>() where TEntity : class
        {
            StringBuilder sqlBuffer = new();

            sqlBuffer.AppendLine("SELECT");
            sqlBuffer.AppendLine("    CAST(");
            sqlBuffer.AppendLine("        CASE");
            sqlBuffer.AppendLine("            WHEN EXISTS (");
            sqlBuffer.AppendLine("                SELECT");
            sqlBuffer.Append("                    1");
            sqlBuffer.AppendFromTable<TEntity>(SqlDialectStrategy, "                ", null);
            sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
            sqlBuffer.AppendLine("            )");
            sqlBuffer.AppendLine("            THEN");
            sqlBuffer.AppendLine("                1");
            sqlBuffer.AppendLine("            ELSE");
            sqlBuffer.AppendLine("                0");
            sqlBuffer.AppendLine("        END AS BIT");
            sqlBuffer.Append("    )");
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate CountSqlBuilder<TEntity>() where TEntity : class
        {
            return BuildAggregateSql<TEntity>($"COUNT_BIG({SqlDialectStrategy.Placeholder})");
        }
    }
}
