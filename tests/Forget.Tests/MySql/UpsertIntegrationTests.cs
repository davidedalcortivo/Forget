using Forget.MySql.Extensions;
using MySqlConnector;
using System.Collections.Concurrent;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// Upsert on a natural key against a table whose primary key the database generates, and the same upsert
    /// issued by many connections at once. The concurrent tests are the ones that matter: a statement that is
    /// correct for one caller can still lose a race between two (a duplicate-key error, or two rows), which no
    /// amount of single-connection testing shows.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class UpsertIntegrationTests
    {
        private const int Callers = 16;
        private const int Rounds = 10;

        private readonly MySqlFixture _fixture;

        public UpsertIntegrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task UpsertAsync_OnANaturalKey_InsertsThenUpdatesTheSameRow()
        {
            await _fixture.Connection.UpsertAsync(new UpsertProduct { Sku = "SINGLE-1", Name = "first", Stock = 1 }, cancellationToken: Ct);
            UpsertProduct inserted = await _fixture.Connection.GetFirstAsync<UpsertProduct>(p => p.Sku == "SINGLE-1", cancellationToken: Ct);

            await _fixture.Connection.UpsertAsync(new UpsertProduct { Sku = "SINGLE-1", Name = "second", Stock = 5 }, cancellationToken: Ct);
            IReadOnlyList<UpsertProduct> rows = await _fixture.Connection.GetAllAsync<UpsertProduct>(p => p.Sku == "SINGLE-1", cancellationToken: Ct);

            UpsertProduct row = Assert.Single(rows);
            Assert.True(inserted.Id > 0);
            Assert.Equal(inserted.Id, row.Id);
            Assert.Equal("second", row.Name);
            Assert.Equal(5, row.Stock);
        }

        [Fact]
        public async Task UpsertRangeAsync_OnANaturalKey_InsertsNewRowsAndUpdatesExistingOnes()
        {
            await _fixture.Connection.UpsertAsync(new UpsertProduct { Sku = "RANGE-1", Name = "old", Stock = 1 }, cancellationToken: Ct);
            UpsertProduct existing = await _fixture.Connection.GetFirstAsync<UpsertProduct>(p => p.Sku == "RANGE-1", cancellationToken: Ct);

            await _fixture.Connection.UpsertRangeAsync(
            [
                new UpsertProduct { Sku = "RANGE-1", Name = "updated", Stock = 10 },
                new UpsertProduct { Sku = "RANGE-2", Name = "new", Stock = 20 },
                new UpsertProduct { Sku = "RANGE-3", Name = "new", Stock = 30 }
            ], cancellationToken: Ct);

            IReadOnlyList<UpsertProduct> rows = await _fixture.Connection.GetAllAsync<UpsertProduct>(p => p.Sku.StartsWith("RANGE-"), sortDescriptors: [new(p => p.Sku)], cancellationToken: Ct);

            Assert.Equal(["RANGE-1", "RANGE-2", "RANGE-3"], rows.Select(p => p.Sku));
            Assert.Equal([10, 20, 30], rows.Select(p => p.Stock));
            Assert.Equal(existing.Id, rows[0].Id);
            Assert.Equal("updated", rows[0].Name);
        }

        [Fact]
        public async Task UpsertAsync_FromManyConnectionsAtOnceOnTheSameNewKey_LeavesExactlyOneRowAndFailsNowhere()
        {
            ConcurrentBag<Exception> failures = [];

            for (int round = 0; round < Rounds; round++)
            {
                string sku = $"CONCURRENT-{round}";

                await RunAtOnceAsync(failures, async (connection, caller) =>
                    await connection.UpsertAsync(new UpsertProduct { Sku = sku, Name = $"caller {caller}", Stock = caller }, cancellationToken: Ct));

                Assert.Equal(1, await _fixture.Connection.CountAsync<UpsertProduct>(p => p.Sku == sku, cancellationToken: Ct));
            }

            AssertNoFailures(failures);
        }

        [Fact]
        public async Task UpsertRangeAsync_FromManyConnectionsAtOnceOnTheSameNewKeys_LeavesExactlyOneRowPerKeyAndFailsNowhere()
        {
            ConcurrentBag<Exception> failures = [];

            for (int round = 0; round < Rounds; round++)
            {
                string prefix = $"CONCURRENT-RANGE-{round}-";

                await RunAtOnceAsync(failures, async (connection, caller) =>
                    await connection.UpsertRangeAsync(
                    [
                        new UpsertProduct { Sku = prefix + "A", Name = $"caller {caller}", Stock = caller },
                        new UpsertProduct { Sku = prefix + "B", Name = $"caller {caller}", Stock = caller },
                        new UpsertProduct { Sku = prefix + "C", Name = $"caller {caller}", Stock = caller }
                    ], cancellationToken: Ct));

                Assert.Equal(3, await _fixture.Connection.CountAsync<UpsertProduct>(p => p.Sku.StartsWith(prefix), cancellationToken: Ct));
            }

            AssertNoFailures(failures);
        }

        /// <summary>
        /// Opens <see cref="Callers"/> connections, holds every caller at a gate until all of them are ready, then
        /// releases them together so the statements really do overlap.
        /// </summary>
        private async Task RunAtOnceAsync(ConcurrentBag<Exception> failures, Func<MySqlConnection, int, Task> action)
        {
            TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CountdownEvent ready = new(Callers);

            Task[] callers = [.. Enumerable.Range(0, Callers).Select(caller => Task.Run(async () =>
            {
                bool signaled = false;

                try
                {
                    await using MySqlConnection connection = new(_fixture.ConnectionString);
                    await connection.OpenAsync(Ct);

                    signaled = true;
                    ready.Signal();
                    await gate.Task;

                    await action(connection, caller);
                }
                catch (Exception ex)
                {
                    failures.Add(ex);

                    if (!signaled)
                        ready.Signal();
                }
            }, Ct))];

            await Task.Run(() => ready.Wait(Ct), Ct);
            gate.SetResult();
            await Task.WhenAll(callers);
        }

        private static void AssertNoFailures(ConcurrentBag<Exception> failures)
        {
            Assert.True(failures.IsEmpty, Environment.NewLine + string.Join(Environment.NewLine, failures.Select(f => $"{f.GetType().Name}: {f.Message}").Distinct()));
        }
    }
}
