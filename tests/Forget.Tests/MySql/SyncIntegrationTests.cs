using Dapper;
using Forget.Core.Models;
using Forget.MySql.Extensions;
using MySqlConnector;
using System.Data;
using System.Linq.Expressions;


namespace Forget.Tests.MySql
{
    /// <summary>
    /// Every operation exists in a synchronous and an asynchronous form, and they share the implementation behind a
    /// flag. The synchronous branches (opening and closing the connection, beginning and committing the transaction,
    /// loading the column metadata) are their own code, so they are executed here against the real engine, one test
    /// per family of operations. The filter-node overloads (<see cref="FilterDescriptor{TEntity}"/> and
    /// <see cref="FilterGroup{TEntity}"/>) are used where they fit, since they build their SQL through a different
    /// translator than the expression overloads.
    /// <para>
    /// Tests stay isolated on the shared container by using a disjoint <c>Id</c> range each.
    /// </para>
    /// </summary>
    [Collection(MySqlCollection.Name)]
    public class SyncIntegrationTests
    {
        private readonly MySqlFixture _fixture;

        public SyncIntegrationTests(MySqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static SyncWidget NewWidget(int id, string name = "sync", decimal price = 1m) => new() { Id = id, Name = name, IsActive = true, Price = price };

        private static FilterGroup<SyncWidget> IdBetween(int from, int to) => new(
        [
            new FilterDescriptor<SyncWidget>("Id", from, ComparisonOperator.GreaterThanOrEqual),
            new FilterDescriptor<SyncWidget>("Id", to, ComparisonOperator.LessThanOrEqual)
        ]);

        [Fact]
        public void Insert_GetById_Update_Delete_RoundTrip()
        {
            Assert.Equal(1, _fixture.Connection.Insert(NewWidget(300)));

            SyncWidget? fetched = _fixture.Connection.GetById<SyncWidget>(300);
            Assert.NotNull(fetched);
            Assert.Equal("sync", fetched!.Name);

            fetched.Name = "changed";
            Assert.Equal(1, _fixture.Connection.Update(fetched));
            Assert.Equal("changed", _fixture.Connection.GetById<SyncWidget>(300)!.Name);

            Assert.Equal(1, _fixture.Connection.Delete(fetched));
            Assert.Null(_fixture.Connection.GetById<SyncWidget>(300));

            _fixture.Connection.Insert(NewWidget(301));
            Assert.Equal(1, _fixture.Connection.Delete<SyncWidget>(301));
            Assert.Null(_fixture.Connection.GetById<SyncWidget>(301));
        }

        [Fact]
        public void RangeOperations_RoundTrip()
        {
            SyncWidget[] rows = [NewWidget(310, "a"), NewWidget(311, "b"), NewWidget(312, "c")];

            Assert.Equal(3, _fixture.Connection.InsertRange(rows));
            Assert.Equal(3, _fixture.Connection.GetByIdRange<SyncWidget>(new[] { 310, 311, 312 }).Count);

            foreach (SyncWidget row in rows)
                row.Name = "updated";

            Assert.Equal(3, _fixture.Connection.UpdateRange(rows));
            Assert.All(_fixture.Connection.GetByIdRange<SyncWidget>(new[] { 310, 311, 312 }), w => Assert.Equal("updated", w.Name));

            Assert.Equal(1, _fixture.Connection.DeleteRange<SyncWidget>(new[] { 310 }));
            Assert.Equal(2, _fixture.Connection.DeleteRange(rows[1..]));
            Assert.Empty(_fixture.Connection.GetByIdRange<SyncWidget>(new[] { 310, 311, 312 }));
        }

        [Fact]
        public void Upsert_And_UpsertRange_InsertThenUpdate()
        {
            _fixture.Connection.Upsert(NewWidget(320, "first"));
            _fixture.Connection.Upsert(NewWidget(320, "second"));
            Assert.Equal("second", _fixture.Connection.GetById<SyncWidget>(320)!.Name);

            _fixture.Connection.UpsertRange([NewWidget(320, "third"), NewWidget(321, "new")]);
            Assert.Equal("third", _fixture.Connection.GetById<SyncWidget>(320)!.Name);
            Assert.Equal("new", _fixture.Connection.GetById<SyncWidget>(321)!.Name);
        }

        [Fact]
        public void Retrieval_ByExpressionAndByFilter()
        {
            _fixture.Connection.InsertRange([.. Enumerable.Range(0, 5).Select(i => NewWidget(330 + i, $"R{i}"))]);
            Expression<Func<SyncWidget, bool>> inRange = w => w.Id >= 330 && w.Id <= 334;
            FilterGroup<SyncWidget> filter = IdBetween(330, 334);
            SortDescriptor<SyncWidget>[] ascending = [new(w => w.Id)];
            SortDescriptor<SyncWidget>[] descending = [new(w => w.Id, SortDirection.Descending)];

            Assert.Equal(5, _fixture.Connection.GetAll(inRange).Count);
            Assert.Equal(5, _fixture.Connection.GetAll(filter).Count);

            Assert.Equal(330, _fixture.Connection.GetFirst(inRange, sortDescriptors: ascending).Id);
            Assert.Equal(334, _fixture.Connection.GetFirst(filter, sortDescriptors: descending).Id);
            Assert.Null(_fixture.Connection.GetFirstOrDefault<SyncWidget>(w => w.Id == 999_301));
            Assert.Equal(330, _fixture.Connection.GetFirstOrDefault(filter, sortDescriptors: ascending)!.Id);

            Assert.Equal("R2", _fixture.Connection.GetSingle<SyncWidget>(w => w.Id == 332).Name);
            Assert.Equal("R3", _fixture.Connection.GetSingle(new FilterDescriptor<SyncWidget>("Id", 333)).Name);
            Assert.Null(_fixture.Connection.GetSingleOrDefault<SyncWidget>(w => w.Id == 999_302));
            Assert.Equal("R4", _fixture.Connection.GetSingleOrDefault(new FilterDescriptor<SyncWidget>("Id", 334))!.Name);

            Assert.Equal([331, 332], _fixture.Connection.GetPage(inRange, sortDescriptors: ascending, skip: 1, take: 2).Select(w => w.Id));
            Assert.Equal([333, 332], _fixture.Connection.GetPage(filter, sortDescriptors: descending, skip: 1, take: 2).Select(w => w.Id));
        }

        [Fact]
        public void ExistsCountAndAggregates_ByExpressionAndByFilter()
        {
            _fixture.Connection.InsertRange([NewWidget(340, price: 10m), NewWidget(341, price: 20m), NewWidget(342, price: 30m)]);
            Expression<Func<SyncWidget, bool>> inRange = w => w.Id >= 340 && w.Id <= 342;
            FilterGroup<SyncWidget> filter = IdBetween(340, 342);

            Assert.True(_fixture.Connection.Exists(inRange));
            Assert.True(_fixture.Connection.Exists(filter));
            Assert.False(_fixture.Connection.Exists<SyncWidget>(w => w.Id == 999_303));

            Assert.Equal(3, _fixture.Connection.Count(inRange));
            Assert.Equal(3, _fixture.Connection.Count(filter));

            Assert.Equal(60m, _fixture.Connection.Sum<SyncWidget>(w => w.Price, inRange));
            Assert.Equal(60m, _fixture.Connection.Sum<SyncWidget>(w => w.Price, filter));
            Assert.Equal(20m, _fixture.Connection.Avg<SyncWidget>(w => w.Price, inRange));
            Assert.Equal(20m, _fixture.Connection.Avg<SyncWidget>(w => w.Price, filter));
            Assert.Equal(10m, _fixture.Connection.Min<SyncWidget, decimal>(w => w.Price, inRange));
            Assert.Equal(10m, _fixture.Connection.Min<SyncWidget, decimal>(w => w.Price, filter));
            Assert.Equal(30m, _fixture.Connection.Max<SyncWidget, decimal>(w => w.Price, inRange));
            Assert.Equal(30m, _fixture.Connection.Max<SyncWidget, decimal>(w => w.Price, filter));
        }

        [Fact]
        public void BulkUpdateAndDelete_ByExpressionAndByFilter()
        {
            _fixture.Connection.InsertRange([.. Enumerable.Range(0, 4).Select(i => new SyncWidget { Id = 350 + i, Name = "Bulk", IsActive = false, Price = 1m })]);

            Assert.Equal(1, _fixture.Connection.Update<SyncWidget>(new { IsActive = true }, w => w.Id == 350));
            Assert.Equal(1, _fixture.Connection.Update<SyncWidget>(new { IsActive = true }, new FilterDescriptor<SyncWidget>("Id", 351)));
            Assert.True(_fixture.Connection.GetById<SyncWidget>(350)!.IsActive);
            Assert.True(_fixture.Connection.GetById<SyncWidget>(351)!.IsActive);
            Assert.False(_fixture.Connection.GetById<SyncWidget>(352)!.IsActive);

            Assert.Equal(1, _fixture.Connection.Delete<SyncWidget>(w => w.Id == 350));
            Assert.Equal(1, _fixture.Connection.Delete<SyncWidget>(new FilterDescriptor<SyncWidget>("Id", 351)));
            Assert.Null(_fixture.Connection.GetById<SyncWidget>(350));
            Assert.Null(_fixture.Connection.GetById<SyncWidget>(351));
            Assert.NotNull(_fixture.Connection.GetById<SyncWidget>(352));
        }

        [Fact]
        public void OnAClosedConnection_EveryOperationOpensItAndLeavesItClosed()
        {
            using MySqlConnection connection = new(_fixture.ConnectionString);
            Assert.Equal(ConnectionState.Closed, connection.State);

            connection.LoadDbCache<SyncWidget>();
            Assert.Equal(ConnectionState.Closed, connection.State);

            Assert.Equal(2, connection.InsertRange([NewWidget(360, "closed"), NewWidget(361, "closed")]));
            Assert.Equal(ConnectionState.Closed, connection.State);

            Assert.Equal("closed", connection.GetById<SyncWidget>(360)!.Name);
            Assert.Equal(ConnectionState.Closed, connection.State);

            connection.Upsert(NewWidget(360, "upserted"));
            Assert.Equal(ConnectionState.Closed, connection.State);
            Assert.Equal("upserted", connection.GetById<SyncWidget>(360)!.Name);

            Assert.Equal(2, connection.DeleteRange<SyncWidget>(new[] { 360, 361 }));
            Assert.Equal(ConnectionState.Closed, connection.State);
        }

        [Fact]
        public void CommandBuilders_ProduceStatementsThatRunThroughDapper()
        {
            MySqlConnection connection = _fixture.Connection;

            DbCommandInfo insert = connection.InsertCommand(NewWidget(370, "built"));
            Assert.Equal(1, connection.Execute(insert.Sql, insert.Parameters));

            DbCommandInfo select = connection.GetByIdCommand<SyncWidget>(370);
            Assert.Equal("built", connection.QuerySingle<SyncWidget>(select.Sql, select.Parameters).Name);

            DbCommandInfo count = connection.CountCommand<SyncWidget>(w => w.Id == 370);
            Assert.Equal(1, connection.ExecuteScalar<long>(count.Sql, count.Parameters));

            SyncWidget changed = NewWidget(370, "rebuilt");
            DbCommandInfo update = connection.UpdateCommand(changed);
            Assert.Equal(1, connection.Execute(update.Sql, update.Parameters));
            Assert.Equal("rebuilt", connection.GetById<SyncWidget>(370)!.Name);

            DbCommandInfo delete = connection.DeleteCommand<SyncWidget>(370);
            Assert.Equal(1, connection.Execute(delete.Sql, delete.Parameters));
            Assert.Null(connection.GetById<SyncWidget>(370));
        }
    }
}
