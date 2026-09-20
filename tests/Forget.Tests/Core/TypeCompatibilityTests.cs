using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Globalization;


namespace Forget.Tests.Core
{
    /// <summary>
    /// The one rule Forget applies to a value it is handed for a property (a filter value, an id, a value of an
    /// <c>Update(values)</c>): it is accepted when the database can compare or store it without losing anything, and it
    /// is never converted, so what is bound is exactly what the caller passed.
    /// </summary>
    public class TypeCompatibilityTests
    {
        private enum OtherKind { X = 1 }

        private sealed class RuleRow
        {
            public int Id { get; set; }
            public sbyte SByteValue { get; set; }
            public byte ByteValue { get; set; }
            public short ShortValue { get; set; }
            public ushort UShortValue { get; set; }
            public int IntValue { get; set; }
            public uint UIntValue { get; set; }
            public long LongValue { get; set; }
            public ulong ULongValue { get; set; }
            public float FloatValue { get; set; }
            public double DoubleValue { get; set; }
            public decimal DecimalValue { get; set; }
            public int? NullableIntValue { get; set; }
            public string TextValue { get; set; } = string.Empty;
            public Guid GuidValue { get; set; }
            public DateTime MomentValue { get; set; }
            public bool FlagValue { get; set; }
            public TypeMatrixKind KindValue { get; set; }
            public byte[] BytesValue { get; set; } = [];
        }

        public static TheoryData<string, object> Accepted() => new()
        {
            { nameof(RuleRow.LongValue), (byte)1 }, { nameof(RuleRow.LongValue), (short)1 }, { nameof(RuleRow.LongValue), 1 }, { nameof(RuleRow.LongValue), 1L },
            { nameof(RuleRow.IntValue), 1L }, { nameof(RuleRow.IntValue), (short)1 }, { nameof(RuleRow.IntValue), (byte)1 }, { nameof(RuleRow.ShortValue), 1L },
            { nameof(RuleRow.ByteValue), 1L }, { nameof(RuleRow.NullableIntValue), 1L }, { nameof(RuleRow.NullableIntValue), 1 },
            { nameof(RuleRow.DecimalValue), 1 }, { nameof(RuleRow.DecimalValue), 1L }, { nameof(RuleRow.DecimalValue), (byte)1 },
            { nameof(RuleRow.DecimalValue), (short)1 }, { nameof(RuleRow.DecimalValue), 1m },
            { nameof(RuleRow.DoubleValue), 1 }, { nameof(RuleRow.DoubleValue), (short)1 }, { nameof(RuleRow.DoubleValue), (byte)1 },
            { nameof(RuleRow.FloatValue), (short)1 }, { nameof(RuleRow.FloatValue), (byte)1 },
            { nameof(RuleRow.KindValue), 1 }, { nameof(RuleRow.KindValue), 1L }, { nameof(RuleRow.KindValue), TypeMatrixKind.Alpha },
            { nameof(RuleRow.IntValue), TypeMatrixKind.Alpha }, { nameof(RuleRow.LongValue), TypeMatrixKind.Beta },
            { nameof(RuleRow.BytesValue), new byte[] { 1, 2, 3 } },
            { nameof(RuleRow.LongValue), new int[] { 1, 2 } }, { nameof(RuleRow.LongValue), new List<short> { 1 } },
            { nameof(RuleRow.DecimalValue), new int[] { 1, 2 } }, { nameof(RuleRow.BytesValue), new List<byte[]> { new byte[] { 1 } } },
            { nameof(RuleRow.UIntValue), 1u }, { nameof(RuleRow.SByteValue), (sbyte)1 },
            { nameof(RuleRow.TextValue), "a" }, { nameof(RuleRow.GuidValue), Guid.NewGuid() }
        };

        public static TheoryData<string, object> Rejected() => new()
        {
            { nameof(RuleRow.IntValue), "1" }, { nameof(RuleRow.IntValue), 1.5m }, { nameof(RuleRow.IntValue), 1.5 },
            { nameof(RuleRow.IntValue), 1.5f }, { nameof(RuleRow.LongValue), 1m },
            { nameof(RuleRow.DecimalValue), 1.5 }, { nameof(RuleRow.DecimalValue), 1.5f },
            { nameof(RuleRow.FloatValue), 1 }, { nameof(RuleRow.FloatValue), 1L }, { nameof(RuleRow.FloatValue), 1.5 },
            { nameof(RuleRow.DoubleValue), 1L }, { nameof(RuleRow.DoubleValue), 1UL }, { nameof(RuleRow.DoubleValue), 1m },
            { nameof(RuleRow.TextValue), 1 }, { nameof(RuleRow.GuidValue), "x" }, { nameof(RuleRow.MomentValue), DateTimeOffset.UtcNow },
            { nameof(RuleRow.FlagValue), 1 }, { nameof(RuleRow.KindValue), "Alpha" }, { nameof(RuleRow.KindValue), OtherKind.X },
            { nameof(RuleRow.BytesValue), new int[] { 1 } }, { nameof(RuleRow.LongValue), new string[] { "1" } },
            { nameof(RuleRow.IntValue), new double[] { 1.5 } },
            { nameof(RuleRow.LongValue), (sbyte)1 }, { nameof(RuleRow.LongValue), (ushort)1 }, { nameof(RuleRow.LongValue), 1u }, { nameof(RuleRow.LongValue), 1UL },
            { nameof(RuleRow.IntValue), 1u }, { nameof(RuleRow.DecimalValue), 1UL }, { nameof(RuleRow.DoubleValue), 1u },
            { nameof(RuleRow.LongValue), new uint[] { 1 } }, { nameof(RuleRow.DoubleValue), 1f }, { nameof(RuleRow.DoubleValue), 1.1f },
            { nameof(RuleRow.IntValue), new byte[] { 1 } }, { nameof(RuleRow.IntValue), new List<byte> { 1 } }, { nameof(RuleRow.ByteValue), new byte[] { 1 } }
        };

