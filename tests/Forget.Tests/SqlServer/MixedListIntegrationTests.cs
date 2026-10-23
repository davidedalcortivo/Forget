using Forget.Core.Models;
using Forget.SqlServer.Extensions;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// A list of mixed types (an <c>int</c> and a <c>long</c> for an <c>int</c> property, accepted since 2.1.0) cannot be
    /// one array. Dapper expands it into one parameter per element, except where the driver binds a list as an array
    /// (Npgsql): there Forget writes it out itself, or PostgreSQL would receive <c>IN @p</c> with an <c>object[]</c>.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public class MixedListIntegrationTests
    {
        private readonly SqlServerFixture _fixture;

        public MixedListIntegrationTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        [Fact]
        public async Task AnInFilterWithAListOfMixedTypes_ReturnsTheRows()
        {
            await _fixture.Connection.InsertRangeAsync(new[] { 720, 721, 722 }.Select(id => new ParameterNameRow { Id = id, Address = $"A{id}", Address2 = $"B{id}" }), cancellationToken: Ct);
            FilterDescriptor<ParameterNameRow> filter = new("Id", new object[] { 720, 722L }, ComparisonOperator.In);

            IReadOnlyList<ParameterNameRow> found = await _fixture.Connection.GetAllAsync(filter, cancellationToken: Ct);

            Assert.Equal([720, 722], found.Select(x => x.Id).Order());
        }
    }
}
