using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// A table keyed by a string. MySQL compares strings with the column's collation, which is case-insensitive by
    /// default, so <c>'abc'</c> and <c>'ABC'</c> are the same key to the database and two different ones to .NET.
    /// </summary>
    [Table("StringKeyed", Schema = "dbo")]
    internal sealed class StringKeyed
    {
        [Key]
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>A table keyed by a <c>byte[]</c>, whose .NET equality is by reference and not by content.</summary>
    [Table("BinaryKeyed", Schema = "dbo")]
    internal sealed class BinaryKeyed
    {
        public byte[] Id { get; set; } = [];
        public string Name { get; set; } = string.Empty;
    }
}
