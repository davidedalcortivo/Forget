using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.MySql
{
    /// <summary>A table with one <c>double</c> and one <c>float</c> column, to compare and write them with narrower numbers.</summary>
    [Table("NumericRow", Schema = "dbo")]
    internal sealed class NumericRow
    {
        public int Id { get; set; }
        public double DoubleValue { get; set; }
        public float SingleValue { get; set; }
    }
}