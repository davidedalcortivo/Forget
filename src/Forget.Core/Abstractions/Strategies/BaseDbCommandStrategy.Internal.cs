using Dapper;
using Forget.Core.Abstractions.Models;
using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Collections;
using System.Collections.Immutable;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;


namespace Forget.Core.Abstractions.Strategies
{
    internal abstract partial class BaseDbCommandStrategy<TStrategy> : IDbCommandStrategy where TStrategy : ISqlBuilderStrategy
    {
        protected virtual void EnsureIdType<TEntity>(PropertyInfo idProperty, object? id) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(id);
            PropertyHelper.EnsureValueType<TEntity>(idProperty, id.GetType());
        }

        protected virtual void EnsureIdType<TEntity, TKey>(PropertyInfo idProperty, TKey id) where TEntity : class where TKey : notnull
        {
            if (!typeof(TKey).IsValueType)
                ArgumentNullException.ThrowIfNull(id);

            PropertyHelper.EnsureValueType<TEntity>(idProperty, typeof(TKey));
        }

        protected virtual List<object> ToIdList<TEntity>(PropertyInfo idProperty, IEnumerable ids) where TEntity : class
        {
            List<object> idList = ids is ICollection collection ? new(collection.Count) : [];
            Type? idType = null;

            foreach (object? id in ids)
            {
                EnsureIdType<TEntity>(idProperty, id);

                Type _idType = id.GetType();
                idType ??= _idType;

                if (_idType != idType)
                    throw new ArgumentException($"All the ids must have the same type, but '{idType}' and '{_idType}' were both provided for the entity '{typeof(TEntity).Name}'.");

                idList.Add(id);
            }

            return idList;
        }

        protected virtual (string?, DynamicParameters?) Translate<TEntity>(ISqlDialectStrategy sqlDialectStrategy, Expression<Func<TEntity, bool>>? predicate, DynamicParameters? parameters) where TEntity : class
        {
            string? clause = null;

            if (predicate is not null)
                (clause, parameters) = ExpressionTranslator<TEntity>.Translate(SqlDialectStrategy, predicate, parameters);

            return (clause, parameters);
        }

        protected virtual (string?, DynamicParameters?) Translate<TEntity>(ISqlDialectStrategy sqlDialectStrategy, IFilterNode<TEntity>? filterNode, DynamicParameters? parameters) where TEntity : class
        {
            string? clause = null;

            if (filterNode is not null)
                (clause, parameters) = FilterNodeTranslator<TEntity>.Translate(SqlDialectStrategy, filterNode, parameters);

            return (clause, parameters);
        }

        protected virtual DbCommandInfo BuildGetFirstCommand<TEntity>(string? clause, DynamicParameters? parameters, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors, int take) where TEntity : class
        {
            StringBuilder sqlBuffer = new();
            parameters ??= new();

            sqlBuffer.AppendWhereClause(clause, string.Empty);
            sqlBuffer.AppendSort(SqlDialectStrategy, sortDescriptors, true);

            string takeName = "Take";
            parameters.Add(takeName, take);

            string sql = SqlBuilderCache<TEntity, TStrategy>.GetFirstSql.Render(sqlBuffer, SqlDialectStrategy.RenderParameter(takeName));
            return new(sql, parameters);
        }

        protected virtual DbCommandInfo BuildGetByIdCommand<TEntity>(PropertyInfo idProperty, object id) where TEntity : class
        {
            DynamicParameters parameters = new();
            string idParameterName = idProperty.Name;

            string sql = SqlBuilderCache<TEntity, TStrategy>.GetByIdSql.Render(SqlDialectStrategy.RenderParameter(idParameterName));
            parameters.Add(idParameterName, id);
            return new(sql, parameters);
        }

