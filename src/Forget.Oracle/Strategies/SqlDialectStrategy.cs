using Forget.Core.Abstractions.Strategies;
using Oracle.ManagedDataAccess.Client;
using System.Data.Common;


namespace Forget.Oracle.Strategies
{
    internal sealed partial class SqlDialectStrategy : BaseSqlDialectStrategy
    {
        public static SqlDialectStrategy Instance { get; } = new();

        public int MaxInValueCount { get; }
        
        private SqlDialectStrategy()
        {
            MaxInValueCount = 1000;
        }

        public override string Terminator { get; } = Environment.NewLine;

        public override string RenderParameter(string name)
        {
            return $":{name}";
        }

        public override string CastAsString(string sql)
        {
            return $"TO_CHAR({sql})";
        }

        protected override string BuildConnectionId(string connectionString)
        {
            OracleConnectionStringBuilder builder = new(connectionString);
            return $"oracle://{builder.DataSource}/{builder.UserID}";
        }
    }
}
