using Forget.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// The shape the README's upsert story is about: a key the database generates, and a natural key
    /// (<see cref="Sku"/>, unique in the table) that identifies the row for <c>Upsert</c>.
    /// </summary>
    [Table("UpsertProduct", Schema = "dbo")]
    internal sealed class UpsertProduct
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [UpsertKey]
        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public int Stock { get; set; }
    }
}
