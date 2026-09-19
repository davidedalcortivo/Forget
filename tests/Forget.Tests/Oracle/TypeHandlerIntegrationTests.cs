using Dapper;
using Forget.Oracle.Extensions;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// Proves Forget stays compatible with a Dapper <see cref="SqlMapper.TypeHandler{T}"/> instead of mapping
    /// types itself: a custom type (<see cref="OracleToken"/>, stored as <c>RAW(16)</c>) works through single-row
    /// and multi-row writes (the multi-row path is Forget's own <c>UNION ALL</c>/cast-discovery code), reads,
    /// updates, and equality filters, with nothing but the handler Dapper documents for that.
    /// <para>
    /// Not covered on purpose: <c>list.Contains(r.Token)</c>. Dapper's own <c>IN</c> list expansion does not run
    /// type handlers on the elements (a plain Dapper <c>WHERE "Token" IN :ids</c> fails with the same
    /// <see cref="ArgumentException"/>), so Forget inherits that limit rather than papering over it.
    /// </para>
    /// </summary>
    [Collection(OracleCollection.Name)]
    public class TypeHandlerIntegrationTests
    {
        private readonly OracleFixture _fixture;

        static TypeHandlerIntegrationTests()
        {
            SqlMapper.AddTypeHandler(new OracleTokenHandler());
        }

        public TypeHandlerIntegrationTests(OracleFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static OracleToken NewToken() => new(Guid.NewGuid());

        [Fact]
        public async Task InsertAsync_ThenGetByIdAsync_RoundTripsThroughTheTypeHandler()
        {
            HandlerRow row = new() { Id = 1, Token = NewToken(), Note = "single" };

            await _fixture.Connection.InsertAsync(row, cancellationToken: Ct);
            HandlerRow? fetched = await _fixture.Connection.GetByIdAsync<HandlerRow>(1, cancellationToken: Ct);

            Assert.NotNull(fetched);
            Assert.Equal(row.Token, fetched!.Token);
            Assert.Equal("single", fetched.Note);
        }

        [Fact]
        public async Task InsertRangeAsync_RoundTripsThroughTheTypeHandlerOnTheMultiRowStatement()
        {
            HandlerRow a = new() { Id = 10, Token = NewToken(), Note = "a" };
            HandlerRow b = new() { Id = 11, Token = NewToken(), Note = null };
            HandlerRow c = new() { Id = 12, Token = NewToken(), Note = "c" };

            int affected = await _fixture.Connection.InsertRangeAsync([a, b, c], cancellationToken: Ct);
            IReadOnlyList<HandlerRow?> fetched = await _fixture.Connection.GetByIdRangeAsync<HandlerRow>(new[] { 10, 11, 12 }, cancellationToken: Ct);

            Assert.Equal(3, affected);
            foreach (HandlerRow expected in new[] { a, b, c })
            {
                HandlerRow? actual = fetched.Single(r => r?.Id == expected.Id);
                Assert.Equal(expected.Token, actual!.Token);
                Assert.Equal(expected.Note, actual.Note);
            }
        }

        [Fact]
        public async Task UpdateAsync_And_UpdateRangeAsync_WriteTheNewValueThroughTheTypeHandler()
        {
            HandlerRow single = new() { Id = 20, Token = NewToken() };
            HandlerRow ranged = new() { Id = 21, Token = NewToken() };
            await _fixture.Connection.InsertRangeAsync([single, ranged], cancellationToken: Ct);

            single.Token = NewToken();
            ranged.Token = NewToken();
            await _fixture.Connection.UpdateAsync(single, cancellationToken: Ct);
            await _fixture.Connection.UpdateRangeAsync([ranged], cancellationToken: Ct);

            Assert.Equal(single.Token, (await _fixture.Connection.GetByIdAsync<HandlerRow>(20, cancellationToken: Ct))!.Token);
            Assert.Equal(ranged.Token, (await _fixture.Connection.GetByIdAsync<HandlerRow>(21, cancellationToken: Ct))!.Token);
        }

        [Fact]
        public async Task GetAllAsync_EqualityFilterOnTheHandledType_BindsThroughTheTypeHandler()
        {
            HandlerRow a = new() { Id = 30, Token = NewToken() };
            HandlerRow b = new() { Id = 31, Token = NewToken() };
            await _fixture.Connection.InsertRangeAsync([a, b], cancellationToken: Ct);

            OracleToken wanted = b.Token;

            IReadOnlyList<HandlerRow> equal = await _fixture.Connection.GetAllAsync<HandlerRow>(r => r.Token == wanted, cancellationToken: Ct);

            Assert.Equal([31], equal.Select(r => r.Id).ToList());
        }

    }
}
