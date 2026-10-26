using Forget.Core.Caching;
using Forget.Core.Models;
using System.Collections.Concurrent;


namespace Forget.Tests.Core
{
    /// <summary>
    /// <see cref="EntityInfoCache{TEntity}"/>, <see cref="DbColumnInfoCache{TEntity}"/> and
    /// <see cref="SqlBuilderCache{TEntity, TStrategy}"/> are process-wide statics that every caller touches
    /// concurrently in a real application. Each test here hammers a cache's *first* access (or first write, for
    /// <see cref="DbColumnInfoCache{TEntity}"/>'s <c>Add</c>) from many threads at once (using a
    /// <see cref="Barrier"/> to line them up so they actually race rather than run one after another) and
    /// requires every participant to succeed with a consistent result. A dedicated entity type per test keeps
    /// "first access" genuinely first: these caches are keyed per closed generic type, so reusing a type already
    /// touched elsewhere in the suite would just exercise the (uninteresting) already-initialized fast path.
    /// </summary>
    public class CacheConcurrencyTests
    {
        private const int Concurrency = 64;

        private sealed class ConcurrencyWidgetA
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public async Task EntityInfoCache_ConcurrentFirstAccess_EveryCallerObservesConsistentState()
        {
            Barrier barrier = new(Concurrency);
            ConcurrentBag<string> tableNames = [];
            ConcurrentBag<Exception> exceptions = [];

            Task[] tasks = [.. Enumerable.Range(0, Concurrency).Select(_ => Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    tableNames.Add(EntityInfoCache<ConcurrencyWidgetA>.TableName);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))];

            await Task.WhenAll(tasks);

            Assert.Empty(exceptions);
            Assert.Equal(Concurrency, tableNames.Count);
            Assert.All(tableNames, name => Assert.Equal(nameof(ConcurrencyWidgetA), name));
        }

        private sealed class ConcurrencyWidgetB
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public async Task SqlBuilderCache_ConcurrentInitialize_PopulatesEveryTemplateConsistently()
        {
            Barrier barrier = new(Concurrency);
            ConcurrentBag<Exception> exceptions = [];

            Task[] tasks = [.. Enumerable.Range(0, Concurrency).Select(_ => Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    SqlBuilderCache<ConcurrencyWidgetB, Forget.SqlServer.Strategies.SqlBuilderStrategy>.Initialize(Forget.SqlServer.Strategies.SqlBuilderStrategy.Instance);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))];

            await Task.WhenAll(tasks);

            Assert.Empty(exceptions);
            Assert.NotNull(SqlBuilderCache<ConcurrencyWidgetB, Forget.SqlServer.Strategies.SqlBuilderStrategy>.GetAllSql);
            Assert.NotNull(SqlBuilderCache<ConcurrencyWidgetB, Forget.SqlServer.Strategies.SqlBuilderStrategy>.InsertSql);
            Assert.NotNull(SqlBuilderCache<ConcurrencyWidgetB, Forget.SqlServer.Strategies.SqlBuilderStrategy>.UpsertSql);
            Assert.NotNull(SqlBuilderCache<ConcurrencyWidgetB, Forget.SqlServer.Strategies.SqlBuilderStrategy>.DeleteSql);
        }

        private sealed class ConcurrencyWidgetC
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public async Task DbColumnInfoCache_ConcurrentAdd_ExactlyOneWriteWinsAndGetIsConsistent()
        {
            const string connectionId = "concurrency-add-test";

            Dictionary<string, DbColumnInfo> columns = new()
            {
                ["Id"] = new DbColumnInfo { Name = "Id" },
                ["Name"] = new DbColumnInfo { Name = "Name" },
            };

            Barrier barrier = new(Concurrency);
            ConcurrentBag<Exception> exceptions = [];

            Task[] tasks = [.. Enumerable.Range(0, Concurrency).Select(_ => Task.Run(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    DbColumnInfoCache<ConcurrencyWidgetC>.Add(connectionId, columns);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))];

            await Task.WhenAll(tasks);

            Assert.Empty(exceptions);
            IDictionary<string, DbColumnInfo> stored = DbColumnInfoCache<ConcurrencyWidgetC>.GetDictValue(connectionId);
            Assert.Equal(2, stored.Count);
            Assert.Equal("Id", stored["Id"].Name);
            Assert.Equal("Name", stored["Name"].Name);
        }

        private sealed class ConcurrencyWidgetD
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [Fact]
        public async Task DbColumnInfoCache_ConcurrentGetSemaphore_ReturnsTheSameInstanceToEveryCaller()
        {
            const string connectionId = "concurrency-semaphore-test";

            Barrier barrier = new(Concurrency);
            ConcurrentBag<SemaphoreSlim> semaphores = [];

            Task[] tasks = [.. Enumerable.Range(0, Concurrency).Select(_ => Task.Run(() =>
            {
                barrier.SignalAndWait();
                semaphores.Add(DbColumnInfoCache<ConcurrencyWidgetD>.GetSemaphore(connectionId));
            }))];

            await Task.WhenAll(tasks);

            Assert.Equal(Concurrency, semaphores.Count);
            Assert.Single(semaphores.Distinct());
        }
    }
}
