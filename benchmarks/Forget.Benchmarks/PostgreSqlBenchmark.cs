using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;


namespace Forget.Benchmarks
{
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public abstract class PostgreSqlBenchmark
    {
        protected const int RowCount = 10_000;

        protected const string Select = "SELECT \"Id\", \"Name\", \"Category\", \"IsActive\", \"Price\", \"Note\" FROM dbo.\"Product\"";

        private static int _nextId = 1_000_000;

        private PostgreSqlContainer _container = null!;

        protected NpgsqlConnection Connection { get; private set; } = null!;

        protected DbContextOptions<BenchmarkContext> Options { get; private set; } = null!;

        protected void Start()
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
            _container.StartAsync().GetAwaiter().GetResult();

            NpgsqlConnectionStringBuilder connectionString = new(_container.GetConnectionString());

            if (Environment.GetEnvironmentVariable("FORGET_BENCHMARK_HOST") is { Length: > 0 } host)
            {
                connectionString.Host = host;
            }

            Connection = new NpgsqlConnection(connectionString.ConnectionString);
            Connection.Open();

            Connection.Execute($"""
                CREATE SCHEMA dbo;
                CREATE TABLE dbo."Product" (
                    "Id" INTEGER PRIMARY KEY,
                    "Name" TEXT NOT NULL,
                    "Category" INTEGER NOT NULL,
                    "IsActive" BOOLEAN NOT NULL,
                    "Price" NUMERIC(18,2) NOT NULL,
                    "Note" TEXT NULL
                );
                CREATE INDEX ON dbo."Product" ("Category");
                INSERT INTO dbo."Product"
                SELECT i, 'Product ' || i, i % 100, (i / 100) % 2 = 0, (i % 1000) + 0.99, CASE WHEN i % 3 = 0 THEN 'Note ' || i END
                FROM generate_series(1, {RowCount}) AS i;
                ANALYZE dbo."Product";
                """);

            Options = new DbContextOptionsBuilder<BenchmarkContext>().UseNpgsql(Connection).Options;
        }

        [GlobalCleanup]
        public void Stop()
        {
            Connection.Dispose();
            _container.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        protected static int ReserveIds(int count) => Interlocked.Add(ref _nextId, count) - count;

        protected static void Same(string what, IEnumerable<Product> expected, IEnumerable<Product> actual)
        {
            static IEnumerable<string> Keys(IEnumerable<Product> products) => products
                .OrderBy(p => p.Id)
                .Select(p => $"{p.Id}|{p.Name}|{p.Category}|{p.IsActive}|{p.Price:F2}|{p.Note}");

            if (!Keys(expected).SequenceEqual(Keys(actual)))
            {
                throw new InvalidOperationException($"{what} does not return the same rows as the Dapper baseline");
            }
        }
    }
}
