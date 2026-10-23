using Forget.Core.Abstractions.Strategies;
using Oracle.ManagedDataAccess.Client;


namespace Forget.Oracle.Strategies
{
    internal sealed partial class SqlDialectStrategy : BaseSqlDialectStrategy
    {
        protected override string BuildConnectionId(string connectionString)
        {
            OracleConnectionStringBuilder builder = new(connectionString);
            return $"oracle://{builder.DataSource}/{builder.UserID}";
        }
    }
}
