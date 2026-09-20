using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// A table keyed by a string. SQL Server compares strings with the database's collation, which is case-insensitive
    /// unless configured otherwise, so <c>'abc'</c> and <c>'ABC'</c> are the same key to the database and two different
    /// ones to .NET.
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
