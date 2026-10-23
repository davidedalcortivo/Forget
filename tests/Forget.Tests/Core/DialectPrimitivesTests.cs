using Forget.Core.Abstractions.Strategies;


namespace Forget.Tests.Core
{
    /// <summary>
    /// Locks the literal SQL syntax each provider's dialect strategy renders. These are the primitives every other
    /// translator/builder test composes with, so a regression here would otherwise surface as confusing failures
    /// everywhere else instead of here.
    /// </summary>
    public class DialectPrimitivesTests
    {
        [Fact]
        public void SqlServer_UsesBracketIdentifiersAndAtParameters()
        {
            Forget.SqlServer.Strategies.SqlDialectStrategy dialect = Forget.SqlServer.Strategies.SqlDialectStrategy.Instance;

            Assert.Equal("[Name]", dialect.RenderIdentifier("Name"));
            Assert.Equal("[a]]b]", dialect.RenderIdentifier("a]b"));
            Assert.Equal("@p_0", dialect.RenderParameter("p_0"));
            Assert.Equal("a + b", dialect.Concat("a", "b"));
            Assert.Equal("[Id] IN @p_0", dialect.In("[Id]", "@p_0", false));
            Assert.Equal("[Id] IN @p_0", dialect.In("[Id]", "@p_0", true));
            Assert.Equal("[Active] = 1", dialect.IsTrue("[Active]"));
            Assert.Equal("CAST(x AS NVARCHAR(MAX))", dialect.CastAsString("x"));
        }

        [Fact]
        public void MySql_UsesBacktickIdentifiersAndConcatFunction()
        {
            Forget.MySql.Strategies.SqlDialectStrategy dialect = Forget.MySql.Strategies.SqlDialectStrategy.Instance;

            Assert.Equal("`Name`", dialect.RenderIdentifier("Name"));
            Assert.Equal("`a``b`", dialect.RenderIdentifier("a`b"));
            Assert.Equal("@p_0", dialect.RenderParameter("p_0"));
            Assert.Equal("CONCAT(a, b)", dialect.Concat("a", "b"));
            Assert.Equal("`Id` IN @p_0", dialect.In("`Id`", "@p_0", false));
            Assert.Equal("`Id` IN @p_0", dialect.In("`Id`", "@p_0", true));
            Assert.Equal("`Active` = 1", dialect.IsTrue("`Active`"));
            Assert.Equal("CAST(x AS CHAR)", dialect.CastAsString("x"));
        }

        [Fact]
        public void PostgreSql_UsesAnyForInAndTrueForBooleans()
        {
            Forget.PostgreSql.Strategies.SqlDialectStrategy dialect = Forget.PostgreSql.Strategies.SqlDialectStrategy.Instance;

            Assert.Equal("\"Name\"", dialect.RenderIdentifier("Name"));
            Assert.Equal("\"a\"\"b\"", dialect.RenderIdentifier("a\"b"));
            Assert.Equal("@p_0", dialect.RenderParameter("p_0"));
            Assert.Equal("a || b", dialect.Concat("a", "b"));
            Assert.Equal("\"Id\" = ANY(@p_0)", dialect.In("\"Id\"", "@p_0", false));
            Assert.Equal("\"Id\" IN @p_0", dialect.In("\"Id\"", "@p_0", true));
            Assert.Equal("\"Active\" = TRUE", dialect.IsTrue("\"Active\""));
            Assert.Equal("x::text", dialect.CastAsString("x"));
        }

        [Fact]
        public void Oracle_UsesColonParametersAndToCharCast()
        {
            Forget.Oracle.Strategies.SqlDialectStrategy dialect = Forget.Oracle.Strategies.SqlDialectStrategy.Instance;

            Assert.Equal("\"Name\"", dialect.RenderIdentifier("Name"));
            Assert.Equal("\"a\"\"b\"", dialect.RenderIdentifier("a\"b"));
            Assert.Equal(":p_0", dialect.RenderParameter("p_0"));
            Assert.Equal("a || b", dialect.Concat("a", "b"));
            Assert.Equal("\"Id\" IN :p_0", dialect.In("\"Id\"", ":p_0", false));
            Assert.Equal("\"Id\" IN :p_0", dialect.In("\"Id\"", ":p_0", true));
            Assert.Equal("\"Active\" = 1", dialect.IsTrue("\"Active\""));
            Assert.Equal("TO_CHAR(x)", dialect.CastAsString("x"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void EscapeLike_EscapesWildcardsButLeavesQuotesAlone(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;

            // The quote is intentionally left untouched: EscapeLike's result is always bound as a query parameter,
            // never inlined as SQL text, so it needs no SQL-string-literal escaping - only the LIKE
            // metacharacters (\, %, _) need protecting from being interpreted as wildcards.
            Assert.Equal("5\\%\\_'", dialect.EscapeLike("5%_'"));
            Assert.Equal("hello", dialect.EscapeLike("hello"));
            Assert.Equal(string.Empty, dialect.EscapeLike(string.Empty));
        }

        [Fact]
        public void MySql_Like_DoublesTheEscapeBackslashForItsOwnStringLiteralParsing()
        {
            Forget.MySql.Strategies.SqlDialectStrategy dialect = Forget.MySql.Strategies.SqlDialectStrategy.Instance;

            // MySQL applies C-style backslash escaping inside string literals by default, so the ESCAPE clause's
            // literal must contain two backslash characters for MySQL's own parser to collapse them into the one
            // literal backslash actually meant as the escape character - unlike the other three providers, whose
            // string literals don't treat backslash specially.
            Assert.Equal("col LIKE pat ESCAPE '\\\\'", dialect.Like("col", "pat"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ToLowerAndToUpper_WrapInStandardFunctions(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;

            Assert.Equal("LOWER(x)", dialect.ToLower("x"));
            Assert.Equal("UPPER(x)", dialect.ToUpper("x"));
        }

        [Theory]
        [InlineData("MySql")]
        [InlineData("PostgreSql")]
        public void Pagination_MySqlAndPostgreSql_UseLimitOffset(string dialectName)
        {
            ISqlDialectStrategy dialect = dialectName == "MySql"
                ? Forget.MySql.Strategies.SqlDialectStrategy.Instance
                : Forget.PostgreSql.Strategies.SqlDialectStrategy.Instance;

            string expected = string.Join(Environment.NewLine,
                "",
                "LIMIT",
                "    <<TAKE>>",
                "OFFSET",
                "    <<SKIP>>");

            Assert.Equal(expected, dialect.Pagination("<<SKIP>>", "<<TAKE>>"));
        }

        [Theory]
        [InlineData("Oracle")]
        [InlineData("SqlServer")]
        public void Pagination_OracleAndSqlServer_UseOffsetFetchNext(string dialectName)
        {
            ISqlDialectStrategy dialect = dialectName == "Oracle"
                ? Forget.Oracle.Strategies.SqlDialectStrategy.Instance
                : Forget.SqlServer.Strategies.SqlDialectStrategy.Instance;

            string expected = string.Join(Environment.NewLine,
                "",
                "OFFSET",
                "    <<SKIP>> ROWS",
                "FETCH NEXT",
                "    <<TAKE>> ROWS ONLY");

            Assert.Equal(expected, dialect.Pagination("<<SKIP>>", "<<TAKE>>"));
        }
    }
}
