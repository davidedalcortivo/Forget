using Forget.Oracle.Extensions;


namespace Forget.Tests.Oracle
{
    /// <summary>
    /// Runs the public CRUD/filter/upsert/range API against a real, disposable Oracle container. Unlike
    /// <see cref="SqlBuilderStrategyTests"/> and <see cref="InsertRangeCommandsTests"/> (which lock the SQL text
    /// Forge generates, with hand-seeded column-cast metadata), these tests prove that text actually executes
    /// correctly against a live engine — including the column-cast PL/SQL block discovering real cast expressions
    /// from <c>ALL_TAB_COLUMNS</c> and those expressions actually avoiding <c>ORA-01790</c> on the real
    /// <c>UNION ALL</c>/<c>DUAL</c> multi-row insert.
    /// <para>
    /// <c>IsActive</c> is <c>int</c> (0/1), not <c>bool</c>, here — see <see cref="Widget"/> for why.
    /// </para>
    /// <para>
    /// All tests share one container and table (started once by <see cref="OracleFixture"/>, which also runs the
    /// real <c>LoadDbCacheAsync</c>) and stay isolated from each other by using a disjoint range of <c>Id</c>
    /// values per test, rather than paying for a fresh container per test.
    /// </para>
    /// </summary>
    [Collection(OracleCollection.Name)]
    public class CrudIntegrationTests
    {
        private readonly OracleFixture _fixture;

