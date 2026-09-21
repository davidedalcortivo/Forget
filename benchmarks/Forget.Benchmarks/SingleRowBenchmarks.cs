using BenchmarkDotNet.Attributes;
using Dapper;
using Forget.PostgreSql.Extensions;
using Microsoft.EntityFrameworkCore;


namespace Forget.Benchmarks
{
    public class SingleRowBenchmarks : PostgreSqlBenchmark
    {
        private const string InsertSql = "INSERT INTO dbo.\"Product\" (\"Id\", \"Name\", \"Category\", \"IsActive\", \"Price\", \"Note\") VALUES (@Id, @Name, @Category, @IsActive, @Price, @Note)";

        private const string UpdateSql = "UPDATE dbo.\"Product\" SET \"Name\" = @Name, \"Category\" = @Category, \"IsActive\" = @IsActive, \"Price\" = @Price, \"Note\" = @Note WHERE \"Id\" = @Id";

        private readonly int _id = 5000;

        private Product _row = null!;

        [GlobalSetup]
        public void Setup()
        {
            Start();

            _row = GetById_Dapper()!;

            Same(nameof(GetById_Forget), [_row], [GetById_Forget()!]);
            Same(nameof(GetById_EfCore), [_row], [GetById_EfCore()!]);

            foreach (Func<Product> insert in new Func<Product>[] { Insert_Dapper, Insert_Forget, Insert_EfCore })
            {
                Product inserted = insert();
                Same(insert.Method.Name, [inserted], Connection.Query<Product>(Select + " WHERE \"Id\" = @id", new { id = inserted.Id }));
            }

            foreach (Func<int> update in new Func<int>[] { Update_Dapper, Update_Forget, Update_EfCore })
            {
                _row.Name = update.Method.Name;
                if (update() != 1)
                {
                    throw new InvalidOperationException($"{update.Method.Name} did not update exactly one row");
                }

                Same(update.Method.Name, [_row], Connection.Query<Product>(Select + " WHERE \"Id\" = @id", new { id = _id }));
            }
        }

        [Benchmark(Baseline = true), BenchmarkCategory("GetById")]
        public Product? GetById_Dapper() => Connection.QuerySingleOrDefault<Product>(Select + " WHERE \"Id\" = @id", new { id = _id });

        [Benchmark, BenchmarkCategory("GetById")]
        public Product? GetById_Forget() => Connection.GetById<Product>(_id);

        [Benchmark, BenchmarkCategory("GetById")]
        public Product? GetById_EfCore()
        {
            using BenchmarkContext context = new(Options);
            return context.Products.AsNoTracking().FirstOrDefault(p => p.Id == _id);
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Insert")]
        public Product Insert_Dapper()
        {
            Product product = NewProduct();
            Connection.Execute(InsertSql, product);
            return product;
        }

        [Benchmark, BenchmarkCategory("Insert")]
        public Product Insert_Forget()
        {
            Product product = NewProduct();
            Connection.Insert(product);
            return product;
        }

        [Benchmark, BenchmarkCategory("Insert")]
        public Product Insert_EfCore()
        {
            Product product = NewProduct();
            using BenchmarkContext context = new(Options);
            context.Products.Add(product);
            context.SaveChanges();
            return product;
        }

        [Benchmark(Baseline = true), BenchmarkCategory("Update")]
        public int Update_Dapper() => Connection.Execute(UpdateSql, _row);

        [Benchmark, BenchmarkCategory("Update")]
        public int Update_Forget() => Connection.Update(_row);

        [Benchmark, BenchmarkCategory("Update")]
        public int Update_EfCore()
        {
            using BenchmarkContext context = new(Options);
            context.Products.Update(_row);
            return context.SaveChanges();
        }

        private static Product NewProduct() => new()
        {
            Id = ReserveIds(1),
            Name = "Inserted",
            Category = 7,
            IsActive = true,
            Price = 12.50m,
            Note = "Note"
        };
    }
}
