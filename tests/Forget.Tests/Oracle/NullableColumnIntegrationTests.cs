using Forget.Oracle.Extensions;
using System.Linq.Expressions;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// <c>HasValue</c> and <c>Value</c> on a nullable column translate to <c>IS NULL</c>, <c>IS NOT NULL</c> and the column
    /// itself; the comparison of <c>HasValue</c> with a boolean would be invalid SQL on some engines if it were rendered as
    /// a comparison of two conditions, so each engine runs it.
    /// </summary>
    [Collection(OracleCollection.Name)]
    public class NullableColumnIntegrationTests
    {
        private readonly OracleFixture _fixture;

        public NullableColumnIntegrationTests(OracleFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static Widget NewWidget(int id, int? quantity) => new() { Id = id, Name = "Nullable", Quantity = quantity, IsActive = 1, Price = 1m };

        private async Task<int[]> IdsAsync(Expression<Func<Widget, bool>> predicate)
        {
            if (await _fixture.Connection.GetByIdAsync<Widget>(900, cancellationToken: Ct) is null)
                await _fixture.Connection.InsertRangeAsync([NewWidget(900, null), NewWidget(901, 3), NewWidget(902, 8), NewWidget(903, null)], cancellationToken: Ct);

            IReadOnlyList<Widget> rows = await _fixture.Connection.GetAllAsync(predicate, cancellationToken: Ct);

            return [.. rows.Select(w => w.Id).Where(id => id is >= 900 and <= 903).Order()];
        }

        [Fact]
        public async Task HasValue_MatchesTheRowsWithAValue()
        {
            Assert.Equal(new[] { 901, 902 }, await IdsAsync(w => w.Quantity.HasValue));
            Assert.Equal(new[] { 900, 903 }, await IdsAsync(w => !w.Quantity.HasValue));
        }

        [Fact]
        public async Task HasValueComparedWithABoolean_MatchesTheSameRows()
        {
            bool no = false;

            Assert.Equal(new[] { 901, 902 }, await IdsAsync(w => w.Quantity.HasValue == true));
            Assert.Equal(new[] { 900, 903 }, await IdsAsync(w => w.Quantity.HasValue == false));
            Assert.Equal(new[] { 900, 903 }, await IdsAsync(w => w.Quantity.HasValue == no));
            Assert.Equal(new[] { 900, 903 }, await IdsAsync(w => w.Quantity.HasValue != true));
        }

        [Fact]
        public async Task ValueAndHasValue_CombineInOneFilter()
        {
            Assert.Equal(new[] { 902 }, await IdsAsync(w => w.Quantity.HasValue && w.Quantity.Value > 5));
            Assert.Equal(new[] { 900, 902, 903 }, await IdsAsync(w => !w.Quantity.HasValue || w.Quantity.Value > 5));
        }
    }
}
