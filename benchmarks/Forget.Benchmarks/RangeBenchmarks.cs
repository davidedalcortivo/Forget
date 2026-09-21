using BenchmarkDotNet.Attributes;
using Dapper;
using Forget.PostgreSql.Extensions;
using Microsoft.EntityFrameworkCore;


namespace Forget.Benchmarks
{
    public class RangeBenchmarks : PostgreSqlBenchmark
    {
        private const string InsertSql = "INSERT INTO dbo.\"Product\" (\"Id\", \"Name\", \"Category\", \"IsActive\", \"Price\", \"Note\") VALUES (@Id, @Name, @Category, @IsActive, @Price, @Note)";

        private const string UpdateSql = "UPDATE dbo.\"Product\" SET \"Name\" = @Name, \"Category\" = @Category, \"IsActive\" = @IsActive, \"Price\" = @Price, \"Note\" = @Note WHERE \"Id\" = @Id";

        private int[] _ids = null!;

        private Product[] _rows = null!;

        [Params(100, 1000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            Start();

            _ids = Enumerable.Range(0, Count).Select(i => 1 + i * 7 % RowCount).ToArray();
            _rows = Connection.Query<Product>(Select + " WHERE \"Id\" = ANY(@ids)", new { ids = _ids }).ToArray();

            IReadOnlyList<Product> found = GetByIdRange_Dapper();

            if (found.Count != Count)
            {
                throw new InvalidOperationException("The ids do not match the expected number of rows");
            }

            Same(nameof(GetByIdRange_Forget), found, GetByIdRange_Forget());
            Same(nameof(GetByIdRange_EfCore), found, GetByIdRange_EfCore());

            foreach (Func<int> insert in new Func<int>[] { InsertRange_Dapper, InsertRange_Forget, InsertRange_EfCore })
            {
                if (insert() != Count)
                {
                    throw new InvalidOperationException($"{insert.Method.Name} did not write {Count} rows");
                }

                Same(insert.Method.Name, _rows, StoredRows());
            }

            _rows = Connection.Query<Product>(Select + " WHERE \"Id\" = ANY(@ids)", new { ids = _ids }).ToArray();

            foreach (Func<int> update in new Func<int>[] { UpdateRange_Dapper, UpdateRange_Forget, UpdateRange_EfCore })
            {
                foreach (Product row in _rows)
                {
                    row.Name = update.Method.Name;
                    row.Price += 1;
                }

                if (update() != Count)
                {
                    throw new InvalidOperationException($"{update.Method.Name} did not update {Count} rows");
                }

                Same(update.Method.Name, _rows, StoredRows());
            }
        }

        [Benchmark(Baseline = true), BenchmarkCategory("GetByIdRange")]
        public IReadOnlyList<Product> GetByIdRange_Dapper() => Connection
            .Query<Product>(Select + " WHERE \"Id\" = ANY(@ids)", new { ids = _ids })
            .AsList();

        [Benchmark, BenchmarkCategory("GetByIdRange")]
        public IReadOnlyList<Product> GetByIdRange_Forget() => Connection.GetByIdRange<Product>(_ids);

        [Benchmark, BenchmarkCategory("GetByIdRange")]
        public IReadOnlyList<Product> GetByIdRange_EfCore()
        {
            using BenchmarkContext context = new(Options);
            return context.Products.AsNoTracking().Where(p => _ids.Contains(p.Id)).ToList();
        }

        [Benchmark(Baseline = true), BenchmarkCategory("InsertRange")]
        public int InsertRange_Dapper()
        {
            Product[] rows = Renumber();
            using var transaction = Connection.BeginTransaction();
            int written = Connection.Execute(InsertSql, rows, transaction);
            transaction.Commit();
            return written;
        }

        [Benchmark, BenchmarkCategory("InsertRange")]
        public int InsertRange_Forget()
        {
            Product[] rows = Renumber();
            using var transaction = Connection.BeginTransaction();
            int written = Connection.InsertRange(rows, transaction: transaction);
            transaction.Commit();
            return written;
        }

        [Benchmark, BenchmarkCategory("InsertRange")]
        public int InsertRange_EfCore()
        {
            Product[] rows = Renumber();
            using BenchmarkContext context = new(Options);
            context.Products.AddRange(rows);
            return context.SaveChanges();
        }

        [Benchmark(Baseline = true), BenchmarkCategory("UpdateRange")]
        public int UpdateRange_Dapper()
        {
            using var transaction = Connection.BeginTransaction();
            int written = Connection.Execute(UpdateSql, _rows, transaction);
            transaction.Commit();
            return written;
        }

        [Benchmark, BenchmarkCategory("UpdateRange")]
        public int UpdateRange_Forget()
        {
            using var transaction = Connection.BeginTransaction();
            int written = Connection.UpdateRange(_rows, transaction: transaction);
            transaction.Commit();
            return written;
        }

        [Benchmark, BenchmarkCategory("UpdateRange")]
        public int UpdateRange_EfCore()
        {
            using BenchmarkContext context = new(Options);
            context.Products.UpdateRange(_rows);
            return context.SaveChanges();
        }

        private Product[] Renumber()
        {
            int first = ReserveIds(Count);

            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].Id = first + i;
            }

            return _rows;
        }

        private IEnumerable<Product> StoredRows() => Connection.Query<Product>(
            Select + " WHERE \"Id\" = ANY(@ids)",
            new { ids = _rows.Select(r => r.Id).ToArray() });
    }
}
