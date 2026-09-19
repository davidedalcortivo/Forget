using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Models;
using Forget.Core.Utilities;


namespace Forget.Tests.Core
{
    /// <summary>
    /// Exercises <see cref="FilterNodeTranslator{TEntity}"/> — the <see cref="FilterDescriptor{TEntity}"/>/
    /// <see cref="FilterGroup{TEntity}"/> tree API — against every provider's real dialect strategy. Several cases
    /// mirror the equivalent <see cref="ExpressionTranslatorTests"/> case, to lock the two APIs producing the same
    /// shape of SQL for equivalent semantics.
    /// </summary>
    public class FilterNodeTranslatorTests
    {
        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void Equal_RendersIdentifierEqualsParameter(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Name, "Widget");

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            Assert.Equal($"({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p0")})", sql);
            Assert.Equal("Widget", parameters!.Get<string>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void EqualNull_TranslatesToIsNullWithoutAddingParameter(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Nickname, null);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            Assert.Equal($"({dialect.IsNull(dialect.RenderIdentifier("Nickname"))})", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void NotEqualNull_TranslatesToIsNotNull(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Nickname, null, ComparisonOperator.NotEqual);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            Assert.Equal($"({dialect.IsNotNull(dialect.RenderIdentifier("Nickname"))})", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void GreaterThan_RendersComparison(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Quantity, 1, ComparisonOperator.GreaterThan);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            Assert.Equal($"({dialect.RenderIdentifier("Quantity")} > {dialect.RenderParameter("p0")})", sql);
            Assert.Equal(1, parameters!.Get<int>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void Not_NegatesTheCondition(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.IsActive, true, ComparisonOperator.Equal, not: true);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            Assert.Equal($"NOT ({dialect.RenderIdentifier("IsActive")} = {dialect.RenderParameter("p0")})", sql);
            Assert.True(parameters!.Get<bool>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void In_WithValuesOnly_TranslatesToIn(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Id, new[] { 1, 2, 3 }, ComparisonOperator.In);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            Assert.Equal($"({dialect.In(dialect.RenderIdentifier("Id"), dialect.RenderParameter("p0"), false)})", sql);
            Assert.Equal([1, 2, 3], parameters!.Get<int[]>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void In_WithNullAndValues_CombinesIsNullAndIn(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Nickname, new[] { "a", null, "b" }, ComparisonOperator.In);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            string col = dialect.RenderIdentifier("Nickname");
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
        public void In_WithHeterogeneousRuntimeTypes_FallsBackToObjectArrayInsteadOfThrowing(string dialectName, object dialectObject)
        {
            // Tag's declared type is `object`, so EnsureValue only requires each element to be assignable to
            // `object` - it does not require them to share one concrete runtime type. Building a strongly-typed
            // array from the first element (an int) would throw InvalidCastException on the string that follows;
            // the translator must recognize the mix, keep a plain object[] parameter, and tell the dialect via
            // the `mixed` flag so PostgreSql falls back to `IN` (relying on Dapper's own list expansion into
            // independently-typed parameters) instead of `= ANY(...)`, which cannot represent a mixed-type array.
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<MixedTypeEntity> filter = new(w => w.Tag, new object[] { 1, "two" }, ComparisonOperator.In);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<MixedTypeEntity>.Translate(dialect, filter);

            Assert.Equal($"({dialect.In(dialect.RenderIdentifier("Tag"), dialect.RenderParameter("p0"), true)})", sql);
            Assert.Equal([1, "two"], parameters!.Get<object[]>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void In_WithEmptyCollection_IsAlwaysFalse(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Id, Array.Empty<int>(), ComparisonOperator.In);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            Assert.Equal("(1 = 0)", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void Contains_TranslatesToLikeWithEscapedWildcards(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Name, "wid", ComparisonOperator.Contains);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            string pattern = dialect.Concat("'%'", dialect.RenderParameter("p0"), "'%'");
            Assert.Equal($"({dialect.Like(dialect.RenderIdentifier("Name"), pattern)})", sql);
            Assert.Equal("wid", parameters!.Get<string>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ContainsIgnoreCase_LowersBothSides(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Name, "Wid", ComparisonOperator.Contains, ignoreCase: true);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            string col = dialect.ToLower(dialect.RenderIdentifier("Name"));
            string pattern = dialect.Concat("'%'", dialect.ToLower(dialect.RenderParameter("p0")), "'%'");
            Assert.Equal($"({dialect.Like(col, pattern)})", sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void StartsWith_TranslatesToLikeWithTrailingWildcard(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Name, "wid", ComparisonOperator.StartsWith);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            string pattern = dialect.Concat(dialect.RenderParameter("p0"), "'%'");
            Assert.Equal($"({dialect.Like(dialect.RenderIdentifier("Name"), pattern)})", sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void EndsWith_TranslatesToLikeWithLeadingWildcard(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterDescriptor<Widget> filter = new(w => w.Name, "get", ComparisonOperator.EndsWith);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, filter);

            string pattern = dialect.Concat("'%'", dialect.RenderParameter("p0"));
            Assert.Equal($"({dialect.Like(dialect.RenderIdentifier("Name"), pattern)})", sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void EmptyGroup_IsAlwaysTrue(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterGroup<Widget> group = new();

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, group);

            Assert.Equal("(1 = 1)", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void GroupAndAlso_CombinesChildNodesWithAnd(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterGroup<Widget> group = new(
            [
                new FilterDescriptor<Widget>(w => w.Name, "A"),
                new FilterDescriptor<Widget>(w => w.Quantity, 1, ComparisonOperator.GreaterThan)
            ]);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, group);

            string expected = $"(({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p0")}) AND " +
                               $"({dialect.RenderIdentifier("Quantity")} > {dialect.RenderParameter("p1")}))";
            Assert.Equal(expected, sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void GroupOrElse_CombinesChildNodesWithOr(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterGroup<Widget> group = new(
            [
                new FilterDescriptor<Widget>(w => w.Name, "A"),
                new FilterDescriptor<Widget>(w => w.Name, "B")
            ], LogicalOperator.OrElse);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, group);

            string expected = $"(({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p0")}) OR " +
                               $"({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p1")}))";
            Assert.Equal(expected, sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void GroupNot_NegatesTheWholeGroup(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterGroup<Widget> group = new([new FilterDescriptor<Widget>(w => w.Name, "A")], LogicalOperator.AndAlso, not: true);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, group);

            Assert.Equal($"NOT (({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p0")}))", sql);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void NestedGroups_ComposeCorrectly(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            FilterGroup<Widget> inner = new(
            [
                new FilterDescriptor<Widget>(w => w.Name, "A"),
                new FilterDescriptor<Widget>(w => w.Name, "B")
            ], LogicalOperator.OrElse);

            FilterGroup<Widget> outer = new(
            [
                new FilterDescriptor<Widget>(w => w.IsActive, true),
                inner
            ]);

            (string sql, DynamicParameters? parameters) = FilterNodeTranslator<Widget>.Translate(dialect, outer);

            string innerExpected = $"(({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p1")}) OR " +
                                    $"({dialect.RenderIdentifier("Name")} = {dialect.RenderParameter("p2")}))";
            string expected = $"(({dialect.RenderIdentifier("IsActive")} = {dialect.RenderParameter("p0")}) AND {innerExpected})";
            Assert.Equal(expected, sql);
        }
    }
}
