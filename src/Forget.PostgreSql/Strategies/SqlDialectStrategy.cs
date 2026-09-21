using Forget.Core.Abstractions.Strategies;
using Npgsql;
using System.Data.Common;
using System.Text;


namespace Forget.PostgreSql.Strategies
{
    internal sealed partial class SqlDialectStrategy : BaseSqlDialectStrategy
    {
        public static SqlDialectStrategy Instance { get; } = new();

        public override string In(string identifier, string parameter, bool mixed)
        {
            if (mixed)
                return base.In(identifier, parameter, mixed);

            return $"{identifier} = ANY({parameter})";
        }

        public override (string, string) In(string identifier, bool mixed)
        {
            if (mixed)
                return base.In(identifier, mixed);

            return ($"{identifier} = ANY(", ")");
        }

        public override string IsTrue(string column)
        {
            return $"{column} = TRUE";
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
            return $"{sql}::text";
        }

        protected override string BuildConnectionId(string connectionString)
        {
            NpgsqlConnectionStringBuilder builder = new(connectionString);
            return $"postgresql://{builder.Host}:{builder.Port}/{builder.Database}";
        }
    }
}
