using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Utilities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;


namespace Forget.Tests.Core
{
    public class NullableColumnTests
    {
        [Table("Entry", Schema = "dbo")]
        private sealed class Entry
        {
            public int Id { get; set; }
            public DateTime? Deleted { get; set; }

            [NotMapped]
            public int? Computed { get; set; }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void HasValue_TranslatesToIsNotNull(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Quantity.HasValue;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.IsNotNull(dialect.RenderIdentifier("Quantity"))})", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void NotHasValue_TranslatesToNotIsNotNull(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => !w.Quantity.HasValue;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"NOT ({dialect.IsNotNull(dialect.RenderIdentifier("Quantity"))})", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void HasValueComparedWithABoolean_TranslatesToIsNullOrIsNotNull(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            string column = dialect.RenderIdentifier("Quantity");
            bool no = false;
            (Expression<Func<Widget, bool>> Predicate, string Expected)[] cases =
            [
                (w => w.Quantity.HasValue == true, $"({dialect.IsNotNull(column)})"),
                (w => w.Quantity.HasValue == false, $"({dialect.IsNull(column)})"),
                (w => w.Quantity.HasValue != true, $"({dialect.IsNull(column)})"),
                (w => w.Quantity.HasValue != false, $"({dialect.IsNotNull(column)})"),
                (w => true == w.Quantity.HasValue, $"({dialect.IsNotNull(column)})"),
                (w => false == w.Quantity.HasValue, $"({dialect.IsNull(column)})"),
                (w => w.Quantity.HasValue == no, $"({dialect.IsNull(column)})")
            ];

            foreach ((Expression<Func<Widget, bool>> predicate, string expected) in cases)
            {
                (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

                Assert.True(expected == sql, $"{predicate}: expected '{expected}' but got '{sql}'");
                Assert.Null(parameters);
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ValueOfANullableColumn_IsTheColumn(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Quantity!.Value > 5;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.RenderIdentifier("Quantity")} > {dialect.RenderParameter("p0")})", sql);
            Assert.Equal(5, parameters!.Get<int>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void HasValueAndValue_CombineWithTheOtherOperators(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            string column = dialect.RenderIdentifier("Quantity");
            string parameter = dialect.RenderParameter("p0");
            (Expression<Func<Widget, bool>> Predicate, string Expected)[] cases =
            [
                (w => w.Quantity.HasValue && w.Quantity.Value > 5, $"(({dialect.IsNotNull(column)}) AND ({column} > {parameter}))"),
                (w => !w.Quantity.HasValue || w.Quantity.Value > 5, $"(NOT ({dialect.IsNotNull(column)}) OR ({column} > {parameter}))"),
                (w => w.IsActive && w.Quantity.HasValue, $"(({dialect.IsTrue(dialect.RenderIdentifier("IsActive"))}) AND ({dialect.IsNotNull(column)}))")
            ];

            foreach ((Expression<Func<Widget, bool>> predicate, string expected) in cases)
            {
                (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

                Assert.True(expected == sql, $"{predicate}: expected '{expected}' but got '{sql}'");
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ValueOfANullableDateColumn_IsComparedWithAVariable(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            DateTime since = new(2026, 1, 1);
            Expression<Func<Entry, bool>> predicate = e => e.Deleted.HasValue && e.Deleted.Value > since;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Entry>.Translate(dialect, predicate);

            string column = dialect.RenderIdentifier("Deleted");
            Assert.Equal($"(({dialect.IsNotNull(column)}) AND ({column} > {dialect.RenderParameter("p0")}))", sql);
            Assert.Equal(since, parameters!.Get<DateTime>("p0"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void HasValueOfANullableVariable_IsAValueAndNotAColumn(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            int? quantity = 5;
            Expression<Func<Widget, bool>> predicate = w => w.IsActive == quantity.HasValue && w.Id == quantity.Value;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"(({dialect.RenderIdentifier("IsActive")} = {dialect.RenderParameter("p0")}) AND ({dialect.RenderIdentifier("Id")} = {dialect.RenderParameter("p1")}))", sql);
            Assert.Equal(true, parameters!.Get<object>("p0"));
            Assert.Equal(5, parameters.Get<int>("p1"));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void HasValueOfANotMappedProperty_ThrowsNotSupportedExceptionNamingTheProperty(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Entry, bool>> predicate = e => e.Computed.HasValue;

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => ExpressionTranslator<Entry>.Translate(dialect, predicate));

            Assert.Contains("'Computed'", ex.Message);
            Assert.Contains("not mapped", ex.Message);
        }
    }
}
