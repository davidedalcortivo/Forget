using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Collections.Immutable;
using System.Data.Common;
using System.Reflection;
using System.Text;


namespace Forget.MySql.Strategies
{
    internal sealed partial class DbCommandStrategy : BaseDbCommandStrategy<SqlBuilderStrategy>
    {
        public static DbCommandStrategy Instance { get; } = new(SqlBuilderStrategy.Instance);

        private DbCommandStrategy(SqlBuilderStrategy strategy) : base(strategy) { }

        public override DbCommandInfo UpsertCommand<TEntity>(DbConnection connection, TEntity entity) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entity);

            ImmutableArray<PropertyInfo> insertProperties = EntityInfoCache<TEntity>.InsertProperties;
            ImmutableDictionary<string, Func<TEntity, object?>> propertyGettersByPropertyName = EntityInfoCache<TEntity>.PropertyGettersByPropertyName;

            StringBuilder sqlBuffer = new();
            DynamicParameters parameters = new();

            for (int i = 0; i < insertProperties.Length; i++)
            {
                string parameterName = insertProperties[i].Name;
                object? parameterValue = propertyGettersByPropertyName[parameterName](entity);

                sqlBuffer.Append("    ");
                sqlBuffer.AppendAndBindParameter(SqlDialectStrategy, parameters, parameterName, parameterValue);
                sqlBuffer.AppendSeparator(i, insertProperties.Length, false);
            }

            string sql = SqlBuilderCache<TEntity, SqlBuilderStrategy>.UpsertSql.Render(sqlBuffer);
            return new(sql, parameters);
        }

        public override IReadOnlyList<DbCommandInfo> UpdateRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entities);

            TEntity[] entityArray = entities as TEntity[] ?? [.. entities];

            if (entityArray.Length == 0)
                return [];

            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;
            SqlTemplate updateRangeSql = SqlBuilderCache<TEntity, SqlBuilderStrategy>.UpdateRangeSql;

            return BuildRangeCommands(entityArray.Length, batchSize, 0, (start, end, parameters) =>
            {
                StringBuilder sqlBuffer = new();

                for (int j = start; j < end; j++)
                {
                    sqlBuffer.Append("    SELECT ");
                    sqlBuffer.AppendRowParameters(SqlDialectStrategy, parameters, properties, entityArray[j], j);

                    if (j < end - 1)
                    {
                        sqlBuffer.AppendLine();
                        sqlBuffer.AppendLine("    UNION ALL");
                    }
                }

                return updateRangeSql.Render(sqlBuffer);
            }, string.Empty, string.Empty);
        }

        public override IReadOnlyList<DbCommandInfo> UpsertRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entities);

            TEntity[] entityArray = entities as TEntity[] ?? [.. entities];

            if (entityArray.Length == 0)
                return [];

            ImmutableArray<PropertyInfo> insertProperties = EntityInfoCache<TEntity>.InsertProperties;
            SqlTemplate upsertRangeSql = SqlBuilderCache<TEntity, SqlBuilderStrategy>.UpsertRangeSql;

            return BuildRangeCommands(entityArray.Length, batchSize, 0, (start, end, parameters) =>
            {
                StringBuilder sqlBuffer = new();

                for (int j = start; j < end; j++)
                {
                    sqlBuffer.Append("    (");
                    sqlBuffer.AppendRowParameters(SqlDialectStrategy, parameters, insertProperties, entityArray[j], j);
                    sqlBuffer.Append(')');
                    sqlBuffer.AppendSeparator(j, end, false);
                }

                return upsertRangeSql.Render(sqlBuffer);
            }, string.Empty, string.Empty);
        }
    }
}
