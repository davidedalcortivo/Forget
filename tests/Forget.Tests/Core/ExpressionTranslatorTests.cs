using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Utilities;
using System.Linq.Expressions;


namespace Forget.Tests.Core
{
    /// <summary>
    /// Exercises <see cref="ExpressionTranslator{TEntity}"/> against every provider's real dialect strategy, so a
    /// translation bug specific to one dialect (quoting, parameter prefix, <c>IN</c>/<c>LIKE</c> syntax, ...) is
    /// caught here instead of only surfacing against a live database.
    /// </summary>
    public class ExpressionTranslatorTests
    {
        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void Equal_RendersIdentifierEqualsParameter(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name == "Widget";

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p0")})", sql);
            Assert.Equal("Widget", parameters!.Get<string>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void NotEqual_RendersInequality(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name != "Widget";

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.RenderIdentifier("Name")} <> {dialect.RenderParameter("p0")})", sql);
            Assert.Equal("Widget", parameters!.Get<string>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void GreaterThan_RendersComparisonOnNullableProperty(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Quantity > 5;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.RenderIdentifier("Quantity")} > {dialect.RenderParameter("p0")})", sql);
            Assert.Equal(5, parameters!.Get<int>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AndAlso_CombinesTwoComparisons(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name == "A" && w.Quantity > 1;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            string expected = $"(({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p0")}) AND " +
                               $"({dialect.RenderIdentifier("Quantity")} > {dialect.RenderParameter("p1")}))";
            Assert.Equal(expected, sql);
            Assert.Equal("A", parameters!.Get<string>("p0"));
            Assert.Equal(1, parameters!.Get<int>("p1"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void OrElse_CombinesTwoComparisons(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name == "A" || w.Name == "B";

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            string expected = $"(({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p0")}) OR " +
                               $"({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p1")}))";
            Assert.Equal(expected, sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void EqualNull_TranslatesToIsNullWithoutAddingParameter(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Nickname == null;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.IsNull(dialect.RenderIdentifier("Nickname"))})", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void NotEqualNull_TranslatesToIsNotNull(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Nickname != null;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.IsNotNull(dialect.RenderIdentifier("Nickname"))})", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void DirectBoolMember_TranslatesToIsTrue(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.IsActive;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.IsTrue(dialect.RenderIdentifier("IsActive"))})", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void NegatedBoolMember_TranslatesToNotIsTrue(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => !w.IsActive;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"NOT ({dialect.IsTrue(dialect.RenderIdentifier("IsActive"))})", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void BoolMemberAndComparison_CombinesIsTrueWithComparison(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.IsActive && w.Quantity > 1;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            string expected = $"(({dialect.IsTrue(dialect.RenderIdentifier("IsActive"))}) AND " +
                               $"({dialect.RenderIdentifier("Quantity")} > {dialect.RenderParameter("p0")}))";
            Assert.Equal(expected, sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void Contains_TranslatesToLikeWithEscapedWildcards(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name.Contains("wid");

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            string pattern = dialect.Concat("'%'", dialect.RenderParameter("p0"), "'%'");
            Assert.Equal($"({dialect.Like(dialect.RenderIdentifier("Name"), pattern)})", sql);
            Assert.Equal("wid", parameters!.Get<string>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void StartsWith_TranslatesToLikeWithTrailingWildcard(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name.StartsWith("wid");

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            string pattern = dialect.Concat(dialect.RenderParameter("p0"), "'%'");
            Assert.Equal($"({dialect.Like(dialect.RenderIdentifier("Name"), pattern)})", sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void EndsWith_TranslatesToLikeWithLeadingWildcard(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name.EndsWith("get");

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            string pattern = dialect.Concat("'%'", dialect.RenderParameter("p0"));
            Assert.Equal($"({dialect.Like(dialect.RenderIdentifier("Name"), pattern)})", sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ContainsIgnoreCase_LowersBothSides(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name.Contains("Wid", StringComparison.OrdinalIgnoreCase);

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            string col = dialect.ToLower(dialect.RenderIdentifier("Name"));
            string pattern = dialect.Concat("'%'", dialect.ToLower(dialect.RenderParameter("p0")), "'%'");
            Assert.Equal($"({dialect.Like(col, pattern)})", sql);
            Assert.Equal("Wid", parameters!.Get<string>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void StringEqualsIgnoreCase_ComparesLoweredSides(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name.Equals("widget", StringComparison.OrdinalIgnoreCase);

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            string left = dialect.ToLower(dialect.RenderIdentifier("Name"));
            string right = dialect.ToLower(dialect.RenderParameter("p0"));
            Assert.Equal($"({left} = {right})", sql);
            Assert.Equal("widget", parameters!.Get<string>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void StringCompareEqualToZero_TranslatesToEquality(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => string.Compare(w.Name, "abc") == 0;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p0")})", sql);
            Assert.Equal("abc", parameters!.Get<string>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void StringCompareLessThanZero_TranslatesToNullSafeComparison(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => string.Compare(w.Name, "abc") < 0;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            string a = dialect.RenderIdentifier("Name");
            string b = dialect.RenderParameter("p0");
            string expected = "(" +
                $"(({dialect.IsNull(a)}) AND ({dialect.IsNotNull(b)})) OR " +
                $"(({dialect.IsNotNull(a)}) AND ({dialect.IsNotNull(b)}) AND ({a} < {b}))" +
                ")";
            Assert.Equal(expected, sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ToStringCall_CastsUnderlyingColumn(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Price.ToString() == "10";

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.CastAsString(dialect.RenderIdentifier("Price"))} = {dialect.RenderParameter("p0")})", sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ToLowerCall_WrapsColumnInLower(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name.ToLower() == "widget";

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.ToLower(dialect.RenderIdentifier("Name"))} = {dialect.RenderParameter("p0")})", sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ToUpperCall_WrapsColumnInUpper(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name.ToUpper() == "WIDGET";

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.ToUpper(dialect.RenderIdentifier("Name"))} = {dialect.RenderParameter("p0")})", sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void CollectionContains_TranslatesToIn(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => new[] { 1, 2, 3 }.Contains(w.Id);

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.In(dialect.RenderIdentifier("Id"), dialect.RenderParameter("p0"), false)})", sql);
            Assert.Equal([1, 2, 3], parameters!.Get<int[]>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void CollectionContainsWithNull_CombinesIsNullAndIn(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            string?[] names = ["a", null, "b"];
            Expression<Func<Widget, bool>> predicate = w => names.Contains(w.Name);

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            string col = dialect.RenderIdentifier("Name");
            string expected = $"(({dialect.IsNull(col)}) OR ({dialect.In(col, dialect.RenderParameter("p0"), false)}))";
            Assert.Equal(expected, sql);
            Assert.Equal(["a", "b"], parameters!.Get<object[]>("p0"));
        }

        private sealed class MixedTypeEntity
        {
            public int Id { get; set; }
            public object? Tag { get; set; }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void CollectionContainsWithHeterogeneousRuntimeTypes_FallsBackToInInsteadOfThrowing(string dialectName, object dialectObject)
        {
            // Mirrors FilterNodeTranslatorTests' equivalent case: Tag's declared type is `object`, so a collection
            // mixing an int and a string is legal C# but shares no single concrete runtime type. Building a
            // strongly-typed array from the first element would throw InvalidCastException on the rest; the
            // translator must fall back to a plain object[] parameter and tell the dialect via `mixed` so
            // PostgreSql renders `IN` (relying on Dapper's own list expansion) instead of `= ANY(...)`.
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            object[] tags = [1, "two"];
            Expression<Func<MixedTypeEntity, bool>> predicate = w => tags.Contains(w.Tag);

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<MixedTypeEntity>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.In(dialect.RenderIdentifier("Tag"), dialect.RenderParameter("p0"), true)})", sql);
            Assert.Equal([1, "two"], parameters!.Get<object[]>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void CollectionContainsWithEmptyCollection_IsAlwaysFalse(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => new int[] { }.Contains(w.Id);

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal("(1 = 0)", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void CollectionContainsWithNullSource_IsAlwaysFalse(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            int[]? ids = null;
            Expression<Func<Widget, bool>> predicate = w => ids!.Contains(w.Id);

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal("(1 = 0)", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void UnsupportedStringMethod_ThrowsNotSupportedException(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name.Trim() == "a";

            Assert.Throws<NotSupportedException>(() => ExpressionTranslator<Widget>.Translate(dialect, predicate));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void UnsupportedBinaryOperator_ThrowsNotSupportedException(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => (w.Id + 1) == 2;

            Assert.Throws<NotSupportedException>(() => ExpressionTranslator<Widget>.Translate(dialect, predicate));
        }
    }
}
