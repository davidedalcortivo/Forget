using Forget.Core.Utilities;


namespace Forget.Tests.Core
{
    /// <summary>
    /// A selector reaches its property through the conversions the compiler adds to fit the selector's type: none for
    /// <c>Min</c> and <c>Max</c> (typed as the property), one for <c>Count</c>, a filter or a sort (<c>object</c>) and for
    /// <c>Sum</c> and <c>Avg</c> (<c>decimal?</c>) on an <c>int?</c> or a <c>decimal</c>, two for <c>Sum</c> and <c>Avg</c>
    /// on a non-nullable <c>int</c> or <c>long</c>, and on a <c>double</c> with the cast it needs (<c>(decimal?)x.Ratio</c>):
    /// to <c>decimal</c>, then to <c>decimal?</c>. A conversion to any other type is the selector's own, and is refused like
    /// any other computed value.
    /// </summary>
    public class SelectorTests
    {
        private sealed class Row
        {
            public int Id { get; set; }
            public int Quantity { get; set; }
            public long Total { get; set; }
            public double Ratio { get; set; }
            public int? MaybeQuantity { get; set; }
            public decimal Price { get; set; }
        }

        [Fact]
        public void ADecimalSelector_OnANonNullableIntLongOrDouble_ReachesTheProperty()
        {
            Assert.Equal("Quantity", PropertyHelper.GetProperty<Row, decimal?>(x => x.Quantity).Name);
            Assert.Equal("Total", PropertyHelper.GetProperty<Row, decimal?>(x => x.Total).Name);
            Assert.Equal("Ratio", PropertyHelper.GetProperty<Row, decimal?>(x => (decimal?)x.Ratio).Name);
        }

        [Fact]
        public void ASelectorWithOneConversionOrNone_StillReachesTheProperty()
        {
            Assert.Equal("MaybeQuantity", PropertyHelper.GetProperty<Row, decimal?>(x => x.MaybeQuantity).Name);
            Assert.Equal("Price", PropertyHelper.GetProperty<Row, decimal?>(x => x.Price).Name);
            Assert.Equal("Quantity", PropertyHelper.GetProperty<Row, object?>(x => x.Quantity).Name);
            Assert.Equal("Total", PropertyHelper.GetProperty<Row, long>(x => x.Total).Name);
        }

        [Fact]
        public void ASelectorThatComputesOrNarrowsTheValue_IsStillRefused()
        {
            Assert.Throws<ArgumentException>(() => PropertyHelper.GetProperty<Row, decimal?>(x => x.Quantity + 1));
            Assert.Throws<ArgumentException>(() => PropertyHelper.GetProperty<Row, decimal?>(x => (decimal?)(byte)x.Total));
            Assert.Throws<ArgumentException>(() => PropertyHelper.GetProperty<Row, object?>(x => (object)(short)x.Quantity));
        }
    }
}
