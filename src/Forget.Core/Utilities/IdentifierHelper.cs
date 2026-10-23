using System.Text.RegularExpressions;


namespace Forget.Core.Utilities
{
    internal static partial class IdentifierHelper
    {
        public static void EnsureValid(string description, string name)
        {
            if (name.Contains('{') || name.Contains('}'))
                throw new InvalidOperationException($"The {description} '{name}' contains a curly brace, which Forget's SQL templates and Dapper use as markers.");

            Match match = NotIdentifierRegex().Match(name);

            if (match.Success)
                throw new InvalidOperationException($"The {description} '{name}' contains '{match.Value}', which Dapper would take for a parameter and rewrite.");
        }

        [GeneratedRegex(@"[@:?$]\w*_\d+_(?!\w)|\?[\p{L}_][\p{L}\p{N}_]*\?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
        public static partial Regex NotIdentifierRegex();
    }
}
