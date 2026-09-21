using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Utilities;
using System.Linq.Expressions;


namespace Forget.Tests.Core
{
    /// <summary>
    /// An expression the translator does not know must be refused, never turned into SQL by concatenating whatever its
    /// parts translate to: <c>new DateTime(2026, 1, 1)</c> became <c>@p0@p1@p2</c> and <c>-x</c> became <c>x</c>.
    /// </summary>
    public class UnsupportedExpressionTests
    {
        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AnExpressionThatIsNotATranslatableValueOrCondition_ThrowsNotSupportedException(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            int x = 5;
            bool flag = true;
            string text = "abc";
            object boxed = "abc";
            int[] ids = [1, 2];
            (string Name, Expression<Func<Widget, bool>> Predicate)[] cases =
            [
                ("negation of a value", w => w.Id == -x),
                ("negation of a column", w => -w.Id == x),
                ("checked negation", w => w.Id == checked(-x)),
                ("bitwise not of a value", w => w.Id == ~x),
                ("length of an array", w => w.Id == ids.Length),
                ("new of a value type", w => w.Price > new decimal(1.5)),
                ("new of a string", w => w.Name == new string('a', 3)),
                ("new of a Guid", w => w.Name == new Guid(text).ToString()),
                ("conditional", w => w.Id == (flag ? 1 : 2)),
                ("is", w => w.Name == text && boxed is string),
                ("as", w => (boxed as string) == w.Name),
                ("array initializer used as a value", w => w.Id == new[] { 1, 2 }.Length)
            ];

            foreach ((string name, Expression<Func<Widget, bool>> predicate) in cases)
            {
                Exception? ex = Record.Exception(() => ExpressionTranslator<Widget>.Translate(dialect, predicate));

                if (ex is not NotSupportedException refused)
                {
                    Assert.Fail($"{name}: expected a NotSupportedException but got {ex?.GetType().Name ?? "no exception"}");
                    return;
                }

                Assert.True(refused.Message.StartsWith("Unsupported"), $"{name}: the message is '{refused.Message}'");
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AnExpressionThatIsEvaluatedAsAValue_StillWorks(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Widget, bool>> predicate = w => w.Name.Contains(new string('a', 2)) && new[] { 1, 2 }.Contains(w.Id);

            (string sql, DynamicParameters? parameters) = ExpressionTranslator<Widget>.Translate(dialect, predicate);

            Assert.Equal("aa", parameters!.Get<string>("p0"));
            Assert.Equal([1, 2], parameters.Get<int[]>("p1"));
            Assert.DoesNotContain("@p0@", sql);
        }
    }
}
