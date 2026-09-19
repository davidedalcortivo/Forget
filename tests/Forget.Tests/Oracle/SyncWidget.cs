using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// A second entity type over the same <c>dbo.Widget</c> table as <see cref="Widget"/>. The database caches Forget
    /// keeps are per entity type, so this one starts with nothing loaded: the operations that need the table's column
    /// metadata load it themselves, and here they do so through the synchronous path.
    /// </summary>
    [Table("Widget", Schema = "dbo")]
    internal sealed class SyncWidget
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public int? Quantity { get; set; }
        public int IsActive { get; set; }
        public decimal Price { get; set; }
    }
}
