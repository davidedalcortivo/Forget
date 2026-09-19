using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// Maps two of the four columns of <c>dbo."PartialRow"</c>. <c>Legacy</c> (NOT NULL, with a default) sits between
    /// the mapped columns and <c>Audit</c> after them, so the unmapped columns are not conveniently at the end.
    /// </summary>
    [Table("PartialRow", Schema = "dbo")]
    internal sealed class PartialRow
    {
        public int Id { get; set; }
        public int Qty { get; set; }
    }
}
