using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;


namespace Forget.PostgreSql.Strategies
{
    internal sealed partial class SqlBuilderStrategy : BaseSqlBuilderStrategy<SqlDialectStrategy>
    {
        public static SqlBuilderStrategy Instance { get; } = new(Strategies.SqlDialectStrategy.Instance);

        private SqlBuilderStrategy(SqlDialectStrategy strategy) : base(strategy) { }

        public override SqlTemplate GetColumnsSqlBuilder<TEntity>()
        {
            string pgAttributeTable = SqlDialectStrategy.RenderIdentifier("pg_attribute");
            string aTable = SqlDialectStrategy.RenderIdentifier("a");
            string attnameColumn = $"{aTable}.{SqlDialectStrategy.RenderIdentifier("attname")}";
            string attnumColumn = $"{aTable}.{SqlDialectStrategy.RenderIdentifier("attnum")}";
            string attrelidColumn = $"{aTable}.{SqlDialectStrategy.RenderIdentifier("attrelid")}";
            string attisdroppedColumn = $"{aTable}.{SqlDialectStrategy.RenderIdentifier("attisdropped")}";
            string newLine = Environment.NewLine;

            StringBuilder sqlBuffer = new();

            sqlBuffer.AppendLine("SELECT");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(attnameColumn);
            sqlBuffer.Append(" AS ");
            sqlBuffer.Append(SqlDialectStrategy.RenderIdentifier(nameof(DbColumnInfo.Name)));
            sqlBuffer.AppendLine(",");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(attnumColumn);
            sqlBuffer.Append(" AS ");
            sqlBuffer.AppendLine(SqlDialectStrategy.RenderIdentifier(nameof(DbColumnInfo.OrdinalPosition)));
            sqlBuffer.AppendLine("FROM");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(pgAttributeTable);
            sqlBuffer.Append(" AS ");
            sqlBuffer.Append(aTable);
            sqlBuffer.AppendWhereClause($"{attrelidColumn} = to_regclass(COALESCE(quote_ident({SqlDialectStrategy.Placeholder}) || '.', '') || quote_ident({SqlDialectStrategy.Placeholder}))" + newLine + $"    AND {attnumColumn} > 0" + newLine + $"    AND NOT {attisdroppedColumn}", string.Empty);
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("ORDER BY");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(attnumColumn);
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate GetFirstSqlBuilder<TEntity>() where TEntity : class
        {
            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;

            StringBuilder sqlBuffer = new();

            sqlBuffer.Append("SELECT");
            sqlBuffer.AppendColumns<TEntity>(SqlDialectStrategy, properties, null, "    ", true, false);
            sqlBuffer.AppendFromTable<TEntity>(SqlDialectStrategy, string.Empty, null);
            sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
            sqlBuffer.AppendLine("LIMIT");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate UpsertSqlBuilder<TEntity>() where TEntity : class
        {
            return BuildUpsertSql<TEntity>(false);
        }

        public override SqlTemplate UpdateRangeSqlBuilder<TEntity>() where TEntity : class
        {
            string tableName = EntityInfoCache<TEntity>.TableName;
            string? schemaName = EntityInfoCache<TEntity>.SchemaName;
            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;
            ImmutableArray<PropertyInfo> updateProperties = EntityInfoCache<TEntity>.UpdateProperties;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            string idColumn = SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[idProperty.Name]);
            string sourceTable = SqlDialectStrategy.RenderIdentifier("source");
            string targetTable = SqlDialectStrategy.RenderIdentifier("target");
            string dataTable = SqlDialectStrategy.RenderIdentifier("data");
            string row = SqlDialectStrategy.RenderIdentifier("row");
            string clause = $"{targetTable}.{idColumn} = {sourceTable}.{idColumn}";

            StringBuilder sqlBuffer = new();

            sqlBuffer.Append("UPDATE ");
            sqlBuffer.Append(SqlDialectStrategy.RenderTable(schemaName, tableName));
            sqlBuffer.Append(" AS ");
            sqlBuffer.AppendLine(targetTable);
            sqlBuffer.Append("SET");
            sqlBuffer.AppendSetColumns<TEntity>(SqlDialectStrategy, updateProperties, sourceTable, null, "    ");
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("FROM (");
            sqlBuffer.Append("    SELECT");
            sqlBuffer.AppendColumns<TEntity>(SqlDialectStrategy, properties, $"({dataTable}.{row})", "        ", false, false);
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("    FROM (");
            sqlBuffer.AppendLine("        VALUES");
            sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append("    ) AS ");
            sqlBuffer.Append(dataTable);
            sqlBuffer.Append(" (");
            sqlBuffer.Append(row);
            sqlBuffer.AppendLine(")");
            sqlBuffer.Append(") AS ");
            sqlBuffer.Append(sourceTable);
            sqlBuffer.AppendWhereClause(clause, string.Empty);
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }

        public override SqlTemplate UpsertRangeSqlBuilder<TEntity>() where TEntity : class
        {
            return BuildUpsertSql<TEntity>(true);
        }

        public override SqlTemplate ExistsSqlBuilder<TEntity>() where TEntity : class
        {
            StringBuilder sqlBuffer = new();

            sqlBuffer.AppendLine("SELECT");
            sqlBuffer.AppendLine("    EXISTS (");
            sqlBuffer.AppendLine("        SELECT");
            sqlBuffer.Append("            1");
            sqlBuffer.AppendFromTable<TEntity>(SqlDialectStrategy, "        ", null);
            sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
            sqlBuffer.Append("    )");
            sqlBuffer.Append(SqlDialectStrategy.Terminator);

            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }
    }
}
