using System.ComponentModel.DataAnnotations.Schema;


namespace Forget.Tests.Core
{
    /// <summary>
    /// Shared entity used by the provider-agnostic Core tests (translator/cache tests that run against all 4
    /// dialect strategies but never touch a live connection). Each provider's own test folder has its own copy of
    /// this same shape (e.g. <c>Oracle.Widget</c>, <c>SqlServer.Widget</c>) for anything that actually executes
    /// against that provider — so a provider-specific type quirk (Oracle's <c>bool</c>/<c>NUMBER(1)</c> binding
    /// issue on pre-23c servers, a future SQLite copy skipping <c>Guid</c>, etc.) is a local change to that one
    /// provider's copy instead of a workaround leaking into every other provider's tests.
    /// </summary>
    [Table("Widget", Schema = "dbo")]
    internal sealed class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public int? Quantity { get; set; }
        public bool IsActive { get; set; }
        public decimal Price { get; set; }
    }
}
