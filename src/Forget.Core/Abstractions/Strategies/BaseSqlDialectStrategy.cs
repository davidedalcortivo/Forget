using System.Collections.Concurrent;
using System.Data.Common;
using System.Text;


namespace Forget.Core.Abstractions.Strategies
{
    internal abstract partial class BaseSqlDialectStrategy : ISqlDialectStrategy
    {
        private bool _isInitialized = false;
        private readonly object _lock = new();
        private readonly ConcurrentDictionary<string, string> _connectionIds = new();

        public string DefaultSchemaName { get; protected set; } = null!;
        public virtual string NullValue { get; } = "NULL";
        public virtual string Terminator { get; } = $";{Environment.NewLine}";
        public virtual string Placeholder { get; } = "{}";

        public virtual string RenderIdentifier(string name)
        {
            return $"\"{name}\"";
        }

        public virtual string RenderParameter(string name)
        {
            return $"@{name}";
        }

        public virtual string Concat(params string[] parts)
        {
            return string.Join(" || ", parts);
        }

        public virtual string ToLower(string sql)
        {
            return $"LOWER({sql})";
        }

        public virtual string ToUpper(string sql)
        {
            return $"UPPER({sql})";
        }

        public virtual string Like(string column, string pattern)
        {
            return $"{column} LIKE {pattern} ESCAPE '\\'";
        }

        public virtual string EscapeLike(string value)
        {
            int i = 0;

            for (; i < value.Length; i++)
            {
                char c = value[i];

                if (c == '\\' || c == '%' || c == '_')
                    break;
            }

            if (i >= value.Length)
                return value;

            StringBuilder sb = new(value.Length);

            sb.Append(value, 0, i);

            for (; i < value.Length; i++)
            {
                char c = value[i];

                if (c == '\\' || c == '%' || c == '_')
                    sb.Append('\\');

                sb.Append(c);
            }

            return sb.ToString();
        }

        public virtual string In(string identifier, string parameter, bool mixed)
        {
            return $"{identifier} IN {parameter}";
        }

        public virtual (string, string) In(string identifier, bool mixed)
        {
            return ($"{identifier} IN ", string.Empty);
        }

        public virtual string IsNull(string column)
        {
            return $"{column} IS NULL";
        }

        public virtual string IsNotNull(string column)
        {
            return $"{column} IS NOT NULL";
        }

        public virtual string IsTrue(string column)
        {
            return $"{column} = 1";
        }

        public virtual string Pagination(string skipParameter, string takeParameter)
        {
            StringBuilder sqlBuffer = new();

            sqlBuffer.AppendLine();
            sqlBuffer.AppendLine("OFFSET");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(skipParameter);
            sqlBuffer.AppendLine(" ROWS");
            sqlBuffer.AppendLine("FETCH NEXT");
            sqlBuffer.Append("    ");
            sqlBuffer.Append(takeParameter);
            sqlBuffer.Append(" ROWS ONLY");

            return sqlBuffer.ToString();
        }

        public abstract string CastAsString(string sql);

        protected abstract string BuildConnectionId(string connectionString);

        public string GetConnectionId(DbConnection connection)
        {
            return _connectionIds.GetOrAdd(connection.ConnectionString, static (connectionString, dialect) => dialect.BuildConnectionId(connectionString), this);
        }

        public void Initialize(DbConnection connection)
        {
            if (!_isInitialized)
            {
                lock (_lock)
                {
                    if (!_isInitialized)
                    {
                        InitializeImpl(connection);
                        _isInitialized = true;
                    }
                }
            }
        }
    }
}
