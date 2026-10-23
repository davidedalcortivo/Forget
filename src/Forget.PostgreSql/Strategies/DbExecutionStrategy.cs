using Forget.Core.Abstractions.Strategies;


namespace Forget.PostgreSql.Strategies
{
    internal sealed partial class DbExecutionStrategy : BaseDbExecutionStrategy<DbCommandStrategy>
    {
        public static DbExecutionStrategy Instance { get; } = new(DbCommandStrategy.Instance);

        private DbExecutionStrategy(DbCommandStrategy strategy) : base(strategy) { }
    }
}
