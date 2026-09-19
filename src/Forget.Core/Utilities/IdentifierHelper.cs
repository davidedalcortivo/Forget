using System.Text.RegularExpressions;


namespace Forget.Core.Utilities
{
    internal static partial class IdentifierHelper
    {
        public const string CharsetPattern = "^[A-Za-z][A-Za-z0-9_]*$";

        [GeneratedRegex(CharsetPattern, RegexOptions.Compiled)]
        public static partial Regex CharsetRegex();
    }
}
