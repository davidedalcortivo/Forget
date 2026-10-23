using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using System.Data.Common;


namespace Forget.SqlServer.Strategies
{
    internal sealed partial class DbExecutionStrategy : BaseDbExecutionStrategy<DbCommandStrategy>
    {
        protected override async Task GetColumnsImplAsync<TEntity>(DbConnection connection, bool sync, int? commandTimeout, CancellationToken cancellationToken) where TEntity : class
        {
            string connectionId = SqlDialectStrategy.GetConnectionId(connection);
            IDictionary<string, DbColumnInfo>? columns = DbColumnInfoCache<TEntity>.GetDictValueOrDefault(connectionId);

            if (columns is null)
            {
                SemaphoreSlim semaphore = DbColumnInfoCache<TEntity>.GetSemaphore(connectionId);

                if (sync)
                    semaphore.Wait(cancellationToken);
                else
                    await semaphore.WaitAsync(cancellationToken);

                try
                {
                    columns = DbColumnInfoCache<TEntity>.GetDictValueOrDefault(connectionId);

                    if (columns is null)
                    {
                        DbCommandInfo command = dbCommandStrategy.GetColumnsCommand<TEntity>(connection);
                        columns = (await QueryImplAsync<DbColumnInfo>(connection, sync, command, null, null, commandTimeout, cancellationToken))
                            .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

                        DbColumnInfoCache<TEntity>.Add(connectionId, columns);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }
    }
}