        protected virtual DbCommandInfo BuildGetPageCommand<TEntity>(string? clause, DynamicParameters? parameters, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors, int? skip, int? take) where TEntity : class
        {
            bool useSkip = skip is not null && skip >= 0;
            bool useTake = take is not null && take >= 0;

            if (!useSkip && useTake)
                return BuildGetFirstCommand(clause, parameters, sortDescriptors, take!.Value);

            StringBuilder sqlBuffer = new();
            sqlBuffer.AppendWhereClause(clause, string.Empty);

            if (useSkip)
            {
                string skipName = "Skip";
                string takeName = "Take";

                sqlBuffer.AppendSort(SqlDialectStrategy, sortDescriptors, true);
                sqlBuffer.Append(SqlDialectStrategy.Pagination(SqlDialectStrategy.RenderParameter(skipName), SqlDialectStrategy.RenderParameter(takeName)));

                parameters ??= new();
                parameters.Add(skipName, skip);

                if (useTake)
                    parameters.Add(takeName, take);
                else
                    parameters.Add(takeName, long.MaxValue);
            }
            else
            {
                sqlBuffer.AppendSort(SqlDialectStrategy, sortDescriptors, false);
            }

            string sql = SqlBuilderCache<TEntity, TStrategy>.GetAllSql.Render(sqlBuffer);
            return new(sql, parameters);
        }

        protected virtual DbCommandInfo BuildUpdateCommand<TEntity>(IReadOnlyList<PropertyInfo> properties, Func<string, object?> propertyGetter, string? clause, DynamicParameters? parameters) where TEntity : class
        {
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            StringBuilder sqlBuffer = new();
            parameters ??= new();

            for (int i = 0; i < properties.Count; i++)
            {
                string parameterName = properties[i].Name;
                object? parameterValue = propertyGetter(parameterName);

                sqlBuffer.Append("    ");
                sqlBuffer.Append(SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[parameterName]));
                sqlBuffer.Append(" = ");
                sqlBuffer.AppendAndBindParameter(SqlDialectStrategy, parameters, parameterName, parameterValue);
                sqlBuffer.AppendSeparator(i, properties.Count, false);
            }

            sqlBuffer.AppendWhereClause(clause, string.Empty);