        [Theory]
        [MemberData(nameof(Accepted), DisableDiscoveryEnumeration = true)]
        public void FilterDescriptor_AcceptsAValueTheDatabaseCanCompareWithoutLoss(string property, object value)
        {
            Exception? ex = Record.Exception(() => new FilterDescriptor<RuleRow>(property, value));

            Assert.True(ex is null, ex?.Message);
        }

        [Theory]
        [MemberData(nameof(Rejected), DisableDiscoveryEnumeration = true)]
        public void FilterDescriptor_RejectsAValueOfAnIncompatibleType(string property, object value)
        {
            Assert.Throws<ArgumentException>(() => new FilterDescriptor<RuleRow>(property, value));
        }

        [Fact]
        public void UpdateValues_AcceptATypeThatFitsTheProperty()
        {
            object[] accepted =
            [
                new { DecimalValue = 5 }, new { DecimalValue = 5L }, new { LongValue = 5 }, new { LongValue = (short)5 }, new { IntValue = (short)5 },
                new { KindValue = 1 }, new { IntValue = TypeMatrixKind.Alpha }, new { DoubleValue = 5 }, new { NullableIntValue = 5 }, new { TextValue = "a" }
            ];

            foreach (object values in accepted)
            {
                Exception? ex = Record.Exception(() => ValuesPropertyCache<RuleRow>.Get(values));
                Assert.True(ex is null, $"{values.GetType().GetProperties()[0].Name}: {ex?.Message}");
            }
        }

        [Fact]
        public void UpdateValues_RejectATypeThatDoesNotFitTheProperty()
        {
            object[] rejected =
            [
                new { IntValue = "5" }, new { FloatValue = 5 }, new { DecimalValue = 5.5 }, new { IntValue = 5.5m },
                new { IntValue = 5L }, new { IntValue = 5u }, new { NullableIntValue = 5L }, new { ShortValue = 5 }, new { KindValue = 1L },
                new { IntValue = (sbyte)5 }, new { IntValue = (ushort)5 }, new { LongValue = 5u }, new { DecimalValue = 5UL }, new { DoubleValue = 1.1f }
            ];

            foreach (object values in rejected)
                Assert.Throws<ArgumentException>(() => ValuesPropertyCache<RuleRow>.Get(values));
        }

        public static TheoryData<Type, Type> ResultsThatCanHold() => new()
        {
            { typeof(long), typeof(int) }, { typeof(long), typeof(int?) }, { typeof(decimal), typeof(int) }, { typeof(decimal), typeof(long) },
            { typeof(double), typeof(int) }, { typeof(float), typeof(short) }, { typeof(int), typeof(TypeMatrixKind) },
            { typeof(long), typeof(TypeMatrixKind) }, { typeof(TypeMatrixKind), typeof(int) }, { typeof(int), typeof(int) }, { typeof(int?), typeof(int) }, { typeof(object), typeof(string) }
        };

        public static TheoryData<Type, Type> ResultsThatCannotHold() => new()
        {
            { typeof(double), typeof(float) }, { typeof(short), typeof(int) }, { typeof(int), typeof(long) }, { typeof(int), typeof(decimal) }, { typeof(float), typeof(double) },
            { typeof(float), typeof(int) }, { typeof(double), typeof(long) }, { typeof(long), typeof(decimal) },
            { typeof(string), typeof(int) }, { typeof(long), typeof(string) }, { typeof(TypeMatrixKind), typeof(long) }
        };

        [Theory]
        [MemberData(nameof(ResultsThatCanHold))]
        public void CanHold_IsTrueWhenEveryValueOfThePropertyFitsTheResultType(Type resultType, Type propertyType)
        {
            Assert.True(PropertyHelper.CanHold(resultType, propertyType));
        }

