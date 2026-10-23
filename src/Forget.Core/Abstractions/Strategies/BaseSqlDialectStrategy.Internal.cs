namespace Forget.Core.Abstractions.Strategies
{
    internal abstract partial class BaseSqlDialectStrategy : ISqlDialectStrategy
    {
        protected virtual string BuildIdentifier(string name)
        {
            return $"\"{name.Replace("\"", "\"\"")}\"";
        }

        protected abstract string BuildConnectionId(string connectionString);
    }
}
