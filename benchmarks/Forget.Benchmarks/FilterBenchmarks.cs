using BenchmarkDotNet.Attributes;
using Dapper;
using Forget.Core.Models;
using Forget.PostgreSql.Extensions;
using Microsoft.EntityFrameworkCore;


namespace Forget.Benchmarks
{
    public class FilterBenchmarks : PostgreSqlBenchmark
    {
        private readonly int _category = 7;

        private readonly int[] _ids = [3, 17, 250, 999, 1500, 2048, 4096, 5000, 7777, 9999];

        private readonly string _prefix = "Product 12";

        [GlobalSetup]
        public void Setup()
        {
            Start();

            IReadOnlyList<Product> all = GetAll_Dapper();

            if (all.Count == 0)
            {
                throw new InvalidOperationException("The filter matches no rows");
            }

            Same(nameof(GetAll_Forget), all, GetAll_Forget());
            Same(nameof(GetAll_EfCore), all, GetAll_EfCore());

            IReadOnlyList<Product> page = GetPage_Dapper();

            if (page.Count != 20)
            {
                throw new InvalidOperationException("The page does not have 20 rows");
            }

            Same(nameof(GetPage_Forget), page, GetPage_Forget());
            Same(nameof(GetPage_EfCore), page, GetPage_EfCore());

            IReadOnlyList<Product> some = GetAll_Contains_Dapper();

            if (some.Count != _ids.Length)
            {
                throw new InvalidOperationException("The ids do not match the expected number of rows");
            }

            Same(nameof(GetAll_Contains_Forget), some, GetAll_Contains_Forget());
            Same(nameof(GetAll_Contains_EfCore), some, GetAll_Contains_EfCore());

            IReadOnlyList<Product> prefixed = GetAll_StartsWith_Dapper();

            if (prefixed.Count == 0)
            {
                throw new InvalidOperationException("The prefix matches no rows");
            }

            Same(nameof(GetAll_StartsWith_Forget), prefixed, GetAll_StartsWith_Forget());
            Same(nameof(GetAll_StartsWith_EfCore), prefixed, GetAll_StartsWith_EfCore());
        }

        [Benchmark(Baseline = true), BenchmarkCategory("GetAll")]
        public IReadOnlyList<Product> GetAll_Dapper() => Connection
            .Query<Product>(Select + " WHERE \"Category\" = @category AND \"IsActive\" = TRUE", new { category = _category })
            .AsList();

        [Benchmark, BenchmarkCategory("GetAll")]
        public IReadOnlyList<Product> GetAll_Forget() => Connection.GetAll<Product>(p => p.Category == _category && p.IsActive);

        [Benchmark, BenchmarkCategory("GetAll")]
        public IReadOnlyList<Product> GetAll_EfCore()
        {
            using BenchmarkContext context = new(Options);
            return context.Products.AsNoTracking().Where(p => p.Category == _category && p.IsActive).ToList();
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Contains")]
        public IReadOnlyList<Product> GetAll_Contains_Dapper() => Connection
            .Query<Product>(Select + " WHERE \"Id\" = ANY(@ids)", new { ids = _ids })
            .AsList();

        [Benchmark, BenchmarkCategory("Contains")]
        public IReadOnlyList<Product> GetAll_Contains_Forget() => Connection.GetAll<Product>(p => _ids.Contains(p.Id));

        [Benchmark, BenchmarkCategory("Contains")]
        public IReadOnlyList<Product> GetAll_Contains_EfCore()
        {
            using BenchmarkContext context = new(Options);
            return context.Products.AsNoTracking().Where(p => _ids.Contains(p.Id)).ToList();
        }

        [Benchmark(Baseline = true), BenchmarkCategory("StartsWith")]
        public IReadOnlyList<Product> GetAll_StartsWith_Dapper() => Connection
            .Query<Product>(Select + " WHERE \"Name\" LIKE @pattern", new { pattern = _prefix + "%" })
            .AsList();

        [Benchmark, BenchmarkCategory("StartsWith")]
        public IReadOnlyList<Product> GetAll_StartsWith_Forget() => Connection.GetAll<Product>(p => p.Name.StartsWith(_prefix));

        [Benchmark, BenchmarkCategory("StartsWith")]
        public IReadOnlyList<Product> GetAll_StartsWith_EfCore()
        {
            using BenchmarkContext context = new(Options);
            return context.Products.AsNoTracking().Where(p => p.Name.StartsWith(_prefix)).ToList();
        }

        [Benchmark(Baseline = true), BenchmarkCategory("GetPage")]
        public IReadOnlyList<Product> GetPage_Dapper() => Connection
            .Query<Product>(Select + " WHERE \"Category\" = @category ORDER BY \"Price\" DESC, \"Id\" OFFSET 10 LIMIT 20", new { category = _category })
            .AsList();

        [Benchmark, BenchmarkCategory("GetPage")]
        public IReadOnlyList<Product> GetPage_Forget() => Connection.GetPage<Product>(
            p => p.Category == _category,
            [new SortDescriptor<Product>(p => p.Price, SortDirection.Descending), new SortDescriptor<Product>(p => p.Id)],
            skip: 10,
            take: 20);

        [Benchmark, BenchmarkCategory("GetPage")]
        public IReadOnlyList<Product> GetPage_EfCore()
        {
            using BenchmarkContext context = new(Options);
            return context.Products.AsNoTracking()
                .Where(p => p.Category == _category)
                .OrderByDescending(p => p.Price).ThenBy(p => p.Id)
                .Skip(10).Take(20)
                .ToList();
        }
    }
}
