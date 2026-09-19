using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// Maps two of the four columns of <c>dbo.PartialRow</c>: <c>Legacy</c> (NOT NULL, with a default) and
    /// <c>Audit</c> exist in the table but not here, the way audit and legacy columns usually do.
    /// </summary>
    [Table("PartialRow", Schema = "dbo")]
    internal sealed class PartialRow
    {
        public int Id { get; set; }
        public int Qty { get; set; }
    }
}
