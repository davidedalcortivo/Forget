using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Utilities;
using System.Linq.Expressions;
using System.Reflection;


namespace Forget.Tests.Core
{
    /// <summary>
    /// C# 14 binds <c>array.Contains(x)</c> in an expression to <c>MemoryExtensions.Contains</c> on a span, converted from
    /// the array with <c>op_Implicit</c>, instead of <c>Enumerable.Contains</c>; for an array of a reference type the array
    /// is also wrapped in a conversion to its own type. The trees are built by hand here, so the cases are covered whatever
    /// language version the tests are compiled with.
    /// </summary>
    public class SpanContainsTests
    {
        private sealed class Holder
        {
            public int[] Ids = [1, 2, 3];
            public string[] Names = ["a", "b"];
            public int[]? None = null;
        }

        internal static Expression<Func<Widget, bool>> OnSpan<T>(Expression array, string column)
        {
            ParameterExpression widget = Expression.Parameter(typeof(Widget), "w");

            MethodInfo contains = typeof(MemoryExtensions).GetMethods()
                .Single(m => m.Name == nameof(MemoryExtensions.Contains)
                    && m.GetParameters() is [{ ParameterType: { IsGenericType: true } span }, _]
                    && span.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>))
                .MakeGenericMethod(typeof(T));

            MethodInfo implicitConversion = typeof(ReadOnlySpan<T>).GetMethod("op_Implicit", [typeof(T[])])!;

            return Expression.Lambda<Func<Widget, bool>>(
                Expression.Call(contains, Expression.Call(implicitConversion, array), Expression.Property(widget, column)),
                widget);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AnArrayContainsConvertedToASpan_TranslatesToIn(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Holder holder = new();
            Expression<Func<Widget, bool>>[] predicates =
            [
                OnSpan<int>(Expression.Constant(holder.Ids), nameof(Widget.Id)),
                OnSpan<int>(Expression.Field(Expression.Constant(holder), nameof(Holder.Ids)), nameof(Widget.Id)),
                OnSpan<int>(Expression.NewArrayInit(typeof(int), Expression.Constant(1), Expression.Constant(2), Expression.Constant(3)), nameof(Widget.Id))
            ];

            foreach (Expression<Func<Widget, bool>> predicate in predicates)
            {
                (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

                Assert.Equal($"({dialect.In(dialect.RenderIdentifier("Id"), dialect.RenderParameter("p0"), false)})", sql);
                Assert.Equal([1, 2, 3], parameters!.Get<int[]>("p0"));
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AnArrayOfAReferenceTypeConvertedToASpan_TranslatesToIn(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Holder holder = new();
            Expression<Func<Widget, bool>>[] predicates =
            [
                OnSpan<string>(Expression.Convert(Expression.Constant(holder.Names), typeof(string[])), nameof(Widget.Name)),
                OnSpan<string>(Expression.Convert(Expression.Field(Expression.Constant(holder), nameof(Holder.Names)), typeof(string[])), nameof(Widget.Name))
            ];

            foreach (Expression<Func<Widget, bool>> predicate in predicates)
            {
                (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

                Assert.Equal($"({dialect.In(dialect.RenderIdentifier("Name"), dialect.RenderParameter("p0"), false)})", sql);
                Assert.Equal(["a", "b"], parameters!.Get<string[]>("p0"));
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ANullArrayConvertedToASpan_IsAlwaysFalse(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = OnSpan<int>(Expression.Field(Expression.Constant(new Holder()), nameof(Holder.None)), nameof(Widget.Id));

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal("(1 = 0)", sql);
            Assert.Null(parameters);
        }
    }
}
