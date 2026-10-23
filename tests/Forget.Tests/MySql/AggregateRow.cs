using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// Non-nullable numeric properties of types other than <c>decimal</c>, which a <c>Sum</c> or <c>Avg</c> selector
    /// (typed <c>decimal?</c>) reaches through two conversions. The table holds three rows: 10, 20, 30; 100, 200, 300;
    /// 0.5, 1.5, 2.5.
    /// </summary>
    [Table("AggregateRow", Schema = "dbo")]
    internal sealed class AggregateRow
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public long Total { get; set; }
        public double Ratio { get; set; }
    }
}
