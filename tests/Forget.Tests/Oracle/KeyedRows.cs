using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// A table keyed by a string. Oracle compares strings exactly (binary collation) unless the session says otherwise,
    /// so <c>'abc'</c> and <c>'ABC'</c> are two keys to the database as well as to .NET.
    /// </summary>
    [Table("StringKeyed", Schema = "dbo")]
    internal sealed class StringKeyed
    {
        [Key]
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>A table keyed by a <c>byte[]</c> (<c>RAW</c>), whose .NET equality is by reference and not by content.</summary>
    [Table("BinaryKeyed", Schema = "dbo")]
    internal sealed class BinaryKeyed
    {
        public byte[] Id { get; set; } = [];
        public string Name { get; set; } = string.Empty;
    }
}