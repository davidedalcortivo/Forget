using Forget.Core.Abstractions.Strategies;
using MySqlConnector;


namespace Forget.MySql.Strategies
{
    internal sealed partial class SqlDialectStrategy : BaseSqlDialectStrategy
    {
        protected override string BuildIdentifier(string name)
        {
            return $"`{name.Replace("`", "``")}`";
        }

        protected override string BuildConnectionId(string connectionString)
        {
            MySqlConnectionStringBuilder builder = new(connectionString);
            return $"mysql://{builder.Server}:{builder.Port}/{builder.Database}";
        }
    }
}
