using Forget.Core.Abstractions.Strategies;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;


namespace Forget.Tests.Core
{
    public class ConnectionIdTests
    {
        private static (string Provider, ISqlDialectStrategy Dialect, DbConnection Connection, string Id)[] Providers() =>
        [
            ("MySql", Forget.MySql.Strategies.SqlDialectStrategy.Instance, new MySqlConnection("Server=db1;Port=3307;Database=Sales;User ID=u;Password=p"), "mysql://db1:3307/Sales"),
            ("Oracle", Forget.Oracle.Strategies.SqlDialectStrategy.Instance, new OracleConnection("User Id=SALES;Password=p;Data Source=db1:1521/XEPDB1"), "oracle://db1:1521/XEPDB1/SALES"),
            ("PostgreSql", Forget.PostgreSql.Strategies.SqlDialectStrategy.Instance, new NpgsqlConnection("Host=db1;Port=5433;Database=Sales;Username=u;Password=p"), "postgresql://db1:5433/Sales"),
            ("SqlServer", Forget.SqlServer.Strategies.SqlDialectStrategy.Instance, new SqlConnection("Server=db1,1433;Database=Sales;User Id=u;Password=p;TrustServerCertificate=true"), "sqlserver://db1,1433/Sales")
        ];

        [Fact]
        public void TheIdOfAConnection_IsBuiltFromItsServerAndItsDatabase()
        {
            foreach ((string provider, ISqlDialectStrategy dialect, DbConnection connection, string id) in Providers())
            {
                Assert.True(id == dialect.GetConnectionId(connection), $"{provider}: expected '{id}' but got '{dialect.GetConnectionId(connection)}'");
                Assert.True(id == dialect.GetConnectionId(connection), $"{provider}: the second call returned a different id");
            }
        }

        [Fact]
        public void TwoConnectionsWithTheSameString_HaveTheSameId()
        {
            foreach ((string provider, ISqlDialectStrategy dialect, DbConnection connection, string id) in Providers())
            {
                DbConnection other = (DbConnection)Activator.CreateInstance(connection.GetType(), connection.ConnectionString)!;

                Assert.True(dialect.GetConnectionId(connection) == dialect.GetConnectionId(other), $"{provider}: two connections with the same string have different ids");
            }
        }

        [Fact]
        public void TheIdOfAConnection_IsNotBuiltAgainOnEveryCall()
        {
            const int Calls = 1000;

            foreach ((string provider, ISqlDialectStrategy dialect, DbConnection connection, string id) in Providers())
            {
                dialect.GetConnectionId(connection);

                long before = GC.GetAllocatedBytesForCurrentThread();

                for (int i = 0; i < Calls; i++)
                {
                    _ = connection.ConnectionString;
                }

                long baseline = GC.GetAllocatedBytesForCurrentThread() - before;
                before = GC.GetAllocatedBytesForCurrentThread();

                for (int i = 0; i < Calls; i++)
                {
                    dialect.GetConnectionId(connection);
                }

                long extra = (GC.GetAllocatedBytesForCurrentThread() - before - baseline) / Calls;

                Assert.True(extra < 32, $"{provider}: getting the id allocates {extra} bytes more than reading the connection string, on every call");
            }
        }
    }
}
