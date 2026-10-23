using Forget.Core.Abstractions.Strategies;
using System.Text.Json;


namespace Forget.Oracle.Strategies
{
    internal sealed partial class DbExecutionStrategy : BaseDbExecutionStrategy<DbCommandStrategy>
    {
        public static DbExecutionStrategy Instance { get; } = new(DbCommandStrategy.Instance);

        private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

        private DbExecutionStrategy(DbCommandStrategy strategy) : base(strategy) { }
    }
}
