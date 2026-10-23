using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Collections.Immutable;
using System.Data.Common;
using System.Reflection;
using System.Text;


namespace Forget.SqlServer.Strategies
{
    internal sealed partial class DbCommandStrategy : BaseDbCommandStrategy<SqlBuilderStrategy>
    {
        public static DbCommandStrategy Instance { get; } = new(SqlBuilderStrategy.Instance);

        private DbCommandStrategy(SqlBuilderStrategy strategy) : base(strategy) { }

        public override DbCommandInfo UpsertCommand<TEntity>(DbConnection connection, TEntity entity) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entity);

            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            ImmutableArray<PropertyInfo> insertProperties = EntityInfoCache<TEntity>.InsertProperties;
            ImmutableArray<PropertyInfo> upsertProperties = EntityInfoCache<TEntity>.UpsertProperties;
            ImmutableArray<PropertyInfo> upsertKeyProperties = EntityInfoCache<TEntity>.UpsertKeyProperties;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;
            ImmutableDictionary<string, Func<TEntity, object?>> propertyGettersByPropertyName = EntityInfoCache<TEntity>.PropertyGettersByPropertyName;

            StringBuilder updateBuffer = new();
            StringBuilder insertBuffer = new();
            StringBuilder upsertKeyBuffer = new();
            DynamicParameters parameters = new();

            for (int i = 0; i < upsertProperties.Length; i++)
            {
                string parameterName = upsertProperties[i].Name;
                object? parameterValue = propertyGettersByPropertyName[parameterName](entity);

                updateBuffer.Append("    ");
                updateBuffer.Append(SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[parameterName]));
                updateBuffer.Append(" = ");
                updateBuffer.AppendAndBindParameter(SqlDialectStrategy, parameters, parameterName, parameterValue);
                updateBuffer.AppendSeparator(i, upsertProperties.Length, false);
            }

            for (int i = 0; i < insertProperties.Length; i++)
            {
                string parameterName = insertProperties[i].Name;
                object? parameterValue = propertyGettersByPropertyName[parameterName](entity);

                insertBuffer.Append("        ");
                insertBuffer.AppendAndBindParameter(SqlDialectStrategy, parameters, parameterName, parameterValue);
                insertBuffer.AppendSeparator(i, insertProperties.Length, false);
            }

            for (int i = 0; i < upsertKeyProperties.Length; i++)
            {
                string parameterName = upsertKeyProperties[i].Name;
                object? parameterValue = propertyGettersByPropertyName[parameterName](entity);

                string column = SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[parameterName]);
                string parameter = SqlDialectStrategy.RenderParameter(parameterName);

                upsertKeyBuffer.AppendNullSafeEquals(column, parameter, i, "    ");
                parameters.Add(parameterName, parameterValue);
            }

            string sql = SqlBuilderCache<TEntity, SqlBuilderStrategy>.UpsertSql.Render(updateBuffer, upsertKeyBuffer, insertBuffer);
            return new(sql, parameters);
        }

        public override IReadOnlyList<DbCommandInfo> UpdateRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class
        {
            return BuildUpsertRangeCommands(entities, batchSize, chunkSize, SqlBuilderCache<TEntity, SqlBuilderStrategy>.UpdateRangeSql, true);
        }

        public override IReadOnlyList<DbCommandInfo> UpsertRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class
        {
            return BuildUpsertRangeCommands(entities, batchSize, chunkSize, SqlBuilderCache<TEntity, SqlBuilderStrategy>.UpsertRangeSql, false);
        }
    }
}
