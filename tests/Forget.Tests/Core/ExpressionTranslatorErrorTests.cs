using Forget.Core.Abstractions.Strategies;
using Forget.Core.Utilities;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;


namespace Forget.Tests.Core
{
    /// <summary>
    /// The predicate translator promises <see cref="NotSupportedException"/> for an expression it cannot translate.
    /// These tests check that promise for the cases that used to escape as a bare
    /// <see cref="KeyNotFoundException"/> (a property the entity does not map) or to lose the reason they failed.
    /// </summary>
    public class ExpressionTranslatorErrorTests
    {
        [Table("Thing", Schema = "dbo")]
        private sealed class Thing
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;

            [NotMapped]
            public string Computed => Name + "!";

            [NotMapped]
            public bool ComputedFlag => Name.Length > 0;
        }

        private sealed class Holder
        {
            public string Value { get; set; } = string.Empty;
        }

        // One predicate per place in the translator that turns a property into a column.
        private static readonly Expression<Func<Thing, bool>>[] NotMappedPredicates =
        [
            t => t.Computed == "x",
            t => !t.ComputedFlag,
            t => t.ComputedFlag,
            t => t.Id > 1 && t.ComputedFlag
        ];

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ANotMappedProperty_InAPredicate_ThrowsNotSupportedExceptionNamingThePropertyAndTheEntity(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;

            foreach (Expression<Func<Thing, bool>> predicate in NotMappedPredicates)
            {
                NotSupportedException ex = Assert.Throws<NotSupportedException>(() => ExpressionTranslator<Thing>.Translate(dialect, predicate));

                Assert.Contains("'Computed", ex.Message);
                Assert.Contains("'Thing'", ex.Message);
                Assert.Contains("not mapped", ex.Message);
            }
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AMemberOfAMember_InAPredicate_ThrowsNotSupportedExceptionWithTheCauseAttached(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Expression<Func<Thing, bool>> predicate = t => t.Name.Length > 3;

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => ExpressionTranslator<Thing>.Translate(dialect, predicate));

            Assert.Contains("t.Name.Length", ex.Message);
            Assert.NotNull(ex.InnerException);
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void AValueThatCannotBeEvaluated_InAPredicate_ThrowsNotSupportedExceptionWithTheCauseAttached(string dialectName, object dialectObject)
        {
            _ = dialectName;
            ISqlDialectStrategy dialect = (ISqlDialectStrategy)dialectObject;
            Holder? holder = null;
            Expression<Func<Thing, bool>> predicate = t => t.Name == holder!.Value;

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => ExpressionTranslator<Thing>.Translate(dialect, predicate));

            Assert.Contains("holder.Value", ex.Message);
            Assert.IsType<NullReferenceException>(ex.InnerException);
        }
    }
}
