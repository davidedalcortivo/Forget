using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// A byte column and a column holding an enum whose underlying type is byte. A list of either used to be bound as a
    /// <c>byte[]</c>, which every driver binds as one binary value.
    /// </summary>
    [Table("ByteListRow", Schema = "dbo")]
    internal sealed class ByteListRow
    {
        public int Id { get; set; }
        public byte Code { get; set; }
        public ByteStatus Status { get; set; }
    }

    internal enum ByteStatus : byte
    {
        Active = 1,
        Suspended = 2,
        Closed = 3
    }

    /// <summary>
    /// An entity keyed by an enum whose underlying type is byte. Only its commands are built, so it needs no table.
    /// </summary>
    [Table("ByteStatusKeyRow", Schema = "dbo")]
    internal sealed class ByteStatusKeyRow
    {
        [Key]
        public ByteStatus Id { get; set; }
    }
}
