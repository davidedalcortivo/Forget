using Forget.MySql.Extensions;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// <c>Sum</c> and <c>Avg</c> take a selector typed <c>decimal?</c>, so for a non-nullable <c>int</c> or <c>long</c>
    /// property the compiler converts the value twice (to <c>decimal</c>, then to <c>decimal?</c>), and so does the cast a
    /// <c>double</c> needs (<c>x => (decimal?)x.Ratio</c>: a <c>double</c> has no implicit conversion to <c>decimal</c>).
    /// Forget unwrapped one conversion only and refused the selector, the most common case, with an
    /// <see cref="ArgumentException"/>.
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class AggregateSelectorIntegrationTests
    {
        private readonly MySqlFixture _fixture;

        public AggregateSelectorIntegrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task SumAsync_WithASelectorOnANonNullableIntLongOrDouble_ReturnsTheSum()
        {
            Assert.Equal(60m, await _fixture.Connection.SumAsync<AggregateRow>(x => x.Quantity, cancellationToken: Ct));
            Assert.Equal(600m, await _fixture.Connection.SumAsync<AggregateRow>(x => x.Total, cancellationToken: Ct));
            Assert.Equal(4.5m, await _fixture.Connection.SumAsync<AggregateRow>(x => (decimal?)x.Ratio, cancellationToken: Ct));
        }

        [Fact]
        public async Task AvgAsync_WithASelectorOnANonNullableIntLongOrDouble_ReturnsTheAverage()
        {
            Assert.Equal(20m, await _fixture.Connection.AvgAsync<AggregateRow>(x => x.Quantity, cancellationToken: Ct));
            Assert.Equal(200m, await _fixture.Connection.AvgAsync<AggregateRow>(x => x.Total, cancellationToken: Ct));
            Assert.Equal(1.5m, await _fixture.Connection.AvgAsync<AggregateRow>(x => (decimal?)x.Ratio, cancellationToken: Ct));
        }
    }
}
