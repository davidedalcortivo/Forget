using Forget.Core.Abstractions.Strategies;
using MySqlConnector;
using System.Data.Common;
using System.Text;


namespace Forget.MySql.Strategies
{
    internal sealed partial class SqlDialectStrategy : BaseSqlDialectStrategy
    {
        public static SqlDialectStrategy Instance { get; } = new();

        public override string RenderIdentifier(string name)
        {
            return $"`{name}`";
        }

        public override string Concat(params string[] parts)
        {
            return $"CONCAT({string.Join(", ", parts)})";
        }

        public override string Like(string column, string pattern)
        {
            return $"{column} LIKE {pattern} ESCAPE '\\\\'";
        }

        public override string Pagination(string skipParameter, string takeParameter)
        {
            StringBuilder sqlBuffer = new();

            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("LIMIT");
            sqlBuffer.Append("    ");
            sqlBuffer.AppendLine(takeParameter);
            sqlBuffer.AppendLine("OFFSET");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(skipParameter);

            return sqlBuffer.ToString();
        }

        public override string CastAsString(string sql)
        {
            return $"CAST({sql} AS CHAR)";
        }

        protected override string BuildConnectionId(string connectionString)
        {
            MySqlConnectionStringBuilder builder = new(connectionString);
            return $"mysql://{builder.Server}:{builder.Port}/{builder.Database}";
        }
    }
}
