using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// Maps two of the six columns of <c>"PartialRow"</c>. Besides <c>Legacy</c> (NOT NULL, with a default) and
    /// <c>Audit</c>, the table has two columns whose names Forget could never map (a backslash and a <c>$</c>): the
    /// discovery must neither trip on them nor let them reach its JSON output.
    /// </summary>
    [Table("PartialRow", Schema = "dbo")]
    internal sealed class PartialRow
    {
        public int Id { get; set; }
        public int Qty { get; set; }
    }
}
