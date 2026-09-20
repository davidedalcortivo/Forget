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
        public ISqlDialectStrategy SqlDialectStrategy { get; }

        protected readonly TStrategy sqlBuilderStrategy;

        protected BaseDbCommandStrategy(TStrategy sqlBuilderStrategy)
        {
            this.sqlBuilderStrategy = sqlBuilderStrategy;
            SqlDialectStrategy = this.sqlBuilderStrategy.SqlDialectStrategy;
        }

        public virtual void LoadRuntimeCache<TEntity>(DbConnection connection) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(connection);

            SqlDialectStrategy.Initialize(connection);
            SqlBuilderCache<TEntity, TStrategy>.Initialize(sqlBuilderStrategy);
        }

        public virtual DbCommandInfo GetColumnsCommand<TEntity>(DbConnection connection) where TEntity : class
        {
            string table = EntityInfoCache<TEntity>.TableName;
            string schema = EntityInfoCache<TEntity>.SchemaName ?? SqlDialectStrategy.DefaultSchemaName;

            DynamicParameters parameters = new();

            string schemaName = "SchemaName";
            string tableName = "TableName";
            parameters.Add(schemaName, schema);
            parameters.Add(tableName, table);

            string sql = SqlBuilderCache<TEntity, TStrategy>.GetColumnsSql.Render(SqlDialectStrategy.RenderParameter(schemaName), SqlDialectStrategy.RenderParameter(tableName));
            return new(sql, parameters);
        }

        public virtual DbCommandInfo GetAllCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildGetPageCommand(clause, parameters, sortDescriptors, null, null);
        }

        public virtual DbCommandInfo GetAllCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildGetPageCommand(clause, parameters, sortDescriptors, null, null);
        }

        public virtual DbCommandInfo GetFirstCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildGetFirstCommand(clause, parameters, sortDescriptors, 1);
        }

        public virtual DbCommandInfo GetFirstCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildGetFirstCommand(clause, parameters, sortDescriptors, 1);
        }

        public virtual DbCommandInfo GetFirstOrDefaultCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildGetFirstCommand(clause, parameters, sortDescriptors, 1);
        }

        public virtual DbCommandInfo GetFirstOrDefaultCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildGetFirstCommand(clause, parameters, sortDescriptors, 1);
        }

        public virtual DbCommandInfo GetSingleCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildGetPageCommand<TEntity>(clause, parameters, null, null, null);
        }

        public virtual DbCommandInfo GetSingleCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildGetPageCommand<TEntity>(clause, parameters, null, null, null);
        }

        public virtual DbCommandInfo GetSingleOrDefaultCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildGetPageCommand<TEntity>(clause, parameters, null, null, null);
        }

        public virtual DbCommandInfo GetSingleOrDefaultCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildGetPageCommand<TEntity>(clause, parameters, null, null, null);
        }

        public virtual DbCommandInfo GetByIdCommand<TEntity>(DbConnection connection, object id) where TEntity : class
        {
            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            EnsureIdType<TEntity>(idProperty, id);

            DynamicParameters parameters = new();
            string idParameterName = idProperty.Name;

            string sql = SqlBuilderCache<TEntity, TStrategy>.GetByIdSql.Render(SqlDialectStrategy.RenderParameter(idParameterName));
            parameters.Add(idParameterName, id);
            return new(sql, parameters);
        }

        public virtual DbCommandInfo GetPageCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors, int? skip, int? take) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildGetPageCommand(clause, parameters, sortDescriptors, skip, take);
        }

        public virtual DbCommandInfo GetPageCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors, int? skip, int? take) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildGetPageCommand(clause, parameters, sortDescriptors, skip, take);
        }

        public virtual DbCommandInfo UpdateCommand<TEntity>(DbConnection connection, TEntity entity) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entity);

            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            ImmutableArray<PropertyInfo> updateProperties = EntityInfoCache<TEntity>.UpdateProperties;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;
            ImmutableDictionary<string, Func<TEntity, object?>> propertyGettersByPropertyName = EntityInfoCache<TEntity>.PropertyGettersByPropertyName;

            DynamicParameters parameters = new();
            string idParameterName = idProperty.Name;

            string clause = $"{SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[idParameterName])} = {SqlDialectStrategy.RenderParameter(idParameterName)}";
            parameters.Add(idParameterName, propertyGettersByPropertyName[idParameterName](entity));

            return BuildUpdateCommand<TEntity>(updateProperties, x => propertyGettersByPropertyName[x](entity), clause, parameters);
        }

        public virtual DbCommandInfo UpdateCommand<TEntity>(DbConnection connection, object values, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo[] paramProperties = ValuesPropertyCache<TEntity>.Get(values);
            ImmutableDictionary<string, Func<object, object?>> paramPropertyGetters = ValuesGetterCache<TEntity>.Get(values);

            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildUpdateCommand<TEntity>(paramProperties, x => paramPropertyGetters[x](values), clause, parameters);
        }

        public virtual DbCommandInfo UpdateCommand<TEntity>(DbConnection connection, object values, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo[] paramProperties = ValuesPropertyCache<TEntity>.Get(values);
            ImmutableDictionary<string, Func<object, object?>> paramPropertyGetters = ValuesGetterCache<TEntity>.Get(values);

            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildUpdateCommand<TEntity>(paramProperties, x => paramPropertyGetters[x](values), clause, parameters);
        }

        public virtual DbCommandInfo InsertCommand<TEntity>(DbConnection connection, TEntity entity) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entity);

            ImmutableArray<PropertyInfo> insertProperties = EntityInfoCache<TEntity>.InsertProperties;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;
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

            string sql = SqlBuilderCache<TEntity, TStrategy>.InsertSql.Render(sqlBuffer);
            return new(sql, parameters);
        }

        public virtual DbCommandInfo DeleteCommand<TEntity>(DbConnection connection, TEntity entity) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entity);

            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;
            ImmutableDictionary<string, Func<TEntity, object?>> propertyGettersByPropertyName = EntityInfoCache<TEntity>.PropertyGettersByPropertyName;

            DynamicParameters parameters = new();
            string idParameterName = idProperty.Name;

            string clause = $"{SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[idParameterName])} = {SqlDialectStrategy.RenderParameter(idParameterName)}";
            parameters.Add(idParameterName, propertyGettersByPropertyName[idParameterName](entity));

            return BuildDeleteCommand<TEntity>(clause, parameters);
        }

        public virtual DbCommandInfo DeleteCommand<TEntity>(DbConnection connection, object id) where TEntity : class
        {
            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            EnsureIdType<TEntity>(idProperty, id);

            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            DynamicParameters parameters = new();
            string idParameterName = idProperty.Name;

            string clause = $"{SqlDialectStrategy.RenderIdentifier(columnNamesByPropertyName[idParameterName])} = {SqlDialectStrategy.RenderParameter(idParameterName)}";
            parameters.Add(idParameterName, id);

            return BuildDeleteCommand<TEntity>(clause, parameters);
        }

        public virtual DbCommandInfo DeleteCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildDeleteCommand<TEntity>(clause, parameters);
        }

        public virtual DbCommandInfo DeleteCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildDeleteCommand<TEntity>(clause, parameters);
        }

        public abstract DbCommandInfo UpsertCommand<TEntity>(DbConnection connection, TEntity entity) where TEntity : class;

        public virtual IReadOnlyList<DbCommandInfo> GetByIdRangeCommands<TEntity>(DbConnection connection, IEnumerable ids, int batchSize, int chunkSize) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(ids);

            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            List<object> idList = ToIdList<TEntity>(idProperty, ids);

            return BuildInRangeCommands(SqlDialectStrategy, SqlBuilderCache<TEntity, TStrategy>.GetByIdRangeSql, idList, batchSize, chunkSize, idProperty, true);
        }

        public abstract IReadOnlyList<DbCommandInfo> UpdateRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class;

        public virtual IReadOnlyList<DbCommandInfo> InsertRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entities);

            TEntity[] entityArray = entities as TEntity[] ?? [.. entities];
            List<DbCommandInfo> commands = [];

            if (entityArray.Length == 0)
                return commands;

            ImmutableArray<PropertyInfo> insertProperties = EntityInfoCache<TEntity>.InsertProperties;
            ImmutableDictionary<string, Func<TEntity, object?>> propertyGettersByPropertyName = EntityInfoCache<TEntity>.PropertyGettersByPropertyName;
            SqlTemplate insertRangeSql = SqlBuilderCache<TEntity, TStrategy>.InsertRangeSql;

            if (batchSize <= 0)
                batchSize = entityArray.Length;

            StringBuilder batchBuffer = new();
            DynamicParameters parameters = new();
            int _batchSize = batchSize;
            int s = 0;

            for (int i = 0; i < entityArray.Length; i += _batchSize)
            {
                if (chunkSize > 0 && chunkSize < batchSize)
                    _batchSize = Math.Min(chunkSize, batchSize - s);

                int end = Math.Min(i + _batchSize, entityArray.Length);
                StringBuilder sqlBuffer = new();

                for (int j = i; j < end; j++)
                {
                    sqlBuffer.Append("    (");

                    for (int k = 0; k < insertProperties.Length; k++)
                    {
                        string parameterName = insertProperties[k].Name;
                        object? parameterValue = propertyGettersByPropertyName[parameterName](entityArray[j]);

                        sqlBuffer.AppendAndBindParameter(SqlDialectStrategy, parameters, parameterName + j, parameterValue);
                        sqlBuffer.AppendSeparator(k, insertProperties.Length, true);
                    }

                    sqlBuffer.Append(')');
                    sqlBuffer.AppendSeparator(j, end, false);

                    s++;
                }

                batchBuffer.Append(insertRangeSql.Render(sqlBuffer));

                if (s >= batchSize || end >= entityArray.Length)
                {
                    commands.Add(new(batchBuffer.ToString(), parameters));
                    batchBuffer.Clear();
                    parameters = new();
                    s = 0;
                }
                else
                {
                    batchBuffer.AppendLine();
                }
            }

            return commands;
        }

        public virtual IReadOnlyList<DbCommandInfo> DeleteRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(entities);

            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            Func<TEntity, object?> idGetter = EntityInfoCache<TEntity>.PropertyGettersByPropertyName[idProperty.Name];
            List<object> idList = [.. entities.Select(x => idGetter(x)!)];

            return BuildInRangeCommands(SqlDialectStrategy, SqlBuilderCache<TEntity, TStrategy>.DeleteRangeSql, idList, batchSize, chunkSize, idProperty, false);
        }

        public virtual IReadOnlyList<DbCommandInfo> DeleteRangeCommands<TEntity>(DbConnection connection, IEnumerable ids, int batchSize, int chunkSize) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(ids);

            PropertyInfo idProperty = EntityInfoCache<TEntity>.IdProperty;
            List<object> idList = ToIdList<TEntity>(idProperty, ids);

            return BuildInRangeCommands(SqlDialectStrategy, SqlBuilderCache<TEntity, TStrategy>.DeleteRangeSql, idList, batchSize, chunkSize, idProperty, false);
        }

        public abstract IReadOnlyList<DbCommandInfo> UpsertRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class;

        public virtual DbCommandInfo ExistsCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildExistsCommand<TEntity>(clause, parameters);
        }

        public virtual DbCommandInfo ExistsCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildExistsCommand<TEntity>(clause, parameters);
        }

        public virtual DbCommandInfo CountCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.CountSql, null, clause, parameters);
        }

        public virtual DbCommandInfo CountCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.CountSql, null, clause, parameters);
        }

        public virtual DbCommandInfo CountCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, object?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.CountSql, property, clause, parameters);
        }

        public virtual DbCommandInfo CountCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, object?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.CountSql, property, clause, parameters);
        }

        public virtual DbCommandInfo CountCommand<TEntity>(DbConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.CountSql, property, clause, parameters);
        }

        public virtual DbCommandInfo CountCommand<TEntity>(DbConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.CountSql, property, clause, parameters);
        }

        public virtual DbCommandInfo AvgCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.AvgSql, property, clause, parameters);
        }

        public virtual DbCommandInfo AvgCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.AvgSql, property, clause, parameters);
        }

        public virtual DbCommandInfo AvgCommand<TEntity>(DbConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.AvgSql, property, clause, parameters);
        }

        public virtual DbCommandInfo AvgCommand<TEntity>(DbConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.AvgSql, property, clause, parameters);
        }

        public virtual DbCommandInfo SumCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.SumSql, property, clause, parameters);
        }

        public virtual DbCommandInfo SumCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.SumSql, property, clause, parameters);
        }

        public virtual DbCommandInfo SumCommand<TEntity>(DbConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.SumSql, property, clause, parameters);
        }

        public virtual DbCommandInfo SumCommand<TEntity>(DbConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.SumSql, property, clause, parameters);
        }

        public virtual DbCommandInfo MinCommand<TEntity, TProperty>(DbConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.MinSql, property, clause, parameters);
        }

        public virtual DbCommandInfo MinCommand<TEntity, TProperty>(DbConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.MinSql, property, clause, parameters);
        }

        public virtual DbCommandInfo MinCommand<TEntity, TProperty>(DbConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            PropertyHelper.EnsureResultType<TEntity>(property, typeof(TProperty));
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.MinSql, property, clause, parameters);
        }

        public virtual DbCommandInfo MinCommand<TEntity, TProperty>(DbConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            PropertyHelper.EnsureResultType<TEntity>(property, typeof(TProperty));
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.MinSql, property, clause, parameters);
        }

        public virtual DbCommandInfo MaxCommand<TEntity, TProperty>(DbConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.MaxSql, property, clause, parameters);
        }

        public virtual DbCommandInfo MaxCommand<TEntity, TProperty>(DbConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.MaxSql, property, clause, parameters);
        }

        public virtual DbCommandInfo MaxCommand<TEntity, TProperty>(DbConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            PropertyHelper.EnsureResultType<TEntity>(property, typeof(TProperty));
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, predicate, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.MaxSql, property, clause, parameters);
        }

        public virtual DbCommandInfo MaxCommand<TEntity, TProperty>(DbConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            PropertyHelper.EnsureResultType<TEntity>(property, typeof(TProperty));
            (string? clause, DynamicParameters? parameters) = Translate(SqlDialectStrategy, filterNode, null);
            return BuildAggregateCommand<TEntity>(SqlBuilderCache<TEntity, TStrategy>.MaxSql, property, clause, parameters);
        }
    }
}
