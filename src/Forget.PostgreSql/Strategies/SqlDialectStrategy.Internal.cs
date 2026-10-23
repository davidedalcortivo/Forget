using Forget.Core.Abstractions.Strategies;
using Npgsql;


namespace Forget.PostgreSql.Strategies
{
    internal sealed partial class SqlDialectStrategy : BaseSqlDialectStrategy
    {
        protected override string BuildConnectionId(string connectionString)
        {
            NpgsqlConnectionStringBuilder builder = new(connectionString);
            return $"postgresql://{builder.Host}:{builder.Port}/{builder.Database}/{builder.SearchPath}";
        }
    }
}
