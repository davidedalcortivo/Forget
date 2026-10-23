using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// A table, schema and columns whose names Oracle only accepts between double quotes: spaces, an accented letter,
    /// <c>#</c> and <c>$</c>, a leading digit or underscore, a backslash, a dot, and <c>@</c>, <c>:</c>, <c>?</c> and
    /// <c>$</c>, which the driver must not take for parameters. Oracle refuses a double quote in any name. The table also
    /// has a column the entity does not map, whose name holds a tab: the cast discovery reads every column of the table
    /// and returns their names as JSON, so a backslash or a control character has to be escaped there.
    /// </summary>
    [Table("Order Details", Schema = "Odd Schema#1")]
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
    }
}
