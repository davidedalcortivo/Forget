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
        protected override DbCommandInfo BuildGetFirstCommand<TEntity>(string? clause, DynamicParameters? parameters, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors, int take) where TEntity : class
        {
            StringBuilder sqlBuffer = new();
            parameters ??= new();

            sqlBuffer.AppendWhereClause(clause, string.Empty);
            sqlBuffer.AppendSort(SqlDialectStrategy, sortDescriptors, true);

            string takeName = "Take";
            parameters.Add(takeName, take);

            string sql = SqlBuilderCache<TEntity, SqlBuilderStrategy>.GetFirstSql.Render(SqlDialectStrategy.RenderParameter(takeName), sqlBuffer);
            return new(sql, parameters);
        }

        protected override DbCommandInfo BuildExistsCommand<TEntity>(string? clause, DynamicParameters? parameters) where TEntity : class
        {
            StringBuilder sqlBuffer = new();
            sqlBuffer.AppendWhereClause(clause, "                ");

            string sql = SqlBuilderCache<TEntity, SqlBuilderStrategy>.ExistsSql.Render(sqlBuffer);
            return new(sql, parameters);
        }

        private List<DbCommandInfo> BuildUpsertRangeCommands<TEntity>(IEnumerable<TEntity> entities, int batchSize, int chunkSize, SqlTemplate sqlTemplate, bool updateOnly) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entities);

            TEntity[] entityArray = entities as TEntity[] ?? [.. entities];

            if (entityArray.Length == 0)
                return [];

            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;

            return BuildRangeCommands(entityArray.Length, batchSize, chunkSize, (start, end, parameters) =>
            {
                StringBuilder sqlBuffer = new();

                for (int j = start; j < end; j++)
                {
                    sqlBuffer.Append("        (");
                    sqlBuffer.AppendRowParameters(SqlDialectStrategy, parameters, properties, entityArray[j], j);
                    sqlBuffer.Append(')');
                    sqlBuffer.AppendSeparator(j, end, false);
                }

                return updateOnly ? sqlTemplate.Render(sqlBuffer) : sqlTemplate.Render(sqlBuffer, sqlBuffer);
            }, Environment.NewLine, string.Empty);
        }

        protected override DbCommandInfo BuildSumOrAvgCommand<TEntity>(DbConnection connection, SqlTemplate sqlTemplate, PropertyInfo property, string? clause, DynamicParameters? parameters) where TEntity : class
        {
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            string connectionId = SqlDialectStrategy.GetConnectionId(connection);
            IDictionary<string, DbColumnInfo> columns = DbColumnInfoCache<TEntity>.GetDictValue(connectionId);

            string columnName = columnNamesByPropertyName[property.Name];
            string column;

            if (((SqlDialectStrategy)SqlDialectStrategy).IntDataTypes.Contains(columns[columnName].DataType ?? string.Empty))
                column = $"CAST({SqlDialectStrategy.RenderIdentifier(columnName)} AS BIGINT)";
            else
                column = SqlDialectStrategy.RenderIdentifier(columnName);

            StringBuilder sqlBuffer = new();
            sqlBuffer.AppendWhereClause(clause, string.Empty);

            string sql = sqlTemplate.Render(column, sqlBuffer);
            return new(sql, parameters);
        }
    }
}
