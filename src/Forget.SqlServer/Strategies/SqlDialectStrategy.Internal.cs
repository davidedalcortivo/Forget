using Forget.Core.Abstractions.Strategies;
using Microsoft.Data.SqlClient;


namespace Forget.SqlServer.Strategies
{
    internal sealed partial class SqlDialectStrategy : BaseSqlDialectStrategy
    {
        protected override string BuildIdentifier(string name)
        {
            return $"[{name.Replace("]", "]]")}]";
        }

        protected override string BuildConnectionId(string connectionString)
        {
            SqlConnectionStringBuilder builder = new(connectionString);
            return $"sqlserver://{builder.DataSource}/{builder.InitialCatalog}/{builder.UserID}";
        }
    }
}