        [Theory]
        [MemberData(nameof(ResultsThatCannotHold))]
        public void CanHold_IsFalseWhenAValueOfThePropertyCouldNotFit(Type resultType, Type propertyType)
        {
            Assert.False(PropertyHelper.CanHold(resultType, propertyType));
        }

        private static string Text(object value) => Convert.ToString(value, CultureInfo.InvariantCulture)!;

        private static List<string> ConversionsThatChangeTheNumber(Func<Type, Type, bool> accepted)
        {
            Type[] numeric = [typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)];
            string[] samples = ["0", "1", "100", "0.1", "1.1", "3.14159"];
            List<string> changed = [];

            foreach (Type from in numeric)
            {
                foreach (Type to in numeric)
                {
                    if (from == to || !accepted(from, to))
                        continue;

                    foreach (string sample in samples)
                    {
                        object source;

                        try { source = Convert.ChangeType(sample, from, CultureInfo.InvariantCulture); }
                        catch (FormatException) { continue; }

                        object converted = Convert.ChangeType(source, to, CultureInfo.InvariantCulture);

                        if (Text(source) != Text(converted))
                            changed.Add($"{from.Name} {Text(source)} becomes {to.Name} {Text(converted)}");
                    }
                }
            }

            return changed;
        }

        [Fact]
        public void EveryValueTypeAcceptedForAProperty_KeepsTheNumberTheCallerMeant()
        {
            Assert.Empty(ConversionsThatChangeTheNumber((value, property) => PropertyHelper.IsCompatible(property, value)));
        }

        [Fact]
        public void EveryResultTypeAccepted_KeepsTheNumberTheCallerMeant()
        {
            Assert.Empty(ConversionsThatChangeTheNumber((property, result) => PropertyHelper.CanHold(result, property)));
        }

        [Fact]
        public void EveryWriteTypeAccepted_KeepsTheNumberTheCallerMeant()
        {
            Assert.Empty(ConversionsThatChangeTheNumber((value, property) => PropertyHelper.CanStore(property, value)));
        }

        public static TheoryData<Type, Type> ValuesThatCanBeStored() => new()
        {
            { typeof(long), typeof(int) }, { typeof(long), typeof(short) }, { typeof(long), typeof(byte) }, { typeof(decimal), typeof(int) },
            { typeof(decimal), typeof(long) }, { typeof(double), typeof(int) }, { typeof(double), typeof(short) }, { typeof(float), typeof(short) },
            { typeof(float), typeof(byte) }, { typeof(int), typeof(int) }, { typeof(int), typeof(short) }, { typeof(int?), typeof(int) },
            { typeof(int), typeof(int?) }, { typeof(uint), typeof(uint) }, { typeof(sbyte), typeof(sbyte) }, { typeof(long), typeof(TypeMatrixKind) },
            { typeof(TypeMatrixKind), typeof(int) }, { typeof(TypeMatrixKind), typeof(TypeMatrixKind) }, { typeof(string), typeof(string) },
            { typeof(object), typeof(string) }
        };

        public static TheoryData<Type, Type> ValuesThatCannotBeStored() => new()
        {
            { typeof(int), typeof(long) }, { typeof(short), typeof(int) }, { typeof(int), typeof(decimal) }, { typeof(float), typeof(int) },
            { typeof(double), typeof(long) }, { typeof(double), typeof(float) }, { typeof(long), typeof(sbyte) }, { typeof(long), typeof(ushort) },
            { typeof(long), typeof(uint) }, { typeof(decimal), typeof(ulong) }, { typeof(decimal), typeof(sbyte) }, { typeof(int), typeof(uint) },
            { typeof(TypeMatrixKind), typeof(long) }, { typeof(string), typeof(int) }, { typeof(Guid), typeof(string) }, { typeof(int), typeof(double) }
        };

        [Theory]
        [MemberData(nameof(ValuesThatCanBeStored))]
        public void CanStore_IsTrueWhenTheValueFitsThePropertyAndTheDriversBindIt(Type propertyType, Type valueType)
        {
            Assert.True(PropertyHelper.CanStore(propertyType, valueType));
        }

        [Theory]
        [MemberData(nameof(ValuesThatCannotBeStored))]
        public void CanStore_IsFalseWhenTheValueDoesNotFitOrTheDriversCannotBindIt(Type propertyType, Type valueType)
        {
            Assert.False(PropertyHelper.CanStore(propertyType, valueType));
        }

        [Fact]
        public void CanStore_NeverAcceptsWhatCanHoldRefuses_AndAcceptsEveryTypeForItself()
        {
            Type[] numeric = [typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)];

            foreach (Type property in numeric)
            {
                Assert.True(PropertyHelper.CanStore(property, property));

                foreach (Type value in numeric)
                    Assert.True(!PropertyHelper.CanStore(property, value) || PropertyHelper.CanHold(property, value), $"{property.Name} stores {value.Name} but cannot hold it");
            }
        }
    }
}
