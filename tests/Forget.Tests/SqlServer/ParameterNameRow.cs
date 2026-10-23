using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// Two properties whose names differ only by trailing digits (<c>Address</c> and <c>Address2</c>), the case that made
    /// two multi-row parameters share a name (<c>Address</c> in row 20 and <c>Address2</c> in row 0).
    /// </summary>
    [Table("ParameterNameRow", Schema = "dbo")]
    internal sealed class ParameterNameRow
    {
        public int Id { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Address2 { get; set; } = string.Empty;
    }
}
