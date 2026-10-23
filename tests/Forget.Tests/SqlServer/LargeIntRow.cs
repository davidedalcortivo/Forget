using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// An <c>INT</c> column whose values add up to more than <see cref="int.MaxValue"/>: 2,000,000,000 twice with
    /// <c>Kind</c> 1 and 5 with <c>Kind</c> 2.
    /// </summary>
    [Table("LargeIntRow", Schema = "dbo")]
    internal sealed class LargeIntRow
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public int Kind { get; set; }
    }
}
