using Forget.Core.Models;
using Forget.Oracle.Extensions;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// <c>IgnoreCase</c> lowers both sides of a string comparison, so it must give the same answer on an engine that
    /// compares case-sensitively and on one that does not. Until now only the generated SQL was checked.
    /// </summary>
    [Collection(OracleCollection.Name)]
    public class FilterCaseIntegrationTests
    {
        private readonly OracleFixture _fixture;

        public FilterCaseIntegrationTests(OracleFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static Widget NewWidget(int id, string name) => new() { Id = id, Name = name, IsActive = 1, Price = 1m };

        private async Task<int[]> IdsAsync(FilterDescriptor<Widget> filter)
        {
            if (await _fixture.Connection.GetByIdAsync<Widget>(800, cancellationToken: Ct) is null)
                await _fixture.Connection.InsertRangeAsync([NewWidget(800, "Mixed-Case-Alpha"), NewWidget(801, "mixed-case-BETA"), NewWidget(802, "OTHER")], cancellationToken: Ct);

            IReadOnlyList<Widget> rows = await _fixture.Connection.GetAllAsync(filter, cancellationToken: Ct);

            return [.. rows.Select(w => w.Id).Where(id => id is >= 800 and <= 802).Order()];
        }

        [Fact]
        public async Task Equal_IgnoringCase_MatchesWhateverTheCase()
        {
            Assert.Equal(new[] { 800 }, await IdsAsync(new("Name", "MIXED-CASE-ALPHA", ComparisonOperator.Equal, ignoreCase: true)));
        }

        [Fact]
        public async Task ContainsStartsWithAndEndsWith_IgnoringCase_MatchWhateverTheCase()
        {
            Assert.Equal(new[] { 800, 801 }, await IdsAsync(new("Name", "CASE-", ComparisonOperator.Contains, ignoreCase: true)));
            Assert.Equal(new[] { 800, 801 }, await IdsAsync(new("Name", "MIXED", ComparisonOperator.StartsWith, ignoreCase: true)));
            Assert.Equal(new[] { 801 }, await IdsAsync(new("Name", "beta", ComparisonOperator.EndsWith, ignoreCase: true)));
        }

        [Fact]
        public async Task NotEqualAndNot_IgnoringCase_ExcludeWhateverTheCase()
        {
            Assert.Equal(new[] { 801, 802 }, await IdsAsync(new("Name", "mixed-case-alpha", ComparisonOperator.NotEqual, ignoreCase: true)));
            Assert.Equal(new[] { 800, 801 }, await IdsAsync(new("Name", "other", ComparisonOperator.Equal, not: true, ignoreCase: true)));
        }
    }
}