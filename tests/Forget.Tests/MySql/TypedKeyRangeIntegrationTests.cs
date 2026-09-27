using Forget.Core.Models;
using Forget.MySql.Extensions;
using MySqlConnector;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// <c>GetByIdRange&lt;TEntity, TKey&gt;</c> is the same operation as <c>GetByIdRange&lt;TEntity&gt;</c> for a caller that
    /// already holds its ids as a <c>TKey</c>: it only skips the work of finding out, one boxed id at a time, what the
    /// type is. Whatever it returns, or refuses, must therefore be what the untyped overload does with the same ids.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class TypedKeyRangeIntegrationTests
    {
        private readonly MySqlFixture _fixture;

        public TypedKeyRangeIntegrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static Widget NewWidget(int id) => new() { Id = id, Name = "typed", IsActive = true, Price = 1m };

        private static void AssertSameCommands(IReadOnlyList<DbCommandInfo> expected, IReadOnlyList<DbCommandInfo> actual)
        {
            Assert.Equal(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.Equal(expected[i].Sql, actual[i].Sql);

                string[] names = [.. expected[i].Parameters!.ParameterNames];
                string[] actualNames = [.. actual[i].Parameters!.ParameterNames];
                Assert.Equal(names, actualNames);

                foreach (string name in names)
                {
                    object expectedValue = expected[i].Parameters!.Get<object>(name);
                    object actualValue = actual[i].Parameters!.Get<object>(name);

                    Assert.Equal(expectedValue.GetType(), actualValue.GetType());
                    Assert.Equal(expectedValue, actualValue);
                }
            }
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithIntIds_ReturnsTheRowsThatExist()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(800_001), NewWidget(800_002), NewWidget(800_003)], cancellationToken: Ct);
            int[] ids = [800_001, 800_003, 800_999];

            IReadOnlyList<Widget> typed = await _fixture.Connection.GetByIdRangeAsync<Widget, int>(ids, cancellationToken: Ct);
            IReadOnlyList<Widget> untyped = await _fixture.Connection.GetByIdRangeAsync<Widget>(ids, cancellationToken: Ct);

            Assert.Equal([800_001, 800_003], typed.Select(w => w.Id).Order());
            Assert.Equal(untyped.Select(w => w.Id).Order(), typed.Select(w => w.Id).Order());
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithRepeatedIdsAndOneIdPerBatch_ReturnsEachRowOnce()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(800_011), NewWidget(800_012)], cancellationToken: Ct);
            List<int> ids = [800_011, 800_012, 800_011, 800_012];

            IReadOnlyList<Widget> rows = await _fixture.Connection.GetByIdRangeAsync<Widget, int>(ids, batchSize: 1, cancellationToken: Ct);

            Assert.Equal([800_011, 800_012], rows.Select(w => w.Id).Order());
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithNoIds_ReturnsNothing()
        {
            IReadOnlyList<Widget> rows = await _fixture.Connection.GetByIdRangeAsync<Widget, int>([], cancellationToken: Ct);

            Assert.Empty(rows);
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithLongIdsOnAnIntKey_WorksAcrossBatches()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(800_021), NewWidget(800_022), NewWidget(800_023)], cancellationToken: Ct);
            long[] ids = [800_021, 800_022, 800_023];

            IReadOnlyList<Widget> rows = await _fixture.Connection.GetByIdRangeAsync<Widget, long>(ids, batchSize: 1, cancellationToken: Ct);

            Assert.Equal([800_021, 800_022, 800_023], rows.Select(w => w.Id).Order());
        }

        [Fact]
        public void GetByIdRange_WithShortIds_FindsTheRow()
        {
            short id = 30_001;
            _fixture.Connection.Insert(NewWidget(id));

            IReadOnlyList<Widget> rows = _fixture.Connection.GetByIdRange<Widget, short>([id]);

            Assert.Equal([30_001], rows.Select(w => w.Id));
        }

        [Fact]
        public async Task GetByIdRangeAsync_InATransaction_SeesTheUncommittedRows()
        {
            using MySqlTransaction transaction = _fixture.Connection.BeginTransaction();
            await _fixture.Connection.InsertAsync(NewWidget(800_031), transaction: transaction, cancellationToken: Ct);

            IReadOnlyList<Widget> inside = await _fixture.Connection.GetByIdRangeAsync<Widget, int>([800_031], transaction: transaction, cancellationToken: Ct);
            transaction.Rollback();
            IReadOnlyList<Widget> after = await _fixture.Connection.GetByIdRangeAsync<Widget, int>([800_031], cancellationToken: Ct);

            Assert.Single(inside);
            Assert.Empty(after);
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithAStringKeyOfAnotherCase_ReturnsOnlyTheRowTheDatabaseMatches()
        {
            await _fixture.Connection.InsertAsync(new StringKeyed { Code = "TYPED-1", Name = "one" }, cancellationToken: Ct);

            IReadOnlyList<StringKeyed> rows = await _fixture.Connection.GetByIdRangeAsync<StringKeyed, string>(["typed-1", "TYPED-1"], batchSize: 1, cancellationToken: Ct);

            Assert.Equal(["TYPED-1"], rows.Select(r => r.Code));
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithABinaryKeyRequestedTwiceInDifferentBatches_ReturnsTheRowOnce()
        {
            await _fixture.Connection.InsertAsync(new BinaryKeyed { Id = [9, 0, 1], Name = "only" }, cancellationToken: Ct);
            byte[][] ids = [[9, 0, 1], [9, 0, 1]];

            IReadOnlyList<BinaryKeyed> rows = await _fixture.Connection.GetByIdRangeAsync<BinaryKeyed, byte[]>(ids, batchSize: 1, cancellationToken: Ct);

            Assert.Equal(["only"], rows.Select(r => r.Name));
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithTheKeyTypedAsObject_BehavesLikeTheUntypedOverload()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(800_041), NewWidget(800_042)], cancellationToken: Ct);

            IReadOnlyList<Widget> uniform = await _fixture.Connection.GetByIdRangeAsync<Widget, object>([800_041, 800_042], cancellationToken: Ct);
            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdRangeAsync<Widget, object>([800_041, 800_042L], cancellationToken: Ct));

            Assert.Equal([800_041, 800_042], uniform.Select(w => w.Id).Order());
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithATypeThatDoesNotFitTheKey_IsRejected()
        {
            ArgumentException text = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdRangeAsync<Widget, string>(["800"], cancellationToken: Ct));
            ArgumentException unsigned = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdRangeAsync<Widget, ushort>([800], cancellationToken: Ct));

            Assert.All([text, unsigned], ex => Assert.Contains("The type of the provided value", ex.Message));
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithAListOfBytes_IsRejected()
        {
            ArgumentException range = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdRangeAsync<Widget, byte>("N"u8.ToArray(), cancellationToken: Ct));

            Assert.Contains("list of bytes", range.Message);
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithANullId_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _fixture.Connection.GetByIdRangeAsync<StringKeyed, string>(["typed-2", null!], cancellationToken: Ct));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _fixture.Connection.GetByIdRangeAsync<Widget, int>(null!, cancellationToken: Ct));
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithTheSameIdRequestedTwiceInDifferentBatches_ReturnsTheRowOnce()
        {
            await _fixture.Connection.InsertAsync(NewWidget(800_071), cancellationToken: Ct);
            int[] ids = [800_071, 800_071];

            IReadOnlyList<Widget> untyped = await _fixture.Connection.GetByIdRangeAsync<Widget>(ids, batchSize: 1, cancellationToken: Ct);
            IReadOnlyList<Widget> typed = await _fixture.Connection.GetByIdRangeAsync<Widget, int>(ids, batchSize: 1, cancellationToken: Ct);

            Assert.Equal([800_071], untyped.Select(w => w.Id));
            Assert.Equal([800_071], typed.Select(w => w.Id));
        }

        [Fact]
        public void GetByIdRangeCommands_BuildTheSameCommandsAsTheUntypedOverload()
        {
            int[] ints = [800_051, 800_052, 800_053, 800_054, 800_055];
            long[] longs = [800_051, 800_052, 800_053];
            string[] strings = ["a", "b", "c"];
            byte[][] binaries = [[1], [2], [3]];

            AssertSameCommands(_fixture.Connection.GetByIdRangeCommands<Widget>(ints, batchSize: 2), _fixture.Connection.GetByIdRangeCommands<Widget, int>(ints, batchSize: 2));
            AssertSameCommands(_fixture.Connection.GetByIdRangeCommands<Widget>(longs, batchSize: 500), _fixture.Connection.GetByIdRangeCommands<Widget, long>(longs, batchSize: 500));
            AssertSameCommands(_fixture.Connection.GetByIdRangeCommands<StringKeyed>(strings, batchSize: 2), _fixture.Connection.GetByIdRangeCommands<StringKeyed, string>(strings, batchSize: 2));
            AssertSameCommands(_fixture.Connection.GetByIdRangeCommands<BinaryKeyed>(binaries, batchSize: 1), _fixture.Connection.GetByIdRangeCommands<BinaryKeyed, byte[]>(binaries, batchSize: 1));
            AssertSameCommands(_fixture.Connection.GetByIdRangeCommands<Widget>(ints, batchSize: 0), _fixture.Connection.GetByIdRangeCommands<Widget, int>(ints, batchSize: 0));
        }

        [Fact]
        public void GetByIdRangeCommands_DoNotHandTheCallersArrayToTheParameters()
        {
            int[] ids = [800_061, 800_062];

            IReadOnlyList<DbCommandInfo> commands = _fixture.Connection.GetByIdRangeCommands<Widget, int>(ids);
            ids[0] = -1;

            Assert.Equal(new[] { 800_061, 800_062 }, commands[0].Parameters!.Get<int[]>("IdArray0"));
        }

        [Fact]
        public void GetByIdRangeCommands_AllocateLessThanTheUntypedOverload()
        {
            int[] ids = [.. Enumerable.Range(1, 1000)];

            long untyped = Allocated(() => _fixture.Connection.GetByIdRangeCommands<Widget>(ids, batchSize: 0));
            long typed = Allocated(() => _fixture.Connection.GetByIdRangeCommands<Widget, int>(ids, batchSize: 0));

            Assert.True(typed < untyped / 2, $"typed {typed} B, untyped {untyped} B");
        }

        private static long Allocated(Func<object> action)
        {
            for (int i = 0; i < 20; i++)
            {
                action();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 50; i++)
            {
                action();
            }

            return (GC.GetAllocatedBytesForCurrentThread() - before) / 50;
        }
    }
}