            string sql = SqlBuilderCache<TEntity, TStrategy>.UpdateSql.Render(sqlBuffer);
            return new(sql, parameters);
        }

        protected virtual DbCommandInfo BuildDeleteCommand<TEntity>(string? clause, DynamicParameters? parameters) where TEntity : class
        {
            StringBuilder sqlBuffer = new();
            sqlBuffer.AppendWhereClause(clause, string.Empty);

            string sql = SqlBuilderCache<TEntity, TStrategy>.DeleteSql.Render(sqlBuffer);
            return new(sql, parameters);
        }

        protected virtual DbCommandInfo BuildDeleteCommand<TEntity>(PropertyInfo idProperty, object id) where TEntity : class
        {
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            DynamicParameters parameters = new();
            string idParameterName = idProperty.Name;

            string clause = $"{SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[idParameterName])} = {SqlDialectStrategy.RenderParameter(idParameterName)}";
            parameters.Add(idParameterName, id);

            return BuildDeleteCommand<TEntity>(clause, parameters);
        }

        protected virtual List<DbCommandInfo> BuildInRangeCommands(ISqlDialectStrategy sqlDialectStrategy, SqlTemplate sqlTemplate, IReadOnlyList<object> idList, int batchSize, int chunkSize, PropertyInfo idProperty, bool useUnion)
        {
            if (idList.Count == 0)
                return [];

            Type idType = idList[0].GetType();
            Type elementType = idType.IsEnum ? Enum.GetUnderlyingType(idType) : idType;

            if (elementType == typeof(byte))
                elementType = typeof(short);

            bool convert = elementType != idType;

            return BuildInRangeCommands(sqlDialectStrategy, sqlTemplate, idList.Count, (start, end) =>
            {
                Array idArray = Array.CreateInstance(elementType, end - start);

                for (int i = start; i < end; i++)
                    idArray.SetValue(convert ? Convert.ChangeType(idList[i], elementType) : idList[i], i - start);

                return idArray;
            }, batchSize, chunkSize, idProperty, useUnion);
        }

        protected virtual List<DbCommandInfo> BuildRangeCommands(int count, int batchSize, int chunkSize, Func<int, int, DynamicParameters, string> renderChunk, string chunkSeparator, string batchTerminator)
        {
            if (count == 0)
                return [];

            if (batchSize <= 0)
                batchSize = count;

            List<DbCommandInfo> commands = new((count + batchSize - 1) / batchSize);
            StringBuilder batchBuffer = new();
            DynamicParameters parameters = new();
            int _batchSize = batchSize;
            int s = 0;

            for (int i = 0; i < count; i += _batchSize)
            {
                if (chunkSize > 0 && chunkSize < batchSize)
                    _batchSize = Math.Min(chunkSize, batchSize - s);

                int end = Math.Min(i + _batchSize, count);

                batchBuffer.Append(renderChunk(i, end, parameters));
                s += end - i;

                if (s >= batchSize || end >= count)
                {
                    batchBuffer.Append(batchTerminator);
                    commands.Add(new(batchBuffer.ToString(), parameters));
                    batchBuffer.Clear();
                    parameters = new();
                    s = 0;
                }
                else
                {
                    batchBuffer.Append(chunkSeparator);
                }
            }

            return commands;
        }

        protected virtual List<DbCommandInfo> BuildInRangeCommands(ISqlDialectStrategy sqlDialectStrategy, SqlTemplate sqlTemplate, int idCount, Func<int, int, Array> createIdArray, int batchSize, int chunkSize, PropertyInfo idProperty, bool useUnion)
        {
            string chunkSeparator = useUnion ? $"{Environment.NewLine}UNION ALL{Environment.NewLine}" : $"{sqlDialectStrategy.Terminator}{Environment.NewLine}";
            int j = 0;

            return BuildRangeCommands(idCount, batchSize, chunkSize, (start, end, parameters) =>
            {
                string parameterName = $"{idProperty.Name}Array_{j}_";
                parameters.Add(parameterName, createIdArray(start, end));
                j++;

                return sqlTemplate.RenderWithoutLastTerminator(sqlDialectStrategy.RenderParameter(parameterName));
            }, chunkSeparator, sqlDialectStrategy.Terminator);
        }

        protected virtual List<DbCommandInfo> BuildInRangeCommands<TEntity, TKey>(SqlTemplate sqlTemplate, IEnumerable<TKey> ids, int batchSize, int chunkSize, bool useUnion) where TEntity : class where TKey : notnull
        {
            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            PropertyHelper.EnsureValueType<TEntity>(idProperty, typeof(TKey));

            TKey[] idArray = [.. ids];

            if (!typeof(TKey).IsValueType)
            {
                foreach (TKey id in idArray)
                    ArgumentNullException.ThrowIfNull(id);
            }

            if (idArray is byte[] byteIdArray)
            {
                short[] shortIdArray = Array.ConvertAll(byteIdArray, x => (short)x);
                return BuildInRangeCommands(SqlDialectStrategy, sqlTemplate, shortIdArray.Length, (start, end) => start == 0 && end == shortIdArray.Length ? shortIdArray : shortIdArray[start..end], batchSize, chunkSize, idProperty, useUnion);
            }

            return BuildInRangeCommands(SqlDialectStrategy, sqlTemplate, idArray.Length, (start, end) => start == 0 && end == idArray.Length ? idArray : idArray[start..end], batchSize, chunkSize, idProperty, useUnion);
        }

        protected virtual DbCommandInfo BuildExistsCommand<TEntity>(string? clause, DynamicParameters? parameters) where TEntity : class
        {
            StringBuilder sqlBuffer = new();
            sqlBuffer.AppendWhereClause(clause, "            ");

            string sql = SqlBuilderCache<TEntity, TStrategy>.ExistsSql.Render(sqlBuffer);
            return new(sql, parameters);
        }

        protected virtual DbCommandInfo BuildAggregateCommand<TEntity>(SqlTemplate sqlTemplate, PropertyInfo? property, string? clause, DynamicParameters? parameters) where TEntity : class
        {
            string column;

            if (property is null)
            {
                column = "*";
            }
            else
            {
                ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;
                column = SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[property.Name]);
            }

            StringBuilder sqlBuffer = new();

            sqlBuffer.AppendWhereClause(clause, string.Empty);

            string sql = sqlTemplate.Render(column, sqlBuffer);
            return new(sql, parameters);
        }

        protected virtual DbCommandInfo BuildSumOrAvgCommand<TEntity>(DbConnection connection, SqlTemplate sqlTemplate, PropertyInfo property, string? clause, DynamicParameters? parameters) where TEntity : class
        {
            return BuildAggregateCommand<TEntity>(sqlTemplate, property, clause, parameters);
        }
    }
}
