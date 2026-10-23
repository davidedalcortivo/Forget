using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Utilities;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;


namespace Forget.Core.Models
{
    internal sealed partial class SqlTranslationContext
    {
        private const string _parameterPrefix = "p";
        private readonly Stack<StringBuilder> _stack;

        public ISqlDialectStrategy SqlDialectStrategy { get; }
        public int ParameterIndex { get; set; }
        public StringBuilder SqlBuffer { get; private set; }
        public DynamicParameters? Parameters { get; private set; }

        public SqlTranslationContext(ISqlDialectStrategy sqlDialectStrategy, DynamicParameters? parameters = null)
        {
            _stack = new();
            SqlDialectStrategy = sqlDialectStrategy;
            SqlBuffer = new();
            Parameters = parameters;
        }

        public void Push()
        {
            _stack.Push(SqlBuffer);
            SqlBuffer = new();
        }

        public string Pop()
        {
            string sql = SqlBuffer.ToString();
            SqlBuffer = _stack.Pop();
            return sql;
        }

        public string AddParameter(object? value)
        {
            Parameters ??= new();

            string name = $"{_parameterPrefix}_{ParameterIndex}";

            if (value is IEnumerable && value is not string && value is not byte[])
                name += "_";

            Parameters.Add(name, value);
            ParameterIndex++;

            return SqlDialectStrategy.RenderParameter(name);
        }

        public string AddParameterList(IEnumerable values)
        {
            Parameters ??= new();

            string name = $"{_parameterPrefix}_{ParameterIndex}_";
            ParameterIndex++;

            StringBuilder sqlBuffer = new();
            int i = 0;

            sqlBuffer.Append('(');

            foreach (object? value in values)
            {
                if (i > 0)
                    sqlBuffer.Append(", ");

                sqlBuffer.AppendAndBindParameter(SqlDialectStrategy, Parameters, $"{name}{i}", value);
                i++;
            }

            sqlBuffer.Append(')');

            return sqlBuffer.ToString();
        }

        [GeneratedRegex($"^{_parameterPrefix}(_\\d+)+_?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex ParameterRegex();
    }
}
