namespace Forget.Tests.Oracle
{
    /// <summary>
    /// No schema in <c>[Table]</c>: the database resolves the table from the session, as it resolves hand-written SQL.
    /// The table is in <c>"dbo"</c> and in <c>"tenant_b"</c>, with different rows.
    /// </summary>
    internal sealed class TenantRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    /// <summary>No schema in <c>[Table]</c>, and the table is only in <c>"tenant_b"</c>.</summary>
    internal sealed class TenantOnlyRow
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Amount { get; set; }
    }
}
