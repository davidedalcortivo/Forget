using Forget.Core.Models;
using Forget.SqlServer.Extensions;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// A value is never converted by Forget, so what a caller may pass for an id, a filter value or an update value is
    /// decided by whether the database can compare or store it without losing anything: an integer for any integer
    /// property, an integer for a decimal one, and so on. These tests run the accepted cases against a real engine.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public class TypeCompatibilityIntegrationTests
    {
        private readonly SqlServerFixture _fixture;

        public TypeCompatibilityIntegrationTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static Widget NewWidget(int id, decimal price = 1m, int? quantity = null) => new() { Id = id, Name = "compat", IsActive = true, Price = price, Quantity = quantity };

        [Fact]
        public async Task GetByIdAsync_WithALongIdOnAnIntKey_FindsTheRow()
        {
            await _fixture.Connection.InsertAsync(NewWidget(700), cancellationToken: Ct);

            Widget? row = await _fixture.Connection.GetByIdAsync<Widget>(700L, cancellationToken: Ct);

            Assert.Equal(700, row?.Id);
        }

        [Fact]
        public async Task DeleteAsync_WithALongIdOnAnIntKey_DeletesTheRow()
        {
            await _fixture.Connection.InsertAsync(NewWidget(701), cancellationToken: Ct);

            int deleted = await _fixture.Connection.DeleteAsync<Widget>(701L, cancellationToken: Ct);

            Assert.Equal(1, deleted);
            Assert.Null(await _fixture.Connection.GetByIdAsync<Widget>(701, cancellationToken: Ct));
        }

        [Fact]
        public async Task GetByIdRangeAsync_And_DeleteRangeAsync_WithLongIdsOnAnIntKey_WorkAcrossBatches()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(702), NewWidget(703), NewWidget(704)], cancellationToken: Ct);
            long[] ids = [702, 703, 704];

            IReadOnlyList<Widget> fetched = await _fixture.Connection.GetByIdRangeAsync<Widget>(ids, batchSize: 1, cancellationToken: Ct);
            int deleted = await _fixture.Connection.DeleteRangeAsync<Widget>(ids, batchSize: 1, cancellationToken: Ct);

            Assert.Equal([702, 703, 704], fetched.Select(w => w.Id).Order());
            Assert.Equal(3, deleted);
        }

        [Fact]
        public async Task GetByIdRangeAsync_WithIdsOfDifferentTypes_IsRejected()
        {
            object[] ids = [705, 706L];

            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdRangeAsync<Widget>(ids, cancellationToken: Ct));
        }

        [Fact]
        public async Task GetByIdAsync_WithAStringIdOnAnIntKey_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdAsync<Widget>("707", cancellationToken: Ct));
        }

        [Fact]
        public async Task GetAllAsync_WithIntegerValuesOnDecimalAndIntProperties_MatchesTheRows()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(708, price: 12.5m, quantity: 3), NewWidget(709, price: 5m, quantity: 4)], cancellationToken: Ct);

            IReadOnlyList<Widget> expensive = await _fixture.Connection.GetAllAsync(new FilterDescriptor<Widget>("Price", 10, ComparisonOperator.GreaterThanOrEqual), cancellationToken: Ct);
            IReadOnlyList<Widget> four = await _fixture.Connection.GetAllAsync(new FilterDescriptor<Widget>("Quantity", 4L), cancellationToken: Ct);

            Assert.Contains(expensive, w => w.Id == 708);
            Assert.DoesNotContain(expensive, w => w.Id == 709);
            Assert.Contains(four, w => w.Id == 709);
            Assert.DoesNotContain(four, w => w.Id == 708);
        }

        [Fact]
        public async Task GetAllAsync_WithAListOfLongsOnAnIntProperty_MatchesTheRows()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(710), NewWidget(711), NewWidget(712)], cancellationToken: Ct);

            IReadOnlyList<Widget> rows = await _fixture.Connection.GetAllAsync(new FilterDescriptor<Widget>("Id", new long[] { 710, 712 }, ComparisonOperator.In), cancellationToken: Ct);

            Assert.Equal([710, 712], rows.Select(w => w.Id).Where(id => id is >= 710 and <= 712).Order());
        }

        [Fact]
        public async Task UpdateAsync_WithValuesOfNarrowerTypes_WritesThem()
        {
            await _fixture.Connection.InsertAsync(NewWidget(713), cancellationToken: Ct);

            await _fixture.Connection.UpdateAsync<Widget>(new { Price = 42, Quantity = 7 }, w => w.Id == 713, cancellationToken: Ct);
            Widget? row = await _fixture.Connection.GetByIdAsync<Widget>(713, cancellationToken: Ct);

            Assert.Equal(42m, row?.Price);
            Assert.Equal(7, row?.Quantity);
        }

        [Fact]
        public async Task UpdateAsync_WithAValueWiderThanTheProperty_IsRejected()
        {
            await _fixture.Connection.InsertAsync(NewWidget(716), cancellationToken: Ct);

            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.UpdateAsync<Widget>(new { Quantity = 7L }, w => w.Id == 716, cancellationToken: Ct));
        }

        [Fact]
        public async Task GetByIdAsync_And_GetByIdRangeAsync_WithAByteOrAShortIdOnAnIntKey_FindTheRow()
        {
            await _fixture.Connection.InsertAsync(NewWidget(78), cancellationToken: Ct);

            Widget? byByte = await _fixture.Connection.GetByIdAsync<Widget>((byte)78, cancellationToken: Ct);
            IReadOnlyList<Widget> byShorts = await _fixture.Connection.GetByIdRangeAsync<Widget>(new short[] { 78 }, cancellationToken: Ct);

            Assert.Equal(78, byByte?.Id);
            Assert.Equal([78], byShorts.Select(w => w.Id));
        }

        [Fact]
        public async Task ASignedByteOrAnUnsignedValue_IsRejectedByForgetWhateverTheDriver()
        {
            ArgumentException byId = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdAsync<Widget>(78u, cancellationToken: Ct));
            ArgumentException range = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdRangeAsync<Widget>(new ushort[] { 78 }, cancellationToken: Ct));
            ArgumentException filter = Assert.Throws<ArgumentException>(() => new FilterDescriptor<Widget>("Id", (sbyte)78));

            Assert.All([byId, range, filter], ex => Assert.Contains("The type of the provided value", ex.Message));
        }

        [Fact]
        public async Task AListOfBytes_IsRejectedByForget_BecauseTheDriversBindItAsOneBinaryValue()
        {
            ArgumentException range = await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.GetByIdRangeAsync<Widget>("N"u8.ToArray(), cancellationToken: Ct));
            ArgumentException filter = Assert.Throws<ArgumentException>(() => new FilterDescriptor<Widget>("Id", "N"u8.ToArray(), ComparisonOperator.In));

            Assert.All([range, filter], ex => Assert.Contains("list of bytes", ex.Message));
        }
        [Fact]
        public void TheSynchronousMethods_ApplyTheSameRule()
        {
            _fixture.Connection.Insert(NewWidget(79));

            Widget? byLong = _fixture.Connection.GetById<Widget>(79L);
            IReadOnlyList<Widget> range = _fixture.Connection.GetByIdRange<Widget>(new long[] { 79 });
            int updated = _fixture.Connection.Update<Widget>(new { Price = 5, Quantity = 3 }, w => w.Id == 79);
            int deleted = _fixture.Connection.DeleteRange<Widget>(new long[] { 79 });

            Assert.Equal(79, byLong?.Id);
            Assert.Equal([79], range.Select(w => w.Id));
            Assert.Equal(1, updated);
            Assert.Equal(1, deleted);
            Assert.Throws<ArgumentException>(() => _fixture.Connection.GetById<Widget>(79u));
            Assert.Throws<ArgumentException>(() => _fixture.Connection.Update<Widget>(new { Quantity = 3L }, w => w.Id == 79));
        }
        [Fact]
        public async Task GetAllAsync_WithAByteArrayValue_MatchesTheRow()
        {
            await _fixture.Connection.InsertAsync(new BinaryKeyed { Id = [5, 0, 1], Name = "bin" }, cancellationToken: Ct);

            IReadOnlyList<BinaryKeyed> rows = await _fixture.Connection.GetAllAsync(new FilterDescriptor<BinaryKeyed>("Id", new byte[] { 5, 0, 1 }), cancellationToken: Ct);

            Assert.Equal(["bin"], rows.Select(r => r.Name));
        }

        [Fact]
        public async Task MinAsync_And_MaxAsync_WithAResultTypeThatCanHoldTheProperty_ReturnTheValue()
        {
            await _fixture.Connection.InsertRangeAsync([NewWidget(714, quantity: 6), NewWidget(715, quantity: 9)], cancellationToken: Ct);

            long? min = await _fixture.Connection.MinAsync<Widget, long>("Quantity", w => w.Id >= 714 && w.Id <= 715, cancellationToken: Ct);
            decimal? max = await _fixture.Connection.MaxAsync<Widget, decimal>("Quantity", w => w.Id >= 714 && w.Id <= 715, cancellationToken: Ct);

            Assert.Equal(6L, min);
            Assert.Equal(9m, max);
        }

        [Fact]
        public async Task MinAsync_And_MaxAsync_WithAResultTypeThatCannotHoldTheProperty_AreRejected()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.MinAsync<Widget, int>("Price", cancellationToken: Ct));
            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.MaxAsync<Widget, short>("Quantity", cancellationToken: Ct));
        }
    }
}
