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
        private SqlTemplate BuildUpsertSql<TEntity>(bool isRange) where TEntity : class
        {
            string tableName = EntityInfoCache<TEntity>.TableName;
            string? schemaName = EntityInfoCache<TEntity>.SchemaName;
            ImmutableArray<PropertyInfo> insertProperties = EntityInfoCache<TEntity>.InsertProperties;
            ImmutableArray<PropertyInfo> upsertProperties = EntityInfoCache<TEntity>.UpsertProperties;
            ImmutableArray<PropertyInfo> upsertKeyProperties = EntityInfoCache<TEntity>.UpsertKeyProperties;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            StringBuilder sqlBuffer = new();

            sqlBuffer.Append("INSERT INTO ");
            sqlBuffer.Append(SqlDialectStrategy.RenderTable(schemaName, tableName));
            sqlBuffer.Append(" (");
            sqlBuffer.AppendColumns<TEntity>(SqlDialectStrategy, insertProperties, null, "    ", false, false);
            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine(")");
            sqlBuffer.Append("VALUES");

            if (isRange)
            {
                sqlBuffer.AppendLine();
                sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
            }
            else
            {
                sqlBuffer.AppendLine(" (");
                sqlBuffer.AppendLine(SqlDialectStrategy.Placeholder);
                sqlBuffer.AppendLine(")");
            }
            
            sqlBuffer.Append("ON CONFLICT (");

            for (int i = 0; i < upsertKeyProperties.Length; i++)
            {
                sqlBuffer.Append(SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[upsertKeyProperties[i].Name]));

                if (i < upsertKeyProperties.Length - 1)
                    sqlBuffer.Append(", ");
            }

            sqlBuffer.Append(") DO UPDATE SET");
            sqlBuffer.AppendSetColumns<TEntity>(SqlDialectStrategy, upsertProperties, "EXCLUDED", null, "    ");
            sqlBuffer.Append(SqlDialectStrategy.Terminator);
            return new(sqlBuffer.ToString(), SqlDialectStrategy.Terminator, SqlDialectStrategy.Placeholder);
        }
    }
}
