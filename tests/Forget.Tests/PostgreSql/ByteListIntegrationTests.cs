using Forget.Core.Models;
using Forget.PostgreSql.Extensions;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// A list of bytes, or of enum values whose underlying type is byte, is bound as a list of <c>short</c>: every driver
    /// binds a <c>byte[]</c> as one binary value, so the list bound as it was made the statement fail (or, for ids and
    /// <c>In</c> filters on bytes, was refused). A <c>short</c> holds every byte, so the values compared are the same.
    /// </summary>
    [Collection(PostgreSqlCollection.Name)]
    public class ByteListIntegrationTests
    {
        private readonly PostgreSqlFixture _fixture;

        public ByteListIntegrationTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static ParameterNameRow Row(int id) => new() { Id = id, Address = "byte", Address2 = "list" };

        [Fact]
        public async Task ContainsOnAListOfBytes_FindsTheRows()
        {
            byte[] codes = [1, 3];

            Assert.Equal(2, await _fixture.Connection.CountAsync<ByteListRow>(x => codes.Contains(x.Code), cancellationToken: Ct));
        }

        [Fact]
        public async Task InWithAListOfBytes_FindsTheRows()
        {
            FilterGroup<ByteListRow> filter = new([new FilterDescriptor<ByteListRow>("Code", new byte[] { 1, 3 }, ComparisonOperator.In)]);

            Assert.Equal(2, await _fixture.Connection.CountAsync(filter, cancellationToken: Ct));
        }

        [Fact]
        public async Task ContainsOnAListOfEnumsWhoseUnderlyingTypeIsByte_FindsTheRows()
        {
            ByteStatus[] statuses = [ByteStatus.Active, ByteStatus.Closed];

            Assert.Equal(2, await _fixture.Connection.CountAsync<ByteListRow>(x => statuses.Contains(x.Status), cancellationToken: Ct));
        }

        [Fact]
        public async Task InWithAListOfEnumsWhoseUnderlyingTypeIsByte_FindsTheRows()
        {
            FilterGroup<ByteListRow> filter = new([new FilterDescriptor<ByteListRow>("Status", new[] { ByteStatus.Active, ByteStatus.Closed }, ComparisonOperator.In)]);

            Assert.Equal(2, await _fixture.Connection.CountAsync(filter, cancellationToken: Ct));
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithByteIds_FindsTheRows()
        {
            await _fixture.Connection.InsertRangeAsync([Row(60), Row(61)], cancellationToken: Ct);
            byte[] ids = [60, 61];

            IReadOnlyList<ParameterNameRow> untyped = await _fixture.Connection.GetByIdRangeAsync<ParameterNameRow>(ids, cancellationToken: Ct);
            IReadOnlyList<ParameterNameRow> typed = await _fixture.Connection.GetByIdRangeAsync<ParameterNameRow, byte>(ids, cancellationToken: Ct);

            Assert.Equal([60, 61], untyped.Select(x => x.Id).Order());
            Assert.Equal([60, 61], typed.Select(x => x.Id).Order());
        }

        [Fact]
        public async Task DeleteRangeAsync_WithByteIds_DeletesTheRows()
        {
            await _fixture.Connection.InsertRangeAsync([Row(62), Row(63), Row(64), Row(65)], cancellationToken: Ct);

            int untyped = await _fixture.Connection.DeleteRangeAsync<ParameterNameRow>(">?"u8.ToArray(), cancellationToken: Ct);
            int typed = await _fixture.Connection.DeleteRangeAsync<ParameterNameRow, byte>("@A"u8.ToArray(), cancellationToken: Ct);

            Assert.Equal((2, 2), (untyped, typed));
        }

        [Fact]
        public async Task InWithAListOfBytesOnAnIntProperty_FindsTheRows()
        {
            await _fixture.Connection.InsertRangeAsync([Row(66), Row(67)], cancellationToken: Ct);
            FilterGroup<ParameterNameRow> filter = new([new FilterDescriptor<ParameterNameRow>("Id", "BC"u8.ToArray(), ComparisonOperator.In)]);

            Assert.Equal(2, await _fixture.Connection.CountAsync(filter, cancellationToken: Ct));
        }

        [Fact]
        public void GetByIdRangeCommands_WithEnumIdsWhoseUnderlyingTypeIsByte_BindsThemAsShort()
        {
            IReadOnlyList<DbCommandInfo> commands = _fixture.Connection.GetByIdRangeCommands<ByteStatusKeyRow>(new[] { ByteStatus.Active, ByteStatus.Closed });

            Assert.Equal(new short[] { 1, 3 }, commands.Single().Parameters!.Get<short[]>("IdArray_0_"));
        }
    }
}
