using Forget.Tests.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// The shape of a reference table keyed by an enum. <c>GetByIdRange</c>/<c>DeleteRange</c> bind their ids as one
    /// array (<c>= ANY(@ids)</c> on PostgreSql), so the key's CLR type decides what array Npgsql is asked to write.
    /// </summary>
    [Table("EnumKeyed", Schema = "dbo")]
    internal sealed class EnumKeyed
    {
        public TypeMatrixKind Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>The same id-range paths with a <see cref="Guid"/> key, the most common non-integer key.</summary>
    [Table("GuidKeyed", Schema = "dbo")]
    internal sealed class GuidKeyed
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// A table keyed by a string. PostgreSQL compares strings exactly, so <c>'abc'</c> and <c>'ABC'</c> are two keys
    /// to the database as well as to .NET.
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