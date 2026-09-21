using Forget.Core.Abstractions.Strategies;
using Microsoft.Data.SqlClient;
using System.Data.Common;


namespace Forget.SqlServer.Strategies
{
    internal sealed partial class SqlDialectStrategy : BaseSqlDialectStrategy
    {
        public static SqlDialectStrategy Instance { get; } = new();

        public int MaxParameterCount { get; }
        public int MaxInsertRowCount { get; }
        public HashSet<string> IntDataTypes { get; }

        private SqlDialectStrategy()
        {
            MaxParameterCount = 2098;
            MaxInsertRowCount = 1000;
            IntDataTypes = new(StringComparer.OrdinalIgnoreCase) { "int", "smallint", "tinyint" };
        }

        public override string RenderIdentifier(string name)
        {
            return $"[{name}]";
        }

        public override string Concat(params string[] parts)
        {
            return string.Join(" + ", parts);
        }

        public override string CastAsString(string sql)
        {
            return $"CAST({sql} AS NVARCHAR(MAX))";
        }

        protected override string BuildConnectionId(string connectionString)
        {
            SqlConnectionStringBuilder builder = new(connectionString);
            return $"sqlserver://{builder.DataSource}/{builder.InitialCatalog}";
        }
    }
}
