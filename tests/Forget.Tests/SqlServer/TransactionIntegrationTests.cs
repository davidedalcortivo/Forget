using Forget.SqlServer.Extensions;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;
using System.Diagnostics;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// What happens around a write: an explicit transaction passed by the caller (committed or rolled back as a
    /// whole), the transaction Forget opens itself for a multi-row write (all or nothing), a cancelled token, a
    /// transaction that belongs to another connection, and the command timeout. Every write and read family takes the
    /// same three parameters, so the first test pushes every one of them through one transaction.
    /// <para>
    /// Tests stay isolated on the shared container by using a disjoint <c>Id</c> range each.
    /// </para>
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public class TransactionIntegrationTests
    {
        private readonly SqlServerFixture _fixture;

        public TransactionIntegrationTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static Widget NewWidget(int id, string name = "tx") => new() { Id = id, Name = name, IsActive = true, Price = 1m };

        private static readonly int[] AllIds = [400, 401, 402, 403, 404, 405, 406, 407, 408, 409];

        /// <summary>
        /// Every write through one transaction, and the reads that have to see them from inside it. Leaves, if the
        /// transaction is committed, exactly ids 400, 401, 402, 405 and 409.
        /// </summary>
        private async Task RunEveryWriteAsync(SqlTransaction tx)
        {
            SqlConnection connection = _fixture.Connection;
            Widget first = NewWidget(400);

            await connection.InsertAsync(first, transaction: tx, cancellationToken: Ct);
            await connection.InsertRangeAsync([NewWidget(401), NewWidget(402)], transaction: tx, cancellationToken: Ct);
            await connection.UpsertAsync(NewWidget(403), transaction: tx, cancellationToken: Ct);
            await connection.UpsertRangeAsync([NewWidget(404), NewWidget(405)], transaction: tx, cancellationToken: Ct);
            Widget[] scratch = [NewWidget(406), NewWidget(407), NewWidget(408), NewWidget(409)];
            await connection.InsertRangeAsync(scratch, transaction: tx, cancellationToken: Ct);

            // Reads that must see the uncommitted rows, so they have to run on the same transaction.
            Assert.Equal(10, (await connection.GetByIdRangeAsync<Widget>(AllIds, transaction: tx, cancellationToken: Ct)).Count);
            Assert.Equal(10, (await connection.GetAllAsync<Widget>(w => w.Id >= 400 && w.Id <= 409, transaction: tx, cancellationToken: Ct)).Count);
            Assert.Equal(10, await connection.CountAsync<Widget>(w => w.Id >= 400 && w.Id <= 409, transaction: tx, cancellationToken: Ct));
            Assert.True(await connection.ExistsAsync<Widget>(w => w.Id == 400, transaction: tx, cancellationToken: Ct));
            Assert.Equal("tx", (await connection.GetByIdAsync<Widget>(400, transaction: tx, cancellationToken: Ct))!.Name);

            first.Name = "changed";
            await connection.UpdateAsync(first, transaction: tx, cancellationToken: Ct);
            await connection.UpdateRangeAsync([NewWidget(401, "changed")], transaction: tx, cancellationToken: Ct);
            await connection.UpdateAsync<Widget>(new { IsActive = false }, w => w.Id == 402, transaction: tx, cancellationToken: Ct);

            await connection.DeleteAsync(NewWidget(403), transaction: tx, cancellationToken: Ct);
            await connection.DeleteAsync<Widget>(404, transaction: tx, cancellationToken: Ct);
            await connection.DeleteRangeAsync<Widget>(new[] { 406 }, transaction: tx, cancellationToken: Ct);
            await connection.DeleteRangeAsync([scratch[1]], transaction: tx, cancellationToken: Ct);
            await connection.DeleteAsync<Widget>(w => w.Id == 408, transaction: tx, cancellationToken: Ct);
        }

        [Fact]
        public async Task EveryWrite_WithAnExplicitTransaction_IsRolledBackTogether()
        {
            using SqlTransaction tx = _fixture.Connection.BeginTransaction();
            await RunEveryWriteAsync(tx);
            tx.Rollback();

            Assert.Empty(await _fixture.Connection.GetByIdRangeAsync<Widget>(AllIds, cancellationToken: Ct));
        }

        [Fact]
        public async Task EveryWrite_WithAnExplicitTransaction_IsKeptWhenItIsCommitted()
        {
            // Same ids as the rollback test, so the two must not overlap in time: they do not, a collection runs its tests in turn.
            using (SqlTransaction tx = _fixture.Connection.BeginTransaction())
            {
                await RunEveryWriteAsync(tx);
                tx.Commit();
            }

            IReadOnlyList<Widget> kept = await _fixture.Connection.GetByIdRangeAsync<Widget>(AllIds, cancellationToken: Ct);

            Assert.Equal([400, 401, 402, 405, 409], kept.Select(w => w.Id).Order());
            Assert.Equal("changed", kept.Single(w => w.Id == 400).Name);
            Assert.Equal("changed", kept.Single(w => w.Id == 401).Name);
            Assert.False(kept.Single(w => w.Id == 402).IsActive);

            await _fixture.Connection.DeleteRangeAsync<Widget>(new[] { 400, 401, 402, 405, 409 }, cancellationToken: Ct);
        }

        [Fact]
        public async Task ATransactionOfAnotherConnection_IsRejected()
        {
            await using SqlConnection other = new(_fixture.ConnectionString);
            await other.OpenAsync(Ct);
            await using SqlTransaction foreign = other.BeginTransaction();

            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.InsertAsync(NewWidget(410), transaction: foreign, cancellationToken: Ct));
            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.InsertRangeAsync([NewWidget(410)], transaction: foreign, cancellationToken: Ct));
            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdAsync<Widget>(410, transaction: foreign, cancellationToken: Ct));

            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(410, cancellationToken: Ct));
        }

        [Fact]
        public async Task ARangeWriteWhoseLaterBatchFails_LeavesNothingOfItBehind()
        {
            await _fixture.Connection.InsertAsync(NewWidget(421), cancellationToken: Ct);

            // One row per round trip: the first (420) is fine, the second (421) is a duplicate key.
            await Assert.ThrowsAnyAsync<DbException>(() => _fixture.Connection.InsertRangeAsync([NewWidget(420), NewWidget(421)], batchSize: 1, cancellationToken: Ct));

            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(420, cancellationToken: Ct));
            Assert.NotNull(await _fixture.Connection.GetByIdAsync<Widget>(421, cancellationToken: Ct));
            Assert.Equal(ConnectionState.Open, _fixture.Connection.State);
        }

        [Fact]
        public async Task ACancelledToken_StopsTheOperationWritesNothingAndLeavesTheConnectionUsable()
        {
            using CancellationTokenSource cancelled = new();
            cancelled.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _fixture.Connection.InsertAsync(NewWidget(430), cancellationToken: cancelled.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _fixture.Connection.InsertRangeAsync([NewWidget(430), NewWidget(431)], cancellationToken: cancelled.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _fixture.Connection.UpsertRangeAsync([NewWidget(430)], cancellationToken: cancelled.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _fixture.Connection.GetByIdAsync<Widget>(430, cancellationToken: cancelled.Token));

            Assert.Empty(await _fixture.Connection.GetByIdRangeAsync<Widget>(new[] { 430, 431 }, cancellationToken: Ct));
            Assert.Equal(ConnectionState.Open, _fixture.Connection.State);
        }

        [Fact(Timeout = 60_000)]
        public async Task TheCommandTimeout_ReachesTheDatabase()
        {
            CancellationToken ct = TestContext.Current.CancellationToken;
            await _fixture.Connection.InsertAsync(NewWidget(440), cancellationToken: ct);

            // A second connection holds the row's lock, so the update below can only wait: it must give up after the timeout.
            await using SqlConnection blocker = new(_fixture.ConnectionString);
            await blocker.OpenAsync(ct);
            await using SqlTransaction held = blocker.BeginTransaction();
            await blocker.UpdateAsync<Widget>(new { Name = "held" }, w => w.Id == 440, transaction: held, cancellationToken: ct);

            Stopwatch watch = Stopwatch.StartNew();
            await Assert.ThrowsAnyAsync<DbException>(() => _fixture.Connection.UpdateAsync<Widget>(new { Name = "waiting" }, w => w.Id == 440, commandTimeout: 1, cancellationToken: ct));
            watch.Stop();

            held.Rollback();
            Assert.InRange(watch.Elapsed, TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(20));
            Assert.Equal("tx", (await _fixture.Connection.GetByIdAsync<Widget>(440, cancellationToken: ct))!.Name);
        }
    }
}
