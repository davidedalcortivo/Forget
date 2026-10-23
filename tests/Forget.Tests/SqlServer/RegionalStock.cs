using Forget.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// A natural key of two columns: a row is identified by <see cref="Sku"/> and <see cref="Region"/> together, so the
    /// same SKU can have one row per region.
    /// </summary>
    [Table("RegionalStock", Schema = "dbo")]
    internal sealed class RegionalStock
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [UpsertKey]
        public string Sku { get; set; } = string.Empty;

        [UpsertKey]
        public string Region { get; set; } = string.Empty;

        public int Stock { get; set; }
    }
}
