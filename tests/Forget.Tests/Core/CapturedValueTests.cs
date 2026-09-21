using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Utilities;
using System.Collections;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.ExceptionServices;


namespace Forget.Tests.Core
{
    /// <summary>
    /// A value that a predicate takes from a variable, a field, a property or a method must translate exactly like the
    /// same value written in the predicate, and translating it must not cost more than the literal does.
    /// </summary>
    public class CapturedValueTests
    {
        private sealed class Criteria
        {
            public int Id { get; set; } = 7;
            public string Name { get; set; } = "abc";
            public string? Nickname { get; set; }
            public int? Quantity { get; set; } = 5;
            public Criteria? Next { get; set; }
            public List<int> Ids { get; set; } = [1, 2, 3];
        }

        private static class StaticCriteria
        {
            public static int Id = 7;
            public static string Name { get; } = "abc";
        }

        private enum Stage
        {
            Created = 1,
            Shipped = 2
        }

        [Table("Shipment", Schema = "dbo")]
        private sealed class Shipment
        {
            public int Id { get; set; }
            public Stage Stage { get; set; }
        }

        [ThreadStatic]
        private static int _firstChanceExceptions;

        private readonly int _id = 7;

        private (string Name, Expression<Func<Widget, bool>> Captured, Expression<Func<Widget, bool>> Literal)[] Cases()
        {
            int id = 7;
            string name = "abc";
            string? nothing = null;
            int? quantity = 5;
            int? noQuantity = null;
            int? nullableId = 7;
            object? nothingObject = null;
            object textObject = "abc";
            int letter = 65;
            List<int> list = [1, 2, 3];
            IEnumerable<int> sequence = list;
            Criteria criteria = new() { Next = new() };

            return
            [
                ("local variable", w => w.Id == id, w => w.Id == 7),
                ("instance field", w => w.Id == _id, w => w.Id == 7),
                ("property of a captured object", w => w.Id == criteria.Id, w => w.Id == 7),
                ("property of a property", w => w.Id == criteria.Next!.Id, w => w.Id == 7),
                ("static field", w => w.Id == StaticCriteria.Id, w => w.Id == 7),
                ("static property", w => w.Name == StaticCriteria.Name, w => w.Name == "abc"),
                ("string", w => w.Name == name, w => w.Name == "abc"),
                ("not equal", w => w.Name != name, w => w.Name != "abc"),
                ("greater than", w => w.Id > id, w => w.Id > 7),
                ("less than or equal", w => w.Id <= id, w => w.Id <= 7),
                ("nullable with a value", w => w.Quantity == quantity, w => w.Quantity == 5),
                ("nullable without a value", w => w.Quantity == noQuantity, w => w.Quantity == null),
                ("nullable not equal without a value", w => w.Quantity != noQuantity, w => w.Quantity != null),
                ("null string", w => w.Nickname == nothing, w => w.Nickname == null),
                ("null string not equal", w => w.Nickname != nothing, w => w.Nickname != null),
                ("null property", w => w.Nickname == criteria.Nickname, w => w.Nickname == null),
                ("column compared with a nullable", w => w.Id == nullableId, w => w.Id == 7),
                ("null object cast to a string", w => w.Nickname == (string?)nothingObject, w => w.Nickname == null),
                ("object cast to a string", w => w.Name == (string)textObject, w => w.Name == "abc"),
                ("null object cast to a string, not equal", w => w.Nickname != (string?)nothingObject, w => w.Nickname != null),
                ("nullable column compared with an int", w => w.Quantity == id, w => w.Quantity == 7),
                ("nullable column greater than an int", w => w.Quantity > id, w => w.Quantity > 7),
                ("two values", w => w.Id == id && w.Name == name, w => w.Id == 7 && w.Name == "abc"),
                ("either value", w => w.Id == id || w.Name == name, w => w.Id == 7 || w.Name == "abc"),
                ("Contains", w => w.Name.Contains(name), w => w.Name.Contains("abc")),
                ("StartsWith", w => w.Name.StartsWith(name), w => w.Name.StartsWith("abc")),
                ("EndsWith", w => w.Name.EndsWith(criteria.Name), w => w.Name.EndsWith("abc")),
                ("null Contains", w => w.Name.Contains(nothing!), w => w.Name.Contains(null!)),
                ("ToLower on both sides", w => w.Name.ToLower() == name.ToLower(), w => w.Name.ToLower() == "abc".ToLower()),
                ("ToUpper against a variable", w => w.Name.ToUpper() == name, w => w.Name.ToUpper() == "abc"),
                ("a variable in ToUpper", w => w.Name == name.ToUpper(), w => w.Name == "abc".ToUpper()),
                ("ToString of a column", w => w.Id.ToString() == name, w => w.Id.ToString() == "abc"),
                ("Contains with a char converted from an int", w => w.Name.Contains((char)letter), w => w.Name.Contains('A')),
                ("Contains with an object converted to a string", w => w.Name.Contains((string)textObject), w => w.Name.Contains("abc")),
                ("list", w => list.Contains(w.Id), w => new List<int> { 1, 2, 3 }.Contains(w.Id)),
                ("list of a captured object", w => criteria.Ids.Contains(w.Id), w => new List<int> { 1, 2, 3 }.Contains(w.Id)),
                ("sequence", w => sequence.Contains(w.Id), w => new List<int> { 1, 2, 3 }.Contains(w.Id))
            ];
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AValueTakenFromAVariable_TranslatesLikeTheSameValueWrittenInThePredicate(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;

            foreach ((string name, Expression<Func<Widget, bool>> captured, Expression<Func<Widget, bool>> literal) in Cases())
            {
                (string expectedSql, DynamicParameters? expectedParameters) = ExpressionTranslator<Widget>.Translate(dialect, literal);
                (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, captured);

                Assert.True(expectedSql == sql, $"{name}: expected '{expectedSql}' but got '{sql}'");
                Assert.True(Describe(expectedParameters) == Describe(parameters), $"{name}: expected parameters '{Describe(expectedParameters)}' but got '{Describe(parameters)}'");
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AnIntConvertedToAnEnum_TranslatesLikeTheEnumValue(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            int code = 2;
            Expression<Func<Shipment, bool>> captured = s => s.Stage == (Stage)code;
            Expression<Func<Shipment, bool>> literal = s => s.Stage == Stage.Shipped;

            (string expectedSql, DynamicParameters? expectedParameters) = ExpressionTranslator<Shipment>.Translate(dialect, literal);
            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Shipment>.Translate(dialect, captured);

            Assert.Equal(expectedSql, sql);
            Assert.Equal(Describe(expectedParameters), Describe(parameters));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AConversionOfAValue_IsNotAppliedToTheParameter(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            int id = 7;
            long wide = 7;
            long tooWide = long.MaxValue;
            double narrow = 12.5;
            (string Column, Expression<Func<Widget, bool>> Predicate, string Parameter)[] cases =
            [
                ("Price", w => w.Price == id, "p0=7 (Int32)"),
                ("Id", w => w.Id == (int)wide, "p0=7 (Int64)"),
                ("Id", w => w.Id == checked((int)wide), "p0=7 (Int64)"),
                ("Id", w => w.Id == checked((int)tooWide), $"p0={long.MaxValue} (Int64)"),
                ("Price", w => w.Price == (decimal)narrow, "p0=12.5 (Double)")
            ];

            foreach ((string column, Expression<Func<Widget, bool>> predicate, string parameter) in cases)
            {
                (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

                Assert.True($"({dialect.RenderIdentifier(column)} = {dialect.RenderParameter("p0")})" == sql, $"{predicate}: got '{sql}'");
                Assert.True(parameter == Describe(parameters), $"{predicate}: expected '{parameter}' but got '{Describe(parameters)}'");
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void TwoColumns_AreComparedWithoutAParameter(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Id == w.Quantity;

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal($"({dialect.RenderIdentifier("Id")} = {dialect.RenderIdentifier("Quantity")})", sql);
            Assert.Null(parameters);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ComparingAColumn_ThrowsNoExceptionWhileTranslating(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            int id = 7;
            int? nullableId = 7;
            string? nothing = null;
            string name = "abc";
            Expression<Func<Widget, bool>>[] predicates =
            [
                w => w.Id == 7,
                w => w.Id != 7,
                w => w.Nickname == null,
                w => w.Id == id,
                w => w.Id == nullableId,
                w => w.Nickname == nothing,
                w => w.Nickname != nothing,
                w => w.Id > id,
                w => w.Name.ToLower() == name,
                w => w.Name.ToUpper() == name,
                w => w.Id.ToString() == name,
                w => w.Name.ToLower() == name.ToLower(),
                w => w.Name == name.ToUpper(),
                w => w.Quantity.HasValue == false,
                w => w.Quantity.HasValue == w.IsActive
            ];

            static void Count(object? sender, FirstChanceExceptionEventArgs e) => _firstChanceExceptions++;

            AppDomain.CurrentDomain.FirstChanceException += Count;

            try
            {
                foreach (Expression<Func<Widget, bool>> predicate in predicates)
                {
                    ExpressionTranslator<Widget>.Translate(dialect, predicate);

                    _firstChanceExceptions = 0;
                    ExpressionTranslator<Widget>.Translate(dialect, predicate);

                    Assert.True(_firstChanceExceptions == 0, $"{predicate} threw {_firstChanceExceptions} exception(s) while being translated");
                }
            }
            finally
            {
                AppDomain.CurrentDomain.FirstChanceException -= Count;
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AValueTakenFromAVariable_AllocatesAsLittleAsTheSameValueWrittenInThePredicate(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            int id = 7;
            string name = "abc";
            long wide = 7;
            double narrow = 12.5;
            Criteria criteria = new() { Next = new() };
            Expression<Func<Widget, bool>>[] captured =
            [
                w => w.Id == id,
                w => w.Name == name,
                w => w.Id == criteria.Next!.Id,
                w => w.Name.Contains(name),
                w => w.Quantity == id,
                w => w.Price == id,
                w => w.Id == (int)wide,
                w => w.Price == (decimal)narrow
            ];
            Expression<Func<Widget, bool>>[] literal =
            [
                w => w.Id == 7,
                w => w.Name == "abc",
                w => w.Id == 7,
                w => w.Name.Contains("abc"),
                w => w.Quantity == 7,
                w => w.Price == 7m,
                w => w.Id == 7,
                w => w.Price == 12.5m
            ];

            for (int i = 0; i < captured.Length; i++)
            {
                long extra = AllocatedBytes(dialect, captured[i]) - AllocatedBytes(dialect, literal[i]);

                Assert.True(extra < 1024, $"{captured[i]} allocates {extra} bytes more than {literal[i]} for each translation");
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ACollectionTakenFromAVariable_AllocatesLessThanACompiledLambda(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            List<int> list = [1, 2, 3];
            string[] names = ["a", "b"];
            Expression<Func<Widget, bool>>[] predicates =
            [
                w => list.Contains(w.Id),
                w => names.Contains(w.Name),
                SpanContainsTests.OnSpan<string>(Expression.Convert(Expression.Constant(names), typeof(string[])), nameof(Widget.Name))
            ];

            foreach (Expression<Func<Widget, bool>> predicate in predicates)
            {
                long bytes = AllocatedBytes(dialect, predicate);

                Assert.True(bytes < 3 * 1024, $"{predicate} allocates {bytes} bytes for each translation");
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AMethodCalledOnAVariable_AllocatesLessThanACompiledLambda(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            string name = "abc";
            Expression<Func<Widget, bool>>[] predicates =
            [
                w => w.Name.ToLower() == name.ToLower(),
                w => w.Name == name.ToUpper()
            ];

            foreach (Expression<Func<Widget, bool>> predicate in predicates)
            {
                long bytes = AllocatedBytes(dialect, predicate);

                Assert.True(bytes < 3 * 1024, $"{predicate} allocates {bytes} bytes for each translation");
            }
        }

        private static long AllocatedBytes(ISqlDialectStrategy dialect, Expression<Func<Widget, bool>> predicate)
        {
            const int Iterations = 300;

            for (int i = 0; i < 100; i++)
            {
                ExpressionTranslator<Widget>.Translate(dialect, predicate);
            }

            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < Iterations; i++)
            {
                ExpressionTranslator<Widget>.Translate(dialect, predicate);
            }

            return (GC.GetAllocatedBytesForCurrentThread() - before) / Iterations;
        }

        private static string Format(object? value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

        private static string Describe(DynamicParameters? parameters)
        {
            if (parameters is null)
            {
                return "none";
            }

            return string.Join("; ", parameters.ParameterNames.Select(name =>
            {
                object? value = parameters.Get<object>(name);
                string text = value is not string and IEnumerable values
                    ? string.Join(",", values.Cast<object?>().Select(Format))
                    : Format(value);
                return $"{name}={text} ({value?.GetType().Name})";
            }));
        }
    }
}
