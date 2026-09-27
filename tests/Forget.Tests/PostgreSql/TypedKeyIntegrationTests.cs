using Forget.Core.Models;
using Forget.PostgreSql.Extensions;
using Forget.Tests.Core;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// <c>GetById</c>, <c>Delete</c> and <c>DeleteRange</c> with a <c>TKey</c> are the same operations as the ones that take an
    /// untyped identifier, for a caller that already holds it as a <c>TKey</c>: whatever they return, or refuse, must be what
    /// the untyped overloads do with the same identifiers.
    /// </summary>
    [Collection(PostgreSqlCollection.Name)]
    public class TypedKeyIntegrationTests
    {
        private readonly PostgreSqlFixture _fixture;

        public TypedKeyIntegrationTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static Widget NewWidget(int id) => new() { Id = id, Name = "typed", IsActive = true, Price = 1m };

        private static void AssertSameCommand(DbCommandInfo expected, DbCommandInfo actual)
        {
            Assert.Equal(expected.Sql, actual.Sql);

            string[] names = [.. expected.Parameters!.ParameterNames];
            string[] actualNames = [.. actual.Parameters!.ParameterNames];
            Assert.Equal(names, actualNames);

            foreach (string name in names)
            {
                object expectedValue = expected.Parameters!.Get<object>(name);
                object actualValue = actual.Parameters!.Get<object>(name);

                Assert.Equal(expectedValue.GetType(), actualValue.GetType());
                Assert.Equal(expectedValue, actualValue);
            }
        }

        private static void AssertSameCommands(IReadOnlyList<DbCommandInfo> expected, IReadOnlyList<DbCommandInfo> actual)
        {
            Assert.Equal(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++)
            {
                AssertSameCommand(expected[i], actual[i]);
            }
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

        [Fact]
        public async Task GetByIdAsync_WithAnIntId_ReturnsTheRowOrNull()
        {
            await _fixture.Connection.InsertAsync(NewWidget(820_001), cancellationToken: Ct);

            Widget? typed = await _fixture.Connection.GetByIdAsync<Widget, int>(820_001, cancellationToken: Ct);
            Widget? untyped = await _fixture.Connection.GetByIdAsync<Widget>(820_001, cancellationToken: Ct);
            Widget? missing = await _fixture.Connection.GetByIdAsync<Widget, int>(820_999, cancellationToken: Ct);

            Assert.Equal(820_001, typed?.Id);
            Assert.Equal(untyped?.Name, typed?.Name);
            Assert.Null(missing);
        }

        [Fact]
        public void GetById_WithAShortOrALongIdOnAnIntKey_FindsTheRow()
        {
            short shortId = 30_101;
            _fixture.Connection.InsertRange([NewWidget(shortId), NewWidget(820_002)]);

            Widget? byShort = _fixture.Connection.GetById<Widget, short>(shortId);
            Widget? byLong = _fixture.Connection.GetById<Widget, long>(820_002L);

            Assert.Equal(30_101, byShort?.Id);
            Assert.Equal(820_002, byLong?.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithAStringKey_ReturnsTheRow()
        {
            await _fixture.Connection.InsertAsync(new StringKeyed { Code = "TYPED-G1", Name = "one" }, cancellationToken: Ct);

            StringKeyed? row = await _fixture.Connection.GetByIdAsync<StringKeyed, string>("TYPED-G1", cancellationToken: Ct);

            Assert.Equal("one", row?.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithABinaryKey_ReturnsTheRow()
        {
            await _fixture.Connection.InsertAsync(new BinaryKeyed { Id = [7, 0, 1], Name = "bin" }, cancellationToken: Ct);

            BinaryKeyed? row = await _fixture.Connection.GetByIdAsync<BinaryKeyed, byte[]>([7, 0, 1], cancellationToken: Ct);

            Assert.Equal("bin", row?.Name);
        }

        [Fact]
        public async Task GetByIdAsync_WithAGuidKey_ReturnsTheRow()
        {
            Guid id = Guid.NewGuid();
            await _fixture.Connection.InsertAsync(new GuidKeyed { Id = id, Name = "guid" }, cancellationToken: Ct);

            GuidKeyed? row = await _fixture.Connection.GetByIdAsync<GuidKeyed, Guid>(id, cancellationToken: Ct);

            Assert.Equal("guid", row?.Name);
            await _fixture.Connection.DeleteAsync<GuidKeyed>(id, cancellationToken: Ct);
        }

        [Fact]
        public async Task GetByIdAsync_WithAnEnumKey_ReturnsTheRow()
        {
            await _fixture.Connection.InsertAsync(new EnumKeyed { Id = TypeMatrixKind.Beta, Name = "beta" }, cancellationToken: Ct);

            EnumKeyed? row = await _fixture.Connection.GetByIdAsync<EnumKeyed, TypeMatrixKind>(TypeMatrixKind.Beta, cancellationToken: Ct);

            Assert.Equal("beta", row?.Name);
            await _fixture.Connection.DeleteAsync<EnumKeyed>(TypeMatrixKind.Beta, cancellationToken: Ct);
        }

        [Fact]
        public async Task GetByIdAsync_WithTheKeyTypedAsObject_BehavesLikeTheUntypedOverload()
        {
            await _fixture.Connection.InsertAsync(NewWidget(820_003), cancellationToken: Ct);

            Widget? found = await _fixture.Connection.GetByIdAsync<Widget, object>(820_003, cancellationToken: Ct);
            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdAsync<Widget, object>("820003", cancellationToken: Ct));

            Assert.Equal(820_003, found?.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithATypeThatDoesNotFitTheKey_IsRejected()
        {
            ArgumentException text = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdAsync<Widget, string>("820", cancellationToken: Ct));
            ArgumentException unsigned = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdAsync<Widget, ushort>(820, cancellationToken: Ct));

            Assert.All([text, unsigned], ex => Assert.Contains("The type of the provided value", ex.Message));
        }

        [Fact]
        public async Task GetByIdAsync_WithANullId_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => _fixture.Connection.GetByIdAsync<StringKeyed, string>(null!, cancellationToken: Ct));
        }

        [Fact]
        public void GetByIdCommand_BuildsTheSameCommandAsTheUntypedOverload()
        {
            AssertSameCommand(_fixture.Connection.GetByIdCommand<Widget>(820_004), _fixture.Connection.GetByIdCommand<Widget, int>(820_004));
            AssertSameCommand(_fixture.Connection.GetByIdCommand<Widget>(820_004L), _fixture.Connection.GetByIdCommand<Widget, long>(820_004L));
            AssertSameCommand(_fixture.Connection.GetByIdCommand<StringKeyed>("a"), _fixture.Connection.GetByIdCommand<StringKeyed, string>("a"));
            AssertSameCommand(_fixture.Connection.GetByIdCommand<BinaryKeyed>(new byte[] { 1, 2 }), _fixture.Connection.GetByIdCommand<BinaryKeyed, byte[]>([1, 2]));
            AssertSameCommand(_fixture.Connection.GetByIdCommand<EnumKeyed>(TypeMatrixKind.Alpha), _fixture.Connection.GetByIdCommand<EnumKeyed, TypeMatrixKind>(TypeMatrixKind.Alpha));
        }

        [Fact]
        public async Task DeleteAsync_WithAnIntId_DeletesTheRowThatExists()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(820_011), NewWidget(820_012)], cancellationToken: Ct);

            int typed = await _fixture.Connection.DeleteAsync<Widget, int>(820_011, cancellationToken: Ct);
            int untyped = await _fixture.Connection.DeleteAsync<Widget>(820_012, cancellationToken: Ct);
            int missing = await _fixture.Connection.DeleteAsync<Widget, int>(820_999, cancellationToken: Ct);

            Assert.Equal([1, 1, 0], [typed, untyped, missing]);
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(820_011, cancellationToken: Ct));
        }

        [Fact]
        public void Delete_WithAShortOrALongIdOnAnIntKey_DeletesTheRow()
        {
            short shortId = 30_102;
            _fixture.Connection.InsertRange([NewWidget(shortId), NewWidget(820_013)]);

            int byShort = _fixture.Connection.Delete<Widget, short>(shortId);
            int byLong = _fixture.Connection.Delete<Widget, long>(820_013L);

            Assert.Equal([1, 1], [byShort, byLong]);
        }

        [Fact]
        public async Task DeleteAsync_WithAStringAndABinaryKey_DeletesTheRows()
        {
            await _fixture.Connection.InsertAsync(new StringKeyed { Code = "TYPED-D1", Name = "one" }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new BinaryKeyed { Id = [7, 0, 2], Name = "bin" }, cancellationToken: Ct);

            int text = await _fixture.Connection.DeleteAsync<StringKeyed, string>("TYPED-D1", cancellationToken: Ct);
            int binary = await _fixture.Connection.DeleteAsync<BinaryKeyed, byte[]>([7, 0, 2], cancellationToken: Ct);

            Assert.Equal([1, 1], [text, binary]);
        }

        [Fact]
        public async Task DeleteAsync_WithAGuidAndAnEnumKey_DeletesTheRows()
        {
            Guid id = Guid.NewGuid();
            await _fixture.Connection.InsertAsync(new GuidKeyed { Id = id, Name = "guid" }, cancellationToken: Ct);
            await _fixture.Connection.InsertAsync(new EnumKeyed { Id = TypeMatrixKind.Beta, Name = "beta" }, cancellationToken: Ct);

            int guid = await _fixture.Connection.DeleteAsync<GuidKeyed, Guid>(id, cancellationToken: Ct);
            int kind = await _fixture.Connection.DeleteAsync<EnumKeyed, TypeMatrixKind>(TypeMatrixKind.Beta, cancellationToken: Ct);

            Assert.Equal([1, 1], [guid, kind]);
        }

        [Fact]
        public async Task DeleteAsync_InATransaction_CanBeRolledBack()
        {
            await _fixture.Connection.InsertAsync(NewWidget(820_014), cancellationToken: Ct);

            await using var transaction = await _fixture.Connection.BeginTransactionAsync(Ct);
            int inside = await _fixture.Connection.DeleteAsync<Widget, int>(820_014, transaction: transaction, cancellationToken: Ct);
            await transaction.RollbackAsync(Ct);

            Assert.Equal(1, inside);
            Assert.NotNull(await _fixture.Connection.GetByIdAsync<Widget>(820_014, cancellationToken: Ct));
        }

        [Fact]
        public async Task DeleteAsync_WithATypeThatDoesNotFitTheKey_OrANullId_IsRejected()
        {
            ArgumentException text = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.DeleteAsync<Widget, string>("820", cancellationToken: Ct));
            ArgumentException unsigned = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.DeleteAsync<Widget, ushort>(820, cancellationToken: Ct));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _fixture.Connection.DeleteAsync<StringKeyed, string>(null!, cancellationToken: Ct));

            Assert.All([text, unsigned], ex => Assert.Contains("The type of the provided value", ex.Message));
        }

        [Fact]
        public void DeleteCommand_BuildsTheSameCommandAsTheUntypedOverload()
        {
            AssertSameCommand(_fixture.Connection.DeleteCommand<Widget>(820_015), _fixture.Connection.DeleteCommand<Widget, int>(820_015));
            AssertSameCommand(_fixture.Connection.DeleteCommand<Widget>(820_015L), _fixture.Connection.DeleteCommand<Widget, long>(820_015L));
            AssertSameCommand(_fixture.Connection.DeleteCommand<StringKeyed>("a"), _fixture.Connection.DeleteCommand<StringKeyed, string>("a"));
            AssertSameCommand(_fixture.Connection.DeleteCommand<EnumKeyed>(TypeMatrixKind.Alpha), _fixture.Connection.DeleteCommand<EnumKeyed, TypeMatrixKind>(TypeMatrixKind.Alpha));
        }

        [Fact]
        public async Task DeleteRangeAsync_WithIntIds_DeletesTheRowsThatExist()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(820_021), NewWidget(820_022), NewWidget(820_023)], cancellationToken: Ct);

            int deleted = await _fixture.Connection.DeleteRangeAsync<Widget, int>([820_021, 820_022, 820_999], batchSize: 1, cancellationToken: Ct);

            Assert.Equal(2, deleted);
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(820_021, cancellationToken: Ct));
            Assert.NotNull(await _fixture.Connection.GetByIdAsync<Widget>(820_023, cancellationToken: Ct));
        }

        [Fact]
        public async Task DeleteRangeAsync_WithTheSameIdTwiceInDifferentBatches_CountsTheRowOnce()
        {
            await _fixture.Connection.InsertAsync(NewWidget(820_031), cancellationToken: Ct);

            int deleted = await _fixture.Connection.DeleteRangeAsync<Widget, int>([820_031, 820_031], batchSize: 1, cancellationToken: Ct);

            Assert.Equal(1, deleted);
        }

        [Fact]
        public async Task DeleteRangeAsync_WithNoIds_DeletesNothing()
        {
            int deleted = await _fixture.Connection.DeleteRangeAsync<Widget, int>([], cancellationToken: Ct);

            Assert.Equal(0, deleted);
        }

        [Fact]
        public async Task DeleteRangeAsync_WithLongIdsOnAnIntKey_WorksAcrossBatches()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(820_041), NewWidget(820_042), NewWidget(820_043)], cancellationToken: Ct);

            int deleted = await _fixture.Connection.DeleteRangeAsync<Widget, long>([820_041, 820_042, 820_043], batchSize: 1, cancellationToken: Ct);

            Assert.Equal(3, deleted);
        }

        [Fact]
        public async Task DeleteRangeAsync_WithAStringAndABinaryKey_DeletesTheRowsWhateverTheBatchSize()
        {
            await _fixture.Connection.InsertRangeAsync([new StringKeyed { Code = "TYPED-R1", Name = "a" }, new StringKeyed { Code = "TYPED-R2", Name = "b" }], cancellationToken: Ct);
            await _fixture.Connection.InsertRangeAsync([new BinaryKeyed { Id = [8, 0, 1], Name = "a" }, new BinaryKeyed { Id = [8, 0, 2], Name = "b" }], cancellationToken: Ct);

            int text = await _fixture.Connection.DeleteRangeAsync<StringKeyed, string>(["TYPED-R1", "TYPED-R2"], batchSize: 1, cancellationToken: Ct);
            int binary = await _fixture.Connection.DeleteRangeAsync<BinaryKeyed, byte[]>([[8, 0, 1], [8, 0, 2]], batchSize: 1, cancellationToken: Ct);

            Assert.Equal([2, 2], [text, binary]);
        }

        [Fact]
        public async Task DeleteRangeAsync_WithAGuidAndAnEnumKey_DeletesTheRows()
        {
            Guid first = Guid.NewGuid();
            Guid second = Guid.NewGuid();
            await _fixture.Connection.InsertRangeAsync([new GuidKeyed { Id = first, Name = "a" }, new GuidKeyed { Id = second, Name = "b" }], cancellationToken: Ct);
            await _fixture.Connection.InsertRangeAsync([new EnumKeyed { Id = TypeMatrixKind.Alpha, Name = "a" }, new EnumKeyed { Id = TypeMatrixKind.Beta, Name = "b" }], cancellationToken: Ct);

            int guids = await _fixture.Connection.DeleteRangeAsync<GuidKeyed, Guid>([first, second], batchSize: 1, cancellationToken: Ct);
            int kinds = await _fixture.Connection.DeleteRangeAsync<EnumKeyed, TypeMatrixKind>([TypeMatrixKind.Alpha, TypeMatrixKind.Beta], cancellationToken: Ct);

            Assert.Equal([2, 2], [guids, kinds]);
        }

        [Fact]
        public async Task DeleteRangeAsync_InATransaction_CanBeRolledBack()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(820_051), NewWidget(820_052)], cancellationToken: Ct);

            await using var transaction = await _fixture.Connection.BeginTransactionAsync(Ct);
            int inside = await _fixture.Connection.DeleteRangeAsync<Widget, int>([820_051, 820_052], transaction: transaction, cancellationToken: Ct);
            await transaction.RollbackAsync(Ct);

            Assert.Equal(2, inside);
            Assert.NotNull(await _fixture.Connection.GetByIdAsync<Widget>(820_051, cancellationToken: Ct));
        }

        [Fact]
        public async Task DeleteRangeAsync_WithTheKeyTypedAsObject_BehavesLikeTheUntypedOverload()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(820_061), NewWidget(820_062)], cancellationToken: Ct);

            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.DeleteRangeAsync<Widget, object>([820_061, 820_062L], cancellationToken: Ct));
            int deleted = await _fixture.Connection.DeleteRangeAsync<Widget, object>([820_061, 820_062], cancellationToken: Ct);

            Assert.Equal(2, deleted);
        }

        [Fact]
        public async Task DeleteRangeAsync_WithATypeThatDoesNotFitTheKey_AListOfBytes_OrANullId_IsRejected()
        {
            ArgumentException text = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.DeleteRangeAsync<Widget, string>(["820"], cancellationToken: Ct));
            ArgumentException unsigned = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.DeleteRangeAsync<Widget, ushort>([820], cancellationToken: Ct));
            ArgumentException bytes = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.DeleteRangeAsync<Widget, byte>("N"u8.ToArray(), cancellationToken: Ct));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _fixture.Connection.DeleteRangeAsync<StringKeyed, string>(["typed-2", null!], cancellationToken: Ct));
            await Assert.ThrowsAsync<ArgumentNullException>(() => _fixture.Connection.DeleteRangeAsync<Widget, int>(null!, cancellationToken: Ct));

            Assert.All([text, unsigned], ex => Assert.Contains("The type of the provided value", ex.Message));
            Assert.Contains("list of bytes", bytes.Message);
        }

        [Fact]
        public void DeleteRange_WithShortIds_DeletesTheRow()
        {
            short id = 30_103;
            _fixture.Connection.Insert(NewWidget(id));

            int deleted = _fixture.Connection.DeleteRange<Widget, short>([id]);

            Assert.Equal(1, deleted);
        }

        [Fact]
        public void DeleteRangeCommands_BuildTheSameCommandsAsTheUntypedOverload()
        {
            int[] ints = [820_071, 820_072, 820_073, 820_074, 820_075];
            long[] longs = [820_071, 820_072, 820_073];
            string[] strings = ["a", "b", "c"];
            byte[][] binaries = [[1], [2], [3]];
            TypeMatrixKind[] kinds = [TypeMatrixKind.Alpha, TypeMatrixKind.Beta];

            AssertSameCommands(_fixture.Connection.DeleteRangeCommands<Widget>(ints, batchSize: 2), _fixture.Connection.DeleteRangeCommands<Widget, int>(ints, batchSize: 2));
            AssertSameCommands(_fixture.Connection.DeleteRangeCommands<Widget>(longs, batchSize: 500), _fixture.Connection.DeleteRangeCommands<Widget, long>(longs, batchSize: 500));
            AssertSameCommands(_fixture.Connection.DeleteRangeCommands<StringKeyed>(strings, batchSize: 2), _fixture.Connection.DeleteRangeCommands<StringKeyed, string>(strings, batchSize: 2));
            AssertSameCommands(_fixture.Connection.DeleteRangeCommands<BinaryKeyed>(binaries, batchSize: 1), _fixture.Connection.DeleteRangeCommands<BinaryKeyed, byte[]>(binaries, batchSize: 1));
            AssertSameCommands(_fixture.Connection.DeleteRangeCommands<Widget>(ints, batchSize: 0), _fixture.Connection.DeleteRangeCommands<Widget, int>(ints, batchSize: 0));
            AssertSameCommands(_fixture.Connection.DeleteRangeCommands<EnumKeyed>(kinds), _fixture.Connection.DeleteRangeCommands<EnumKeyed, TypeMatrixKind>(kinds));
        }

        [Fact]
        public void DeleteRangeCommands_DoNotHandTheCallersArrayToTheParameters()
        {
            int[] ids = [820_081, 820_082];

            IReadOnlyList<DbCommandInfo> commands = _fixture.Connection.DeleteRangeCommands<Widget, int>(ids);
            ids[0] = -1;

            Assert.Equal(new[] { 820_081, 820_082 }, commands[0].Parameters!.Get<int[]>("IdArray0"));
        }

        [Fact]
        public void DeleteRangeCommands_AllocateLessThanTheUntypedOverload()
        {
            int[] ids = [.. Enumerable.Range(1, 1000)];

            long untyped = Allocated(() => _fixture.Connection.DeleteRangeCommands<Widget>(ids, batchSize: 0));
            long typed = Allocated(() => _fixture.Connection.DeleteRangeCommands<Widget, int>(ids, batchSize: 0));

            Assert.True(typed < untyped / 2, $"typed {typed} B, untyped {untyped} B");
        }
    }
}
