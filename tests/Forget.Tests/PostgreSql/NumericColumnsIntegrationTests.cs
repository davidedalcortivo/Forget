using Forget.Core.Models;
using Forget.Tests.Core;
using Forget.PostgreSql.Extensions;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// The compatibility rule lets a narrower number be compared with, and written to, a <c>double</c> or a <c>float</c>
    /// column (an integer of up to 32 bits for a <c>double</c>, one of up to 16 bits for a <c>float</c>) because every such
    /// value is exactly representable. Here the four engines confirm it.
    /// </summary>
    [Collection(PostgreSqlCollection.Name)]
    public class NumericColumnsIntegrationTests
    {
        private readonly PostgreSqlFixture _fixture;

        public NumericColumnsIntegrationTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private async Task<int[]> IdsAsync(FilterDescriptor<NumericRow> filter)
        {
            if (await _fixture.Connection.GetByIdAsync<NumericRow>(1, cancellationToken: Ct) is null)
                await _fixture.Connection.InsertRangeAsync([new NumericRow { Id = 1, DoubleValue = 3, SingleValue = 2 }, new NumericRow { Id = 2, DoubleValue = 4, SingleValue = 5 }], cancellationToken: Ct);

            IReadOnlyList<NumericRow> rows = await _fixture.Connection.GetAllAsync(filter, cancellationToken: Ct);

            return [.. rows.Where(r => r.Id is 1 or 2).Select(r => r.Id).Order()];
        }

        [Fact]
        public async Task Equal_WithNarrowerNumbersOnADoubleColumn_MatchesTheRow()
        {
            Assert.Equal(new[] { 1 }, await IdsAsync(new("DoubleValue", 3)));
            Assert.Equal(new[] { 1 }, await IdsAsync(new("DoubleValue", (short)3)));
            Assert.Equal(new[] { 1 }, await IdsAsync(new("DoubleValue", (byte)3)));
            Assert.Equal(new[] { 2 }, await IdsAsync(new("DoubleValue", 3, ComparisonOperator.GreaterThan)));
        }

        [Fact]
        public async Task Equal_WithNarrowerNumbersOnAFloatColumn_MatchesTheRow()
        {
            Assert.Equal(new[] { 1 }, await IdsAsync(new("SingleValue", (short)2)));
            Assert.Equal(new[] { 2 }, await IdsAsync(new("SingleValue", (byte)5)));
        }

        [Fact]
        public async Task In_WithListsOfNarrowerNumbers_MatchesTheRows()
        {
            Assert.Equal(new[] { 1, 2 }, await IdsAsync(new("DoubleValue", new int[] { 3, 4 }, ComparisonOperator.In)));
            Assert.Equal(new[] { 1, 2 }, await IdsAsync(new("SingleValue", new short[] { 2, 5 }, ComparisonOperator.In)));
        }

        [Fact]
        public async Task UpdateAsync_WithNarrowerNumbers_WritesThemToDoubleAndFloatColumns()
        {
            await _fixture.Connection.InsertAsync(new NumericRow { Id = 3, DoubleValue = 0, SingleValue = 0 }, cancellationToken: Ct);

            await _fixture.Connection.UpdateAsync<NumericRow>(new { DoubleValue = 7, SingleValue = (short)8 }, r => r.Id == 3, cancellationToken: Ct);
            NumericRow? row = await _fixture.Connection.GetByIdAsync<NumericRow>(3, cancellationToken: Ct);

            Assert.Equal(7d, row?.DoubleValue);
            Assert.Equal(8f, row?.SingleValue);
        }

        [Fact]
        public void ANumberThatCouldChangeItsValue_IsStillRejected()
        {
            Assert.Throws<ArgumentException>(() => new FilterDescriptor<NumericRow>("SingleValue", 3));
            Assert.Throws<ArgumentException>(() => new FilterDescriptor<NumericRow>("DoubleValue", 3L));
            Assert.Throws<ArgumentException>(() => new FilterDescriptor<NumericRow>("DoubleValue", 1.1f));
        }

        [Fact]
        public async Task UpdateAsync_WithANumberThatCouldChangeItsValue_IsRejected()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.UpdateAsync<NumericRow>(new { SingleValue = 5 }, r => r.Id == 3, cancellationToken: Ct));
            await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Connection.UpdateAsync<NumericRow>(new { DoubleValue = 1.1f }, r => r.Id == 3, cancellationToken: Ct));
        }

        [Fact]
        public async Task AnIntegerKeyValue_OnAnEnumKey_FindsTheRow()
        {
            await _fixture.Connection.InsertAsync(new EnumKeyed { Id = TypeMatrixKind.None, Name = "zero" }, cancellationToken: Ct);

            EnumKeyed? byInt = await _fixture.Connection.GetByIdAsync<EnumKeyed>(0, cancellationToken: Ct);
            EnumKeyed? byLong = await _fixture.Connection.GetByIdAsync<EnumKeyed>(0L, cancellationToken: Ct);
            IReadOnlyList<EnumKeyed> range = await _fixture.Connection.GetByIdRangeAsync<EnumKeyed>(new int[] { 0 }, cancellationToken: Ct);
            int deleted = await _fixture.Connection.DeleteRangeAsync<EnumKeyed>(new long[] { 0 }, cancellationToken: Ct);

            Assert.Equal("zero", byInt?.Name);
            Assert.Equal("zero", byLong?.Name);
            Assert.Equal(["zero"], range.Select(r => r.Name));
            Assert.Equal(1, deleted);
        }
    }
}