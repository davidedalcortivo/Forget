using Forget.Core.Abstractions.Models;
using Forget.Core.Models;
using System.Collections;
using System.Data.Common;
using System.Linq.Expressions;


namespace Forget.Core.Abstractions.Strategies
{
    internal interface IDbCommandStrategy
    {
        ISqlDialectStrategy SqlDialectStrategy { get; }

        void LoadRuntimeCache<TEntity>(DbConnection connection) where TEntity : class;
        DbCommandInfo GetColumnsCommand<TEntity>(DbConnection connection) where TEntity : class;
        DbCommandInfo GetAllCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class;
        DbCommandInfo GetAllCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class;
        DbCommandInfo GetFirstCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class;
        DbCommandInfo GetFirstCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class;
        DbCommandInfo GetFirstOrDefaultCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class;
        DbCommandInfo GetFirstOrDefaultCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors) where TEntity : class;
        DbCommandInfo GetSingleCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo GetSingleCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo GetSingleOrDefaultCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo GetSingleOrDefaultCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo GetByIdCommand<TEntity>(DbConnection connection, object id) where TEntity : class;
        DbCommandInfo GetByIdCommand<TEntity, TKey>(DbConnection connection, TKey id) where TEntity : class where TKey : notnull;
        DbCommandInfo GetPageCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors, int? skip, int? take) where TEntity : class;
        DbCommandInfo GetPageCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors, int? skip, int? take) where TEntity : class;
        DbCommandInfo UpdateCommand<TEntity>(DbConnection connection, TEntity entity) where TEntity : class;
        DbCommandInfo UpdateCommand<TEntity>(DbConnection connection, object values, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo UpdateCommand<TEntity>(DbConnection connection, object values, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo InsertCommand<TEntity>(DbConnection connection, TEntity entity) where TEntity : class;
        DbCommandInfo DeleteCommand<TEntity>(DbConnection connection, TEntity entity) where TEntity : class;
        DbCommandInfo DeleteCommand<TEntity>(DbConnection connection, object id) where TEntity : class;
        DbCommandInfo DeleteCommand<TEntity, TKey>(DbConnection connection, TKey id) where TEntity : class where TKey : notnull;
        DbCommandInfo DeleteCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo DeleteCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo UpsertCommand<TEntity>(DbConnection connection, TEntity entity) where TEntity : class;
        IReadOnlyList<DbCommandInfo> GetByIdRangeCommands<TEntity>(DbConnection connection, IEnumerable ids, int batchSize, int chunkSize) where TEntity : class;
        IReadOnlyList<DbCommandInfo> GetByIdRangeCommands<TEntity, TKey>(DbConnection connection, IEnumerable<TKey> ids, int batchSize, int chunkSize) where TEntity : class where TKey : notnull;
        IReadOnlyList<DbCommandInfo> UpdateRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class;
        IReadOnlyList<DbCommandInfo> InsertRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class;
        IReadOnlyList<DbCommandInfo> DeleteRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class;
        IReadOnlyList<DbCommandInfo> DeleteRangeCommands<TEntity>(DbConnection connection, IEnumerable ids, int batchSize, int chunkSize) where TEntity : class;
        IReadOnlyList<DbCommandInfo> DeleteRangeCommands<TEntity, TKey>(DbConnection connection, IEnumerable<TKey> ids, int batchSize, int chunkSize) where TEntity : class where TKey : notnull;
        IReadOnlyList<DbCommandInfo> UpsertRangeCommands<TEntity>(DbConnection connection, IEnumerable<TEntity> entities, int batchSize, int chunkSize) where TEntity : class;
        DbCommandInfo ExistsCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo ExistsCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo CountCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo CountCommand<TEntity>(DbConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo CountCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, object?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo CountCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, object?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo CountCommand<TEntity>(DbConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo CountCommand<TEntity>(DbConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo AvgCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo AvgCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo AvgCommand<TEntity>(DbConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo AvgCommand<TEntity>(DbConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo SumCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo SumCommand<TEntity>(DbConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo SumCommand<TEntity>(DbConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo SumCommand<TEntity>(DbConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo MinCommand<TEntity, TProperty>(DbConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo MinCommand<TEntity, TProperty>(DbConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo MinCommand<TEntity, TProperty>(DbConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo MinCommand<TEntity, TProperty>(DbConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo MaxCommand<TEntity, TProperty>(DbConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo MaxCommand<TEntity, TProperty>(DbConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class;
        DbCommandInfo MaxCommand<TEntity, TProperty>(DbConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class;
        DbCommandInfo MaxCommand<TEntity, TProperty>(DbConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class;
    }
}
