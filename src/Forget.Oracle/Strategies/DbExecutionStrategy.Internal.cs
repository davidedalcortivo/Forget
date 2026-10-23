using Forget.Core.Abstractions.Strategies;
using Forget.Core.Caching;
using Forget.Core.Models;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using System.Data;
using System.Data.Common;
using System.Text.Json;


namespace Forget.Oracle.Strategies
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
                        OracleConnection _connection = (OracleConnection)connection;
                        bool ownsConnection = _connection.State == ConnectionState.Closed;

                        try
                        {
                            if (ownsConnection)
                            {
                                if (sync)
                                    _connection.Open();
                                else
                                    await _connection.OpenAsync(cancellationToken);
                            }

                            DbCommandInfo command = dbCommandStrategy.GetColumnsCommand<TEntity>(_connection);
                            string schemaName = "SchemaName";
                            string tableName = "TableName";

                            using OracleCommand oracleCommand = _connection.CreateCommand();
                            using OracleParameter schemaParameter = new(schemaName, OracleDbType.Varchar2, ParameterDirection.Input);
                            using OracleParameter tableParameter = new(tableName, OracleDbType.Varchar2, ParameterDirection.Input);
                            using OracleParameter resultParameter = new("result", OracleDbType.Clob, ParameterDirection.Output);

                            oracleCommand.BindByName = true;
                            oracleCommand.CommandText = command.Sql;

                            if (commandTimeout.HasValue)
                                oracleCommand.CommandTimeout = commandTimeout.Value;

                            oracleCommand.Parameters.Add(schemaParameter).Value = (object?)command.Parameters!.Get<string?>(schemaName) ?? DBNull.Value;
                            oracleCommand.Parameters.Add(tableParameter).Value = command.Parameters!.Get<string>(tableName);
                            oracleCommand.Parameters.Add(resultParameter);

                            if (sync)
                                _ = oracleCommand.ExecuteNonQuery();
                            else
                                _ = await oracleCommand.ExecuteNonQueryAsync(cancellationToken);

                            string json = ((OracleClob)resultParameter.Value).Value;
                            columns = (JsonSerializer.Deserialize<List<DbColumnInfo>>(json, _jsonOptions) ?? [])
                                .ToDictionary(x => x.Name, x => x, StringComparer.OrdinalIgnoreCase);

                            DbColumnInfoCache<TEntity>.Add(connectionId, columns);
                        }
                        finally
                        {
                            if (ownsConnection)
                            {
                                if (sync)
                                    _connection.Close();
                                else
                                    await _connection.CloseAsync();
                            }
                        }
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
