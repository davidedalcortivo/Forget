using System.Data.Common;


namespace Forget.Core.Abstractions.Strategies
{
    internal interface ISqlDialectStrategy
    {
        string NullValue { get; }
        string Terminator { get; }
        string Placeholder { get; }
        bool BindsListsAsArrays { get; }

        string RenderIdentifier(string name);
        string RenderTable(string? schemaName, string tableName);
        string RenderParameter(string name);
        string Concat(params string[] parts);
        string ToLower(string sql);
        string ToUpper(string sql);
        string Like(string column, string pattern);
        string EscapeLike(string value);
        string In(string identifier, string parameter, bool mixed);
        (string, string) In(string identifier, bool mixed);
        string IsNull(string column);
        string IsNotNull(string column);
        string IsTrue(string column);
        string Pagination(string skipParameter, string takeParameter);
        string CastAsString(string sql);
        string GetConnectionId(DbConnection connection);
    }
}
