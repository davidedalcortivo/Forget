using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Forget.PostgreSql.Extensions;
using Npgsql;


namespace Forget.Benchmarks
{
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class RangeGenerationBenchmarks
    {
        private readonly NpgsqlConnection _connection = new();

        private int[] _ids = null!;

        [Params(100, 1000, 10000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void Setup() => _ids = [.. Enumerable.Range(1, Count)];

        [Benchmark(Baseline = true), BenchmarkCategory("GetByIdRange")]
        public object GetByIdRangeCommands() => _connection.GetByIdRangeCommands<Product>(_ids, batchSize: 0);

        [Benchmark, BenchmarkCategory("GetByIdRange")]
        public object GetByIdRangeCommands_Typed() => _connection.GetByIdRangeCommands<Product, int>(_ids, batchSize: 0);

        [Benchmark(Baseline = true), BenchmarkCategory("DeleteRange")]
        public object DeleteRangeCommands() => _connection.DeleteRangeCommands<Product>(_ids, batchSize: 0);

        [Benchmark, BenchmarkCategory("DeleteRange")]
        public object DeleteRangeCommands_Typed() => _connection.DeleteRangeCommands<Product, int>(_ids, batchSize: 0);
    }
}
