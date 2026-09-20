using Forget.MySql.Extensions;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// <c>GetByIdRange</c> asks the database for the rows and returns what it finds. Which rows match a key is the
    /// database's decision, not .NET's: with a case-insensitive collation <c>'abc'</c> matches the row stored as
    /// <c>'ABC'</c>, and a <c>byte[]</c> key is compared by content by the database but by reference by .NET.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class KeyedRangeIntegrationTests
    {
        private readonly MySqlFixture _fixture;

        public KeyedRangeIntegrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task GetByIdRangeAsync_WithAStringKeyOfAnotherCase_ReturnsTheRowTheDatabaseMatches()
        {
            await _fixture.Connection.InsertAsync(new StringKeyed { Code = "ABC-1", Name = "one" }, cancellationToken: Ct);

            IReadOnlyList<StringKeyed> rows = await _fixture.Connection.GetByIdRangeAsync<StringKeyed>(new[] { "abc-1" }, cancellationToken: Ct);

            Assert.Equal(["ABC-1"], rows.Select(r => r.Code));
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithTwoIdsTheDatabaseConsidersEqual_ReturnsTheRowOnce()
        {
            await _fixture.Connection.InsertAsync(new StringKeyed { Code = "ABC-2", Name = "two" }, cancellationToken: Ct);

            IReadOnlyList<StringKeyed> rows = await _fixture.Connection.GetByIdRangeAsync<StringKeyed>(new[] { "abc-2", "Abc-2" }, cancellationToken: Ct);

            Assert.Equal(["ABC-2"], rows.Select(r => r.Code));
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithTwoIdsTheDatabaseConsidersEqualInDifferentBatches_ReturnsTheRowOnce()
        {
            await _fixture.Connection.InsertAsync(new StringKeyed { Code = "ABC-3", Name = "three" }, cancellationToken: Ct);

            IReadOnlyList<StringKeyed> rows = await _fixture.Connection.GetByIdRangeAsync<StringKeyed>(new[] { "abc-3", "Abc-3" }, batchSize: 1, cancellationToken: Ct);

            Assert.Equal(["ABC-3"], rows.Select(r => r.Code));
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithABinaryKey_ReturnsTheRowsWhateverTheBatchSize()
        {
            await _fixture.Connection.InsertRangeAsync(
            [
                new BinaryKeyed { Id = [1, 0, 1], Name = "a" },
                new BinaryKeyed { Id = [1, 0, 2], Name = "b" },
                new BinaryKeyed { Id = [1, 0, 3], Name = "c" }
            ], cancellationToken: Ct);
            byte[][] ids = [[1, 0, 1], [1, 0, 2], [1, 0, 3]];

            IReadOnlyList<BinaryKeyed> together = await _fixture.Connection.GetByIdRangeAsync<BinaryKeyed>(ids, cancellationToken: Ct);
            IReadOnlyList<BinaryKeyed> apart = await _fixture.Connection.GetByIdRangeAsync<BinaryKeyed>(ids, batchSize: 1, cancellationToken: Ct);

            Assert.Equal(["a", "b", "c"], together.Select(r => r.Name).Order());
            Assert.Equal(["a", "b", "c"], apart.Select(r => r.Name).Order());
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithTheSameBinaryKeyRequestedTwiceInDifferentBatches_ReturnsTheRowOnce()
        {
            await _fixture.Connection.InsertAsync(new BinaryKeyed { Id = [2, 0, 1], Name = "only" }, cancellationToken: Ct);
            byte[][] ids = [[2, 0, 1], [2, 0, 1]];

            IReadOnlyList<BinaryKeyed> rows = await _fixture.Connection.GetByIdRangeAsync<BinaryKeyed>(ids, batchSize: 1, cancellationToken: Ct);

            Assert.Equal(["only"], rows.Select(r => r.Name));
        }
    }
}
