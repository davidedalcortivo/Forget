using Forget.Core.Models;
using Forget.SqlServer.Extensions;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// SQL Server adds up an <c>INT</c> column in an <c>INT</c>, for <c>SUM</c> and for the sum behind <c>AVG</c>, and
    /// fails with an arithmetic overflow past <see cref="int.MaxValue"/>. Forget reads the column type from the catalog and
    /// casts an <c>INT</c>, <c>SMALLINT</c> or <c>TINYINT</c> column to <c>BIGINT</c>, in every form of <c>Sum</c> and
    /// <c>Avg</c>.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public class LargeIntAggregateIntegrationTests
    {
        private readonly SqlServerFixture _fixture;

        public LargeIntAggregateIntegrationTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static FilterDescriptor<LargeIntRow> KindOne => new(x => x.Kind, 1);

        [Fact]
        public async Task SumAsync_OfAnIntColumnPastIntMaxValue_DoesNotOverflow()
        {
            Assert.Equal(4_000_000_005m, await _fixture.Connection.SumAsync<LargeIntRow>(x => x.Quantity, cancellationToken: Ct));
            Assert.Equal(4_000_000_000m, await _fixture.Connection.SumAsync<LargeIntRow>(x => x.Quantity, x => x.Kind == 1, cancellationToken: Ct));
            Assert.Equal(4_000_000_000m, await _fixture.Connection.SumAsync<LargeIntRow>(x => x.Quantity, KindOne, cancellationToken: Ct));
            Assert.Equal(4_000_000_005m, await _fixture.Connection.SumAsync<LargeIntRow>(nameof(LargeIntRow.Quantity), cancellationToken: Ct));
            Assert.Equal(4_000_000_000m, await _fixture.Connection.SumAsync<LargeIntRow>(nameof(LargeIntRow.Quantity), x => x.Kind == 1, cancellationToken: Ct));
            Assert.Equal(4_000_000_000m, await _fixture.Connection.SumAsync<LargeIntRow>(nameof(LargeIntRow.Quantity), KindOne, cancellationToken: Ct));
        }

        [Fact]
        public async Task AvgAsync_OfAnIntColumnPastIntMaxValue_DoesNotOverflow()
        {
            Assert.Equal(1_333_333_335m, await _fixture.Connection.AvgAsync<LargeIntRow>(x => x.Quantity, cancellationToken: Ct));
            Assert.Equal(2_000_000_000m, await _fixture.Connection.AvgAsync<LargeIntRow>(x => x.Quantity, x => x.Kind == 1, cancellationToken: Ct));
            Assert.Equal(2_000_000_000m, await _fixture.Connection.AvgAsync<LargeIntRow>(x => x.Quantity, KindOne, cancellationToken: Ct));
            Assert.Equal(1_333_333_335m, await _fixture.Connection.AvgAsync<LargeIntRow>(nameof(LargeIntRow.Quantity), cancellationToken: Ct));
            Assert.Equal(2_000_000_000m, await _fixture.Connection.AvgAsync<LargeIntRow>(nameof(LargeIntRow.Quantity), x => x.Kind == 1, cancellationToken: Ct));
            Assert.Equal(2_000_000_000m, await _fixture.Connection.AvgAsync<LargeIntRow>(nameof(LargeIntRow.Quantity), KindOne, cancellationToken: Ct));
        }
    }
}
