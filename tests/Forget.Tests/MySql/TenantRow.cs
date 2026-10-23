namespace Forget.Tests.MySql
{
    /// <summary>
    /// No schema in <c>[Table]</c>: the database resolves the table from the session, as it resolves hand-written SQL.
    /// The table is in <c>tenant_a</c> and in <c>tenant_b</c>, with different rows.
    /// </summary>
    internal sealed class TenantRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
    }
}