        public CrudIntegrationTests(OracleFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task InsertAsync_ThenGetByIdAsync_RoundTripsAllColumns()
        {
            Widget widget = new() { Id = 1, Name = "Widget One", Nickname = "w1", Quantity = 5, IsActive = 1, Price = 12.34m };

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
            await _fixture.Connection.InsertAsync(new Widget { Id = 10, Name = "Active High", Quantity = 5, IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 11, Name = "Active Low", Quantity = 1, IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 12, Name = "Inactive High", Quantity = 5, IsActive = 0, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 13, Name = "No Quantity", Quantity = null, IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> matches = await _fixture.Connection.GetAllAsync<Widget>(w => w.IsActive == 1 && w.Quantity > 2, cancellationToken: TestContext.Current.CancellationToken);

            Widget matched = Assert.Single(matches, w => w.Id is 10 or 11 or 12 or 13);
            Assert.Equal(10, matched.Id);
        }

        [Fact]
        public async Task GetAllAsync_EqualsNullPredicate_MatchesRowsWithNullColumn()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 14, Name = "Has nickname", Nickname = "nick", IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 15, Name = "No nickname", Nickname = null, IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> matches = await _fixture.Connection.GetAllAsync<Widget>(w => w.Nickname == null, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Contains(matches, w => w.Id == 15);
            Assert.DoesNotContain(matches, w => w.Id == 14);
        }

        [Fact]
        public async Task GetAllAsync_ContainsFilter_TreatsPercentAndUnderscoreAsLiteralCharacters()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 20, Name = "Discount 100% today", IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 21, Name = "Discount 100X today", IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 22, Name = "a_b widget", IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 23, Name = "aXb widget", IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> percentMatches = await _fixture.Connection.GetAllAsync<Widget>(w => w.Name.Contains("100%"), cancellationToken: TestContext.Current.CancellationToken);
            IReadOnlyList<Widget> underscoreMatches = await _fixture.Connection.GetAllAsync<Widget>(w => w.Name.Contains("a_b"), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal([20], percentMatches.Where(w => w.Id is 20 or 21).Select(w => w.Id));
            Assert.Equal([22], underscoreMatches.Where(w => w.Id is 22 or 23).Select(w => w.Id));
        }

        [Fact]
        public async Task GetAllAsync_ContainsFilter_MatchesValueContainingAnApostrophe()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 24, Name = "O'Brien's Widget", IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> matches = await _fixture.Connection.GetAllAsync<Widget>(w => w.Name.Contains("O'Brien"), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal([24], matches.Where(w => w.Id == 24).Select(w => w.Id));
        }

        [Fact]
        public async Task GetAllAsync_CollectionContainsFilter_UsesInWithExpandedParameters()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 60, Name = "W60", IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 61, Name = "W61", IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);
            await _fixture.Connection.InsertAsync(new Widget { Id = 62, Name = "W62", IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            IReadOnlyList<Widget> matches = await _fixture.Connection.GetAllAsync<Widget>(w => new[] { 60, 62 }.Contains(w.Id), cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal([60, 62], matches.Where(w => w.Id is 60 or 61 or 62).Select(w => w.Id).OrderBy(id => id));
        }

        [Fact]
        public async Task UpdateAsync_PersistsChangedColumns()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 30, Name = "Before", Quantity = 1, IsActive = 0, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            Widget updated = new() { Id = 30, Name = "After", Nickname = "after-nick", Quantity = 9, IsActive = 1, Price = 99.99m };
            int affected = await _fixture.Connection.UpdateAsync(updated, cancellationToken: TestContext.Current.CancellationToken);
            Widget? fetched = await _fixture.Connection.GetByIdAsync<Widget>(30, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, affected);
            Assert.NotNull(fetched);
            Assert.Equal("After", fetched!.Name);
            Assert.Equal("after-nick", fetched.Nickname);
            Assert.Equal(9, fetched.Quantity);
            Assert.Equal(1, fetched.IsActive);
            Assert.Equal(99.99m, fetched.Price);
        }

        [Fact]
        public async Task UpsertAsync_InsertsWhenMissingThenUpdatesOnMerge()
        {
            Widget first = new() { Id = 40, Name = "First insert", Quantity = 1, IsActive = 0, Price = 1m };
            int firstAffected = await _fixture.Connection.UpsertAsync(first, cancellationToken: TestContext.Current.CancellationToken);
            Widget? afterInsert = await _fixture.Connection.GetByIdAsync<Widget>(40, cancellationToken: TestContext.Current.CancellationToken);

            Widget second = new() { Id = 40, Name = "Upserted", Quantity = 2, IsActive = 1, Price = 2m };
            int secondAffected = await _fixture.Connection.UpsertAsync(second, cancellationToken: TestContext.Current.CancellationToken);
            Widget? afterUpsert = await _fixture.Connection.GetByIdAsync<Widget>(40, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, firstAffected);
            Assert.Equal("First insert", afterInsert!.Name);

            Assert.Equal(1, secondAffected);
            Assert.Equal("Upserted", afterUpsert!.Name);
            Assert.Equal(2, afterUpsert.Quantity);
            Assert.Equal(1, afterUpsert.IsActive);
        }

        [Fact]
        public async Task DeleteAsync_RemovesRow()
        {
            await _fixture.Connection.InsertAsync(new Widget { Id = 50, Name = "To delete", IsActive = 1, Price = 1m }, cancellationToken: TestContext.Current.CancellationToken);

            int affected = await _fixture.Connection.DeleteAsync<Widget>(50, cancellationToken: TestContext.Current.CancellationToken);
            Widget? fetched = await _fixture.Connection.GetByIdAsync<Widget>(50, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(1, affected);
            Assert.Null(fetched);
        }

        [Fact]
        public async Task InsertRangeAsync_UsesRealDiscoveredCastExpressionsWithoutOra01790()
        {
            // This is the actual point of the whole DUAL/UNION ALL/CAST mechanism: OracleFixture already ran the
            // real PL/SQL column-discovery block against this schema (not hand-seeded metadata), so if the cast
            // expressions it found are wrong for any of these column types, this insert throws ORA-01790.
            Widget[] widgets =
            [
                new() { Id = 70, Name = "Batch A", Nickname = null, Quantity = 5, IsActive = 1, Price = 9.99m },
                new() { Id = 71, Name = "Batch B", Nickname = "b-nick", Quantity = null, IsActive = 0, Price = 19.99m },
                new() { Id = 72, Name = "Batch C", Nickname = "c-nick", Quantity = 7, IsActive = 1, Price = 29.99m },
            ];

            int affected = await _fixture.Connection.InsertRangeAsync(widgets, batchSize: 2, cancellationToken: TestContext.Current.CancellationToken);
            IReadOnlyList<Widget> fetched = await _fixture.Connection.GetAllAsync<Widget>(w => w.Id >= 70 && w.Id <= 72, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(3, affected);
            Assert.Equal(3, fetched.Count);
            Assert.Contains(fetched, w => w.Id == 70 && w.Name == "Batch A" && w.Nickname == null && w.Quantity == 5 && w.IsActive == 1);
            Assert.Contains(fetched, w => w.Id == 71 && w.Name == "Batch B" && w.Nickname == "b-nick" && w.Quantity == null && w.IsActive == 0);
            Assert.Contains(fetched, w => w.Id == 72 && w.Name == "Batch C" && w.Price == 29.99m);
        }

        [Fact]
        public async Task UpsertRangeAsync_InsertsNewRowsThenUpdatesExistingOnesViaMerge()
        {
            Widget[] initial =
            [
                new() { Id = 80, Name = "Initial 80", IsActive = 1, Price = 1m },
                new() { Id = 81, Name = "Initial 81", IsActive = 1, Price = 1m },
            ];
            await _fixture.Connection.UpsertRangeAsync(initial, cancellationToken: TestContext.Current.CancellationToken);

            Widget[] mixed =
            [
                new() { Id = 80, Name = "Updated 80", IsActive = 0, Price = 2m },
                new() { Id = 82, Name = "New 82", IsActive = 1, Price = 3m },
            ];
            await _fixture.Connection.UpsertRangeAsync(mixed, cancellationToken: TestContext.Current.CancellationToken);

            Widget? row80 = await _fixture.Connection.GetByIdAsync<Widget>(80, cancellationToken: TestContext.Current.CancellationToken);
            Widget? row81 = await _fixture.Connection.GetByIdAsync<Widget>(81, cancellationToken: TestContext.Current.CancellationToken);
            Widget? row82 = await _fixture.Connection.GetByIdAsync<Widget>(82, cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal("Updated 80", row80!.Name);
            Assert.Equal(0, row80.IsActive);
            Assert.Equal("Initial 81", row81!.Name);
            Assert.Equal("New 82", row82!.Name);
        }
    }
}
