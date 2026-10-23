using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// A table, schema and columns whose names SQL Server only accepts between brackets: spaces, an accented letter,
    /// <c>#</c> and <c>$</c>, a leading digit or underscore, a backslash, a dot, the closing bracket itself, and
    /// <c>@</c>, <c>:</c>, <c>?</c> and <c>$</c>, which the driver must not take for parameters.
    /// </summary>
    [Table("Order] Details", Schema = "Odd Schema#1")]
    internal sealed class OddNameRow
    {
        public int Id { get; set; }

        [Column("Line Name")]
        public string LineName { get; set; } = string.Empty;

        [Column("Città")]
        public string City { get; set; } = string.Empty;

        [Column("Code#$")]
        public int Code { get; set; }

        [Column("2nd-Value")]
        public int SecondValue { get; set; }

        [Column("_Flag")]
        public int Flag { get; set; }

        [Column("Back\\slash")]
        public string Backslash { get; set; } = string.Empty;

        [Column("Email@Work:Port?Cost$")]
        public string Symbols { get; set; } = string.Empty;

        [Column("Dot.Name")]
        public string DotName { get; set; } = string.Empty;

        [Column("Bracket]Name")]
        public string Quoted { get; set; } = string.Empty;
    }
}
