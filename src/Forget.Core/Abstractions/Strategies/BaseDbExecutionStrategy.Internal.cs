using Dapper;
using Forget.Core.Models;
using System.Data;
using System.Data.Common;


namespace Forget.Core.Abstractions.Strategies
{
    internal abstract partial class BaseDbExecutionStrategy<TStrategy> : IDbExecutionStrategy where TStrategy : IDbCommandStrategy
    {
        protected virtual void EnsureTransaction(DbConnection connection, DbTransaction? transaction)
        {
            if (transaction is not null && transaction.Connection != connection)
                throw new ArgumentException("The provided transaction does not belong to the current connection.");
        }

        protected virtual async Task<IReadOnlyList<TEntity>> QueryImplAsync<TEntity>(DbConnection connection, bool sync, DbCommandInfo command, int? take, DbTransaction? transaction, int? commandTimeout, CancellationToken cancellationToken) where TEntity : class
        {
            EnsureTransaction(connection, transaction);

            if (take == 0)
                return [];

            if (sync)
                return connection.Query<TEntity>(command.Sql, command.Parameters, transaction, true, commandTimeout).AsList();

            return (await connection.QueryAsync<TEntity>(new CommandDefinition(command.Sql, command.Parameters, transaction, commandTimeout, cancellationToken: cancellationToken))).AsList();
        }

        protected virtual async Task<TEntity> QueryFirstImplAsync<TEntity>(DbConnection connection, bool sync, DbCommandInfo command, DbTransaction? transaction, int? commandTimeout, CancellationToken cancellationToken) where TEntity : class
        {
            EnsureTransaction(connection, transaction);

            if (sync)
                return connection.QueryFirst<TEntity>(command.Sql, command.Parameters, transaction, commandTimeout);

            return await connection.QueryFirstAsync<TEntity>(new CommandDefinition(command.Sql, command.Parameters, transaction, commandTimeout, cancellationToken: cancellationToken));
        }

        protected virtual async Task<TEntity?> QueryFirstOrDefaultImplAsync<TEntity>(DbConnection connection, bool sync, DbCommandInfo command, DbTransaction? transaction, int? commandTimeout, CancellationToken cancellationToken) where TEntity : class
        {
            EnsureTransaction(connection, transaction);

            if (sync)
                return connection.QueryFirstOrDefault<TEntity?>(command.Sql, command.Parameters, transaction, commandTimeout);

            return await connection.QueryFirstOrDefaultAsync<TEntity?>(new CommandDefinition(command.Sql, command.Parameters, transaction, commandTimeout, cancellationToken: cancellationToken));
        }

        protected virtual async Task<TEntity> QuerySingleImplAsync<TEntity>(DbConnection connection, bool sync, DbCommandInfo command, DbTransaction? transaction, int? commandTimeout, CancellationToken cancellationToken) where TEntity : class
        {
            EnsureTransaction(connection, transaction);

            if (sync)
                return connection.QuerySingle<TEntity>(command.Sql, command.Parameters, transaction, commandTimeout);

            return await connection.QuerySingleAsync<TEntity>(new CommandDefinition(command.Sql, command.Parameters, transaction, commandTimeout, cancellationToken: cancellationToken));
        }

        protected virtual async Task<TEntity?> QuerySingleOrDefaultImplAsync<TEntity>(DbConnection connection, bool sync, DbCommandInfo command, DbTransaction? transaction, int? commandTimeout, CancellationToken cancellationToken) where TEntity : class
        {
            EnsureTransaction(connection, transaction);

            if (sync)
                return connection.QuerySingleOrDefault<TEntity?>(command.Sql, command.Parameters, transaction, commandTimeout);

            return await connection.QuerySingleOrDefaultAsync<TEntity?>(new CommandDefinition(command.Sql, command.Parameters, transaction, commandTimeout, cancellationToken: cancellationToken));
        }

        protected virtual async Task<int> ExecuteImplAsync(DbConnection connection, bool sync, DbCommandInfo command, DbTransaction? transaction, int? commandTimeout, CancellationToken cancellationToken)
        {
            EnsureTransaction(connection, transaction);

            if (sync)
                return connection.Execute(command.Sql, command.Parameters, transaction, commandTimeout);

            return await connection.ExecuteAsync(new CommandDefinition(command.Sql, command.Parameters, transaction, commandTimeout, cancellationToken: cancellationToken));
        }

        protected virtual async Task<int> ExecuteRangeImplAsync(DbConnection connection, bool sync, IReadOnlyList<DbCommandInfo> commands, DbTransaction? transaction, int? commandTimeout, CancellationToken cancellationToken)
        {
            EnsureTransaction(connection, transaction);

            int result = 0;

            if (commands.Count == 0)
                return result;

            bool ownsConnection = connection.State == ConnectionState.Closed;
            bool ownsTransaction = transaction is null;
            DbTransaction? _transaction = transaction;
            DbTransaction? tempTransaction = null;

            try
            {
                if (ownsConnection)
                {
                    if (sync)
                        connection.Open();
                    else
                        await connection.OpenAsync(cancellationToken);
                }

                if (ownsTransaction)
                {
                    if (sync)
                        tempTransaction = connection.BeginTransaction();
                    else
                        tempTransaction = await connection.BeginTransactionAsync(cancellationToken);
                }

                _transaction ??= tempTransaction!;

                foreach (DbCommandInfo command in commands)
                    result += await ExecuteImplAsync(connection, sync, command, _transaction, commandTimeout, cancellationToken);

                if (ownsTransaction)
                {
                    if (sync)
                        _transaction.Commit();
                    else
                        await _transaction.CommitAsync(cancellationToken);
                }
            }
            catch
            {
                try
                {
                    if (ownsTransaction && _transaction is not null)
                    {
                        if (sync)
                            _transaction.Rollback();
                        else
                            await _transaction.RollbackAsync(cancellationToken);
                    }
                }
                catch
                {

                }

                throw;
            }
            finally
            {
                if (tempTransaction is not null)
                {
                    if (sync)
                        tempTransaction.Dispose();
                    else
                        await tempTransaction.DisposeAsync();
                }

                if (ownsConnection)
                {
                    if (sync)
                        connection.Close();
                    else
                        await connection.CloseAsync();
                }
            }

            return result;
        }

        protected virtual async Task<TProperty?> ExecuteScalarImplAsync<TProperty>(DbConnection connection, bool sync, DbCommandInfo command, DbTransaction? transaction, int? commandTimeout, CancellationToken cancellationToken)
        {
            EnsureTransaction(connection, transaction);

            if (sync)
                return connection.ExecuteScalar<TProperty?>(command.Sql, command.Parameters, transaction, commandTimeout);

            return await connection.ExecuteScalarAsync<TProperty?>(new CommandDefinition(command.Sql, command.Parameters, transaction, commandTimeout, cancellationToken: cancellationToken));
        }
    }
}
