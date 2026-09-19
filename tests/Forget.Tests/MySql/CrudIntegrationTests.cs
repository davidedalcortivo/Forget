using Forget.MySql.Extensions;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// Runs the public CRUD/filter/upsert API against a real, disposable MySql container. Unlike
    /// <see cref="SqlBuilderStrategyTests"/> (which locks the SQL text Forge generates), these tests prove that
    /// text actually executes correctly against a live engine — real NULL semantics, real <c>LIKE</c> escaping,
    /// real <c>ON DUPLICATE KEY UPDATE</c> behavior.
    /// <para>
    /// All tests share one container and table (started once by <see cref="MySqlFixture"/>) and stay isolated
    /// from each other by using a disjoint range of <c>Id</c> values per test, rather than paying for a fresh
    /// container per test.
    /// </para>
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class CrudIntegrationTests
    {
        private readonly MySqlFixture _fixture;

        public CrudIntegrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task InsertAsync_ThenGetByIdAsync_RoundTripsAllColumns()
        {
            Widget widget = new() { Id = 1, Name = "Widget One", Nickname = "w1", Quantity = 5, IsActive = true, Price = 12.34m };

            int affected = await _fixture.Connection.InsertAsync(widget, cancellationToken: TestContext.Current.CancellationToken);
            Widget? fetched = await _fixture.Connection.GetByIdAsync<Widget>(1, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, affected);
            Assert.NotNull(fetched);
            Assert.Equal(widget.Id, fetched!.Id);
            Assert.Equal(widget.Name, fetched.Name);
            Assert.Equal(widget.Nickname, fetched.Nickname);
            Assert.Equal(widget.Quantity, fetched.Quantity);
            Assert.Equal(widget.IsActive, fetched.IsActive);
            Assert.Equal(widget.Price, fetched.Price);
        }

        [Fact]
        public async Task GetByIdAsync_WhenRowDoesNotExist_ReturnsNull()
        {
            Widget? fetched = await _fixture.Connection.GetByIdAsync<Widget>(999_999, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Null(fetched);
        }

        [Fact]
        public async Task GetAllAsync_WithPredicate_ReturnsOnlyMatchingRows()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 10, Name = "Active High", Quantity = 5, IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 11, Name = "Active Low", Quantity = 1, IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 12, Name = "Inactive High", Quantity = 5, IsActive = false, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 13, Name = "No Quantity", Quantity = null, IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> matches = await _fixture.Connection.GetAllAsync<Widget>(w => w.IsActive && w.Quantity > 2, cancellationToken: TestContext.Current.CancellationToken);

            Widget matched = Assert.Single(matches, w => w.Id is 10 or 11 or 12 or 13);
            Assert.Equal(10, matched.Id);
        }

        [Fact]
        public async Task GetAllAsync_EqualsNullPredicate_MatchesRowsWithNullColumn()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 14, Name = "Has nickname", Nickname = "nick", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 15, Name = "No nickname", Nickname = null, IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> matches = await _fixture.Connection.GetAllAsync<Widget>(w => w.Nickname == null, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Contains(matches, w => w.Id == 15);
            Assert.DoesNotContain(matches, w => w.Id == 14);
        }

        [Fact]
        public async Task GetAllAsync_ContainsFilter_TreatsPercentAndUnderscoreAsLiteralCharacters()
        {
            // If EscapeLike didn't actually work against a real LIKE, "100%" would match anything ("%" is a
            // wildcard), and "a_b" would match "aXb" ("_" matches any single character).
            await _fixture.Connection.InsertAsync(new Widget { Id = 20, Name = "Discount 100% today", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 21, Name = "Discount 100X today", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 22, Name = "a_b widget", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 23, Name = "aXb widget", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> percentMatches = await _fixture.Connection.GetAllAsync<Widget>(w => w.Name.Contains("100%"), cancellationToken: TestContext.Current.CancellationToken);
            IReadOnlyList<Widget> underscoreMatches = await _fixture.Connection.GetAllAsync<Widget>(w => w.Name.Contains("a_b"), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal([20], percentMatches.Where(w => w.Id is 20 or 21).Select(w => w.Id));
            Assert.Equal([22], underscoreMatches.Where(w => w.Id is 22 or 23).Select(w => w.Id));
        }

        [Fact]
        public async Task GetAllAsync_ContainsFilter_MatchesValueContainingAnApostrophe()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 24, Name = "O'Brien's Widget", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> matches = await _fixture.Connection.GetAllAsync<Widget>(w => w.Name.Contains("O'Brien"), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal([24], matches.Where(w => w.Id == 24).Select(w => w.Id));
        }

        [Fact]
        public async Task GetAllAsync_CollectionContainsFilter_UsesInWithExpandedParameters()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 60, Name = "W60", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 61, Name = "W61", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 62, Name = "W62", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> matches = await _fixture.Connection.GetAllAsync<Widget>(w => new[] { 60, 62 }.Contains(w.Id), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal([60, 62], matches.Where(w => w.Id is 60 or 61 or 62).Select(w => w.Id).OrderBy(id => id));
        }

        [Fact]
        public async Task UpdateAsync_PersistsChangedColumns()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 30, Name = "Before", Quantity = 1, IsActive = false, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            Widget updated = new() { Id = 30, Name = "After", Nickname = "after-nick", Quantity = 9, IsActive = true, Price = 99.99m };
            int affected = await _fixture.Connection.UpdateAsync(updated, cancellationToken: TestContext.Current.CancellationToken);
            Widget? fetched = await _fixture.Connection.GetByIdAsync<Widget>(30, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, affected);
            Assert.NotNull(fetched);
            Assert.Equal("After", fetched!.Name);
            Assert.Equal("after-nick", fetched.Nickname);
            Assert.Equal(9, fetched.Quantity);
            Assert.True(fetched.IsActive);
            Assert.Equal(99.99m, fetched.Price);
        }

        [Fact]
        public async Task UpsertAsync_InsertsWhenMissingThenUpdatesOnDuplicateKey()
        {
            Widget first = new() { Id = 40, Name = "First insert", Quantity = 1, IsActive = false, Price = 1m };
            int firstAffected = await _fixture.Connection.UpsertAsync(first, cancellationToken: TestContext.Current.CancellationToken);
            Widget? afterInsert = await _fixture.Connection.GetByIdAsync<Widget>(40, cancellationToken: TestContext.Current.CancellationToken);

            Widget second = new() { Id = 40, Name = "Upserted", Quantity = 2, IsActive = true, Price = 2m };
            int secondAffected = await _fixture.Connection.UpsertAsync(second, cancellationToken: TestContext.Current.CancellationToken);
            Widget? afterUpsert = await _fixture.Connection.GetByIdAsync<Widget>(40, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, firstAffected);
            Assert.Equal("First insert", afterInsert!.Name);

            Assert.Equal("Upserted", afterUpsert!.Name);
            Assert.Equal(2, afterUpsert.Quantity);
            Assert.True(afterUpsert.IsActive);
            _ = secondAffected; // MySql's ON DUPLICATE KEY UPDATE reports 2 affected rows for an updated row, not 1.
        }

        [Fact]
        public async Task DeleteAsync_RemovesRow()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 50, Name = "To delete", IsActive = true, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            int affected = await _fixture.Connection.DeleteAsync<Widget>(50, cancellationToken: TestContext.Current.CancellationToken);
            Widget? fetched = await _fixture.Connection.GetByIdAsync<Widget>(50, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, affected);
            Assert.Null(fetched);
        }
    }
}
