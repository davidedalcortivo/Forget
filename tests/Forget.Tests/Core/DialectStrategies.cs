using Dapper;
using Forget.Core.Abstractions.Strategies;


namespace Forget.Tests.Core
{
    internal static class DialectStrategies
    {
        public static TheoryData<string, object> All()
        {
            return new()
            {
                { "MySql", Forget.MySql.Strategies.SqlDialectStrategy.Instance },
                { "Oracle", Forget.Oracle.Strategies.SqlDialectStrategy.Instance },
                { "PostgreSql", Forget.PostgreSql.Strategies.SqlDialectStrategy.Instance },
                { "SqlServer", Forget.SqlServer.Strategies.SqlDialectStrategy.Instance }
            };
        }

        // A list is bound as one parameter (an array on PostgreSQL, a list Dapper expands elsewhere), except a list of mixed
        // types on PostgreSQL: Npgsql cannot bind it as an array and Dapper does not expand it there, so Forget writes it
        // out as one parameter per element, named after the list and numbered from 0: p_0_ becomes (@p_0_0, @p_0_1, ...).
        private static bool IsWrittenOut(string dialectName, bool mixed) => dialectName == "PostgreSql" && mixed;

        public static string In(ISqlDialectStrategy dialect, string dialectName, string column, string name, int count, bool mixed)
        {
            if (!IsWrittenOut(dialectName, mixed))
                return dialect.In(column, dialect.RenderParameter(name), mixed);

            return dialect.In(column, $"({string.Join(", ", Enumerable.Range(0, count).Select(i => dialect.RenderParameter($"{name}{i}")))})", mixed);
        }

        public static object?[] ListValues(string dialectName, DynamicParameters parameters, string name, int count, bool mixed)
        {
            if (!IsWrittenOut(dialectName, mixed))
            {
                Assert.Equal([name], parameters.ParameterNames.Where(x => x.StartsWith(name)));
                return [.. parameters.Get<Array>(name).Cast<object?>()];
            }

            Assert.Equal(Enumerable.Range(0, count).Select(i => $"{name}{i}"), parameters.ParameterNames.Where(x => x.StartsWith(name)));
            return [.. Enumerable.Range(0, count).Select(i => parameters.Get<object?>($"{name}{i}"))];
        }
    }
}
