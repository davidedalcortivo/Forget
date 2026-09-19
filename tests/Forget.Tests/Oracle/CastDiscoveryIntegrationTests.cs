using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.Oracle.Extensions;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// Oracle has no native multi-row <c>VALUES (...), (...)</c>, so Forget builds <c>SELECT ... FROM DUAL UNION ALL
    /// ...</c> and wraps each bound value in a <c>CAST</c> discovered from the data dictionary (otherwise a first row
    /// full of nulls gives <c>ORA-01790</c>). These tests run that discovery against a real schema and check that the
    /// cast it picks does not change the value on its way into the column — in particular for <c>NUMBER</c> columns
    /// without a precision, whose <c>DATA_LENGTH</c> (22, the storage size) once got mistaken for a precision.
    /// </summary>
    [Collection(OracleCollection.Name)]
    public class CastDiscoveryIntegrationTests
    {
        private readonly OracleFixture _fixture;

        public CastDiscoveryIntegrationTests(OracleFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static CastRow Full(int id, decimal plain, decimal scaled) => new()
        {
            Id = id,
            Plain = plain,
            Scaled = scaled,
            Exact = 123.4567m,
            Ratio = 0.125,
            Approx = 2.5,
            Body = "a clob value",
            Payload = [1, 2, 3, 4],
            Moment = new DateTime(2026, 9, 19, 10, 11, 12, 123),
            Day = new DateTime(2026, 9, 19, 10, 11, 12),
            Code = "abcde",
            CharSemantic = "añ€bc",
            National = "ünï€x",
            Label = "a label",
            LabelChars = "ünï€ label",
            Unicode = "ünï€",
            Token = [.. Enumerable.Range(1, 16).Select(i => (byte)i)]
        };

        private static void AssertSame(CastRow expected, CastRow? actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.Plain, actual!.Plain);
            Assert.Equal(expected.Scaled, actual.Scaled);
            Assert.Equal(expected.Exact, actual.Exact);
            Assert.Equal(expected.Ratio, actual.Ratio);
            Assert.Equal(expected.Approx, actual.Approx);
            Assert.Equal(expected.Body, actual.Body);
            Assert.Equal(expected.Payload, actual.Payload);
            Assert.Equal(expected.Moment, actual.Moment);
            Assert.Equal(expected.Day, actual.Day);
            Assert.Equal(expected.Code, actual.Code);
            Assert.Equal(expected.CharSemantic, actual.CharSemantic);
            Assert.Equal(expected.National, actual.National);
            Assert.Equal(expected.Label, actual.Label);
            Assert.Equal(expected.LabelChars, actual.LabelChars);
            Assert.Equal(expected.Unicode, actual.Unicode);
            Assert.Equal(expected.Token, actual.Token);
        }

        [Fact]
        public void LoadDbCache_DiscoversACastThatKeepsTheColumnsOwnShape()
        {
            string connectionId = Forget.Oracle.Strategies.SqlDialectStrategy.Instance.GetConnectionId(_fixture.Connection);
            IDictionary<string, DbColumnInfo> columns = DbColumnInfoCache<CastRow>.GetDictValue(connectionId);

            // A precision or scale that the column does not declare must never be invented.
            Assert.Equal("CAST({} AS NUMBER)", columns["Plain"].CastExpression);
            Assert.Equal("CAST({} AS NUMBER(38,2))", columns["Scaled"].CastExpression);
            Assert.Equal("CAST({} AS NUMBER(18,4))", columns["Exact"].CastExpression);

            // A length is the column's own only for character and binary types.
            Assert.Equal("CAST({} AS VARCHAR2(50))", columns["Label"].CastExpression);
            Assert.Equal("CAST({} AS RAW(16))", columns["Token"].CastExpression);

            // A fixed-width character type is padded to the cast's length, so that length has to be the column's own
            // in the column's own unit: DATA_LENGTH counts bytes, which for CHAR(5 CHAR) is 20 and for NCHAR(5) is 10.
            Assert.Equal("CAST({} AS CHAR(5 BYTE))", columns["Code"].CastExpression);
            Assert.Equal("CAST({} AS CHAR(5 CHAR))", columns["CharSemantic"].CastExpression);
            Assert.Equal("CAST({} AS NCHAR(5))", columns["National"].CastExpression);
        }

        [Fact]
        public async Task InsertRangeAsync_KeepsTheFractionalPartOfNumberColumnsWithoutPrecision()
        {
            CastRow a = new() { Id = 100, Plain = 12.75m, Scaled = 1.25m };
            CastRow b = new() { Id = 101, Plain = 0.5m, Scaled = 9.4m };

            await _fixture.Connection.InsertRangeAsync([a, b], cancellationToken: Ct);
            IReadOnlyList<CastRow?> fetched = await _fixture.Connection.GetByIdRangeAsync<CastRow>(new[] { 100, 101 }, cancellationToken: Ct);

            Assert.Equal(12.75m, fetched.Single(r => r?.Id == 100)!.Plain);
            Assert.Equal(1.25m, fetched.Single(r => r?.Id == 100)!.Scaled);
            Assert.Equal(0.5m, fetched.Single(r => r?.Id == 101)!.Plain);
            Assert.Equal(9.4m, fetched.Single(r => r?.Id == 101)!.Scaled);
        }

        [Fact]
        public async Task UpdateRangeAsync_KeepsTheFractionalPartOfNumberColumnsWithoutPrecision()
        {
            await _fixture.Connection.InsertRangeAsync([new CastRow { Id = 110, Plain = 1m, Scaled = 1m }], cancellationToken: Ct);

            await _fixture.Connection.UpdateRangeAsync([new CastRow { Id = 110, Plain = 12.75m, Scaled = 1.25m }], cancellationToken: Ct);
            CastRow? fetched = await _fixture.Connection.GetByIdAsync<CastRow>(110, cancellationToken: Ct);

            Assert.Equal(12.75m, fetched!.Plain);
            Assert.Equal(1.25m, fetched.Scaled);
        }

        [Fact]
        public async Task UpsertRangeAsync_KeepsTheFractionalPartOfNumberColumnsWithoutPrecision()
        {
            await _fixture.Connection.InsertRangeAsync([new CastRow { Id = 120, Plain = 1m, Scaled = 1m }], cancellationToken: Ct);

            await _fixture.Connection.UpsertRangeAsync([new CastRow { Id = 120, Plain = 12.75m, Scaled = 1.25m }, new CastRow { Id = 121, Plain = 0.5m, Scaled = 9.4m }], cancellationToken: Ct);
            IReadOnlyList<CastRow?> fetched = await _fixture.Connection.GetByIdRangeAsync<CastRow>(new[] { 120, 121 }, cancellationToken: Ct);

            Assert.Equal(12.75m, fetched.Single(r => r?.Id == 120)!.Plain);
            Assert.Equal(1.25m, fetched.Single(r => r?.Id == 120)!.Scaled);
            Assert.Equal(0.5m, fetched.Single(r => r?.Id == 121)!.Plain);
            Assert.Equal(9.4m, fetched.Single(r => r?.Id == 121)!.Scaled);
        }

        [Fact]
        public async Task InsertRangeAsync_RoundTripsEveryColumnKindEvenWhenTheFirstRowIsAllNull()
        {
            // The first row carries nothing but its key: without a cast on each column Oracle cannot infer the type
            // of the UNION ALL branches from it (ORA-01790).
            CastRow empty = new() { Id = 130 };
            CastRow first = Full(131, 12.75m, 1.25m);
            CastRow second = Full(132, 0.5m, 9.4m);

            await _fixture.Connection.InsertRangeAsync([empty, first, second], cancellationToken: Ct);
            IReadOnlyList<CastRow?> fetched = await _fixture.Connection.GetByIdRangeAsync<CastRow>(new[] { 130, 131, 132 }, cancellationToken: Ct);

            CastRow? emptyBack = fetched.Single(r => r?.Id == 130);
            Assert.Null(emptyBack!.Plain);
            Assert.Null(emptyBack.Body);
            Assert.Null(emptyBack.Payload);
            Assert.Null(emptyBack.Token);
            AssertSame(first, fetched.Single(r => r?.Id == 131));
            AssertSame(second, fetched.Single(r => r?.Id == 132));
        }
    }
}
