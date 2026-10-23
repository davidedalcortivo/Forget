using Forget.Core.Abstractions.Strategies;
using Forget.Core.Models;
using System.Data.Common;


namespace Forget.SqlServer.Strategies
{
    internal sealed partial class DbExecutionStrategy : BaseDbExecutionStrategy<DbCommandStrategy>
    {
        public static DbExecutionStrategy Instance { get; } = new(DbCommandStrategy.Instance);

        private DbExecutionStrategy(DbCommandStrategy strategy) : base(strategy) { }

        public override async Task<int> UpsertImplAsync<TEntity>(DbConnection connection, bool sync, TEntity entity, DbTransaction? transaction, int? commandTimeout, CancellationToken cancellationToken) where TEntity : class
        {
            EnsureTransaction(connection, transaction);

            DbCommandInfo command = dbCommandStrategy.UpsertCommand(connection, entity);
            return await ExecuteRangeImplAsync(connection, sync, [command], transaction, commandTimeout, cancellationToken);
        }
    }
}
