using Dapper;
using Forget.Core.Abstractions.Models;
using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Collections;
using System.Collections.Immutable;
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
            Type? underlyingIdType = idType.IsEnum ? Enum.GetUnderlyingType(idType) : null;
            Type elementType = underlyingIdType ?? idType;

            return BuildInRangeCommands(sqlDialectStrategy, sqlTemplate, idList.Count, idType, (start, end) =>
            {
                Array idArray = Array.CreateInstance(elementType, end - start);

                for (int i = start; i < end; i++)
                    idArray.SetValue(underlyingIdType is null ? idList[i] : Convert.ChangeType(idList[i], underlyingIdType), i - start);

                return idArray;
            }, batchSize, chunkSize, idProperty, useUnion);
        }

        protected virtual List<DbCommandInfo> BuildInRangeCommands(ISqlDialectStrategy sqlDialectStrategy, SqlTemplate sqlTemplate, int idCount, Type idType, Func<int, int, Array> createIdArray, int batchSize, int chunkSize, PropertyInfo idProperty, bool useUnion)
        {
            if (idCount == 0)
                return [];

            if (batchSize <= 0)
                batchSize = idCount;

            List<DbCommandInfo> commands = new((idCount + batchSize - 1) / batchSize);
            StringBuilder batchBuffer = new();
            DynamicParameters parameters = new();
            int _batchSize = batchSize;
            int j = 0;
            int s = 0;

            if (idType == typeof(byte))
                throw new ArgumentException("A list of bytes cannot be used as ids: the drivers bind a byte array as one binary value.");

            for (int i = 0; i < idCount; i += _batchSize)
            {
                if (chunkSize > 0 && chunkSize < batchSize)
                    _batchSize = Math.Min(chunkSize, batchSize - s);

                int end = Math.Min(i + _batchSize, idCount);
                StringBuilder sqlBuffer = new();

                string parameterName = $"{idProperty.Name}Array{j}";
                sqlBuffer.Append(sqlDialectStrategy.RenderParameter(parameterName));

                parameters.Add(parameterName, createIdArray(i, end));

                j++;
                s += end - i;

                batchBuffer.Append(sqlTemplate.RenderWithoutLastTerminator(sqlBuffer));

                if (s >= batchSize || end >= idCount)
                {
                    batchBuffer.Append(sqlDialectStrategy.Terminator);
                    commands.Add(new(batchBuffer.ToString(), parameters));
                    batchBuffer.Clear();
                    parameters = new();
                    s = 0;
                }
                else if (useUnion)
                {
                    batchBuffer.AppendLine();
                    batchBuffer.AppendLine("UNION ALL");
                }
                else
                {
                    batchBuffer.Append(sqlDialectStrategy.Terminator);
                    batchBuffer.AppendLine();
                }
            }

            return commands;
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

            return BuildInRangeCommands(SqlDialectStrategy, sqlTemplate, idArray.Length, typeof(TKey), (start, end) => start == 0 && end == idArray.Length ? idArray : idArray[start..end], batchSize, chunkSize, idProperty, useUnion);
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
    }
}
