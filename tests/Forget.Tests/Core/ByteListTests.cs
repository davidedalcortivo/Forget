using Dapper;
using Forget.Core.Abstractions.Strategies;
using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Linq.Expressions;


namespace Forget.Tests.Core
{
    /// <summary>
    /// A list of bytes, or of enum values whose underlying type is byte, is bound as a list of <c>short</c>, because every
    /// driver binds a <c>byte[]</c> as one binary value. An <c>In</c> filter needs a list: a single value of the type of
    /// the property (a <c>byte[]</c> for a <c>byte[]</c> property) is refused, and a list of bytes is still refused with
    /// any other operator, where it would be bound as that one binary value.
    /// </summary>
    public class ByteListTests
    {
        private enum ByteKind : byte
        {
            One = 1,
            Three = 3
        }

        private sealed class ByteRow
        {
            public int Id { get; set; }
            public byte Code { get; set; }
            public ByteKind Kind { get; set; }
            public byte[] Payload { get; set; } = [];
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ContainsOnAListOfBytes_BindsThemAsShort(string dialectName, object dialectObject)
        {
            byte[] codes = [1, 3];
            Expression<Func<ByteRow, bool>> predicate = x => codes.Contains(x.Code);

            (_, DynamicParameters? parameters) = ExpressionTranslator<ByteRow>.Translate((ISqlDialectStrategy)dialectObject, predicate);

            Assert.Equal<object?>([(short)1, (short)3], DialectStrategies.ListValues(dialectName, parameters!, "p_0_", 2, false));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void ContainsOnAListOfEnumsWhoseUnderlyingTypeIsByte_BindsThemAsShort(string dialectName, object dialectObject)
        {
            ByteKind[] kinds = [ByteKind.One, ByteKind.Three];
            Expression<Func<ByteRow, bool>> predicate = x => kinds.Contains(x.Kind);

            (_, DynamicParameters? parameters) = ExpressionTranslator<ByteRow>.Translate((ISqlDialectStrategy)dialectObject, predicate);

            Assert.Equal<object?>([(short)1, (short)3], DialectStrategies.ListValues(dialectName, parameters!, "p_0_", 2, false));
        }

        [Theory]
        [MemberData(nameof(DialectStrategies.All), MemberType = typeof(DialectStrategies))]
        public void InWithAListOfBytes_BindsThemAsShort(string dialectName, object dialectObject)
        {
            FilterDescriptor<ByteRow> filter = new("Code", new List<byte> { 1, 3 }, ComparisonOperator.In);

            (_, DynamicParameters? parameters) = FilterNodeTranslator<ByteRow>.Translate((ISqlDialectStrategy)dialectObject, filter);

            Assert.Equal<object?>([(short)1, (short)3], DialectStrategies.ListValues(dialectName, parameters!, "p_0_", 2, false));
        }

        [Fact]
        public void InWithASingleValueOfTheTypeOfTheProperty_IsRefused()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new FilterDescriptor<ByteRow>("Payload", new byte[] { 1, 2 }, ComparisonOperator.In));

            Assert.Contains("one value", ex.Message);
        }

        [Fact]
        public void AListOfBytesWithAnOperatorOtherThanIn_IsStillRefused()
        {
            ArgumentException ex = Assert.Throws<ArgumentException>(() => new FilterDescriptor<ByteRow>("Code", new byte[] { 1, 2 }));

            Assert.Contains("list of bytes", ex.Message);
        }
    }
}
