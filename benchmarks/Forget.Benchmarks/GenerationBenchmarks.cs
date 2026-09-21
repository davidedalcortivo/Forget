using BenchmarkDotNet.Attributes;
using Forget.Core.Models;
using Forget.PostgreSql.Extensions;
using Npgsql;


namespace Forget.Benchmarks
{
    [MemoryDiagnoser]
    public class GenerationBenchmarks
    {
        private readonly NpgsqlConnection _connection = new();

        private readonly int _category = 7;

        [Benchmark(Baseline = true)]
        public object GetById() => _connection.GetByIdCommand<Product>(5000);

        [Benchmark]
        public object GetAll() => _connection.GetAllCommand<Product>();

        [Benchmark]
        public object GetAll_Filter_Literal() => _connection.GetAllCommand<Product>(p => p.Category == 7 && p.IsActive);

        [Benchmark]
        public object GetAll_Filter() => _connection.GetAllCommand<Product>(p => p.Category == _category && p.IsActive);

        [Benchmark]
        public object GetPage_Filter_Sort() => _connection.GetPageCommand<Product>(
            p => p.Category == _category,
            [new SortDescriptor<Product>(p => p.Price, SortDirection.Descending), new SortDescriptor<Product>(p => p.Id)],
            skip: 10,
            take: 20);
    }
}
