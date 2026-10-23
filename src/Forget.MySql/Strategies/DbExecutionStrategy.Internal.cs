using Forget.Core.Abstractions.Strategies;
using System.Data.Common;


namespace Forget.MySql.Strategies
{
    internal sealed partial class DbExecutionStrategy : BaseDbExecutionStrategy<DbCommandStrategy>
    {
        protected override Task GetColumnsImplAsync<TEntity>(DbConnection connection, bool sync, int? commandTimeout, CancellationToken cancellationToken) where TEntity : class
        {
            return Task.CompletedTask;
        }
    }
}
