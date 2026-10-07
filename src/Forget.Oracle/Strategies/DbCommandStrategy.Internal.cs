using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Collections.Immutable;
using System.Data.Common;
using System.Reflection;
using System.Text;


namespace Forget.Oracle.Strategies
{
    internal sealed partial class DbCommandStrategy : BaseDbCommandStrategy<SqlBuilderStrategy>
    {
        private List<DbCommandInfo> BuildUpsertRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, IReadOnlyList<PropertyInfo> properties, int batchSize, SqlTemplate sqlTemplate, string indentation) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entities);

            TEntity[] entityArray = entities as TEntity[] ?? [.. entities];

            if (entityArray.Length == 0)
                return [];

            ImmutableDictionary<string, Func<TEntity, object?>> propertyGettersByPropertyName = EntityInfoCache<TEntity>.PropertyGettersByPropertyName;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            string connectionId = SqlDialectStrategy.GetConnectionId(connection);
            IDictionary<string, DbColumnInfo> columns = DbColumnInfoCache<TEntity>.GetDictValue(connectionId);

            if (batchSize <= 0)
                batchSize = entityArray.Length;

            List<DbCommandInfo> commands = new((entityArray.Length + batchSize - 1) / batchSize);

            for (int i = 0; i < entityArray.Length; i += batchSize)
            {
                int end = Math.Min(i + batchSize, entityArray.Length);

                StringBuilder sqlBuffer = new();
                DynamicParameters parameters = new();

                for (int j = i; j < end; j++)
                {
                    sqlBuffer.Append(indentation);
                    sqlBuffer.Append("SELECT ");

                    for (int k = 0; k < properties.Count; k++)
                    {
                        PropertyInfo property = properties[k];
                        string propertyName = property.Name;
                        string parameterName = propertyName + j;
                        object? parameterValue = propertyGettersByPropertyName[propertyName](entityArray[j]);
                        DbColumnInfo column = columns[columnNamesByPropertyName[propertyName]];

                        if (column.IsCastable ?? false)
                            sqlBuffer.Append(column.CastExpression!.Replace(SqlDialectStrategy.Placeholder, SqlDialectStrategy.RenderParameter(parameterName)));
                        else
                            sqlBuffer.Append(SqlDialectStrategy.RenderParameter(parameterName));

                        parameters.Add(parameterName, parameterValue);

                        sqlBuffer.Append(" AS ");
                        sqlBuffer.Append(SqlDialectStrategy.RenderIdentifier(column.Name));
                        sqlBuffer.AppendSeparator(k, properties.Count, true);
                    }

                    sqlBuffer.Append(" FROM DUAL");

                    if (j < end - 1)
                    {
                        sqlBuffer.AppendLine();
                        sqlBuffer.Append(indentation);
                        sqlBuffer.AppendLine("UNION ALL");
                    }
                }

                string sql = sqlTemplate.Render(sqlBuffer);
                commands.Add(new(sql, parameters));
            }

            return commands;
        }
    }
}
