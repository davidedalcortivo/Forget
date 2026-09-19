using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// One column per kind of Oracle type the multi-row statement has to cast (see
    /// <see cref="CastDiscoveryIntegrationTests"/>). Every type here binds with ODP.NET and Dapper as they are, so
    /// nothing in these tests depends on a type handler.
    /// </summary>
    [Table("CastRow", Schema = "dbo")]
    internal sealed class CastRow
    {
        public int Id { get; set; }

        /// <summary><c>NUMBER</c> with no precision or scale: holds any decimal.</summary>
        public decimal? Plain { get; set; }

        /// <summary><c>NUMBER(*,2)</c>: scale only, no precision.</summary>
        public decimal? Scaled { get; set; }

        /// <summary><c>NUMBER(18,4)</c>.</summary>
        public decimal? Exact { get; set; }

        /// <summary><c>BINARY_DOUBLE</c>.</summary>
        public double? Ratio { get; set; }

        /// <summary><c>FLOAT</c>.</summary>
        public double? Approx { get; set; }

        /// <summary><c>CLOB</c>.</summary>
        public string? Body { get; set; }

        /// <summary><c>BLOB</c>.</summary>
        public byte[]? Payload { get; set; }

        /// <summary><c>TIMESTAMP(6)</c>.</summary>
        public DateTime? Moment { get; set; }

        /// <summary><c>DATE</c>.</summary>
        public DateTime? Day { get; set; }

        /// <summary><c>CHAR(5)</c>.</summary>
        public string? Code { get; set; }

        /// <summary><c>CHAR(5 CHAR)</c>: the length counts characters, not bytes.</summary>
        public string? CharSemantic { get; set; }

        /// <summary><c>NCHAR(5)</c>.</summary>
        public string? National { get; set; }

        /// <summary><c>VARCHAR2(50)</c>.</summary>
        public string? Label { get; set; }

        /// <summary><c>VARCHAR2(50 CHAR)</c>.</summary>
        public string? LabelChars { get; set; }

        /// <summary><c>NVARCHAR2(20)</c>.</summary>
        public string? Unicode { get; set; }

        /// <summary><c>RAW(16)</c>.</summary>
        public byte[]? Token { get; set; }
    }
}
