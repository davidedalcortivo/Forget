using Dapper;
using Forget.Core.Models;
using Forget.SqlServer.Extensions;


namespace Forget.Tests.SqlServer
{
    /// <summary>
    /// Every <c>*RangeCommands</c> method documents <c>batchSize</c> as capped according to SQL Server's limit of roughly
    /// 2,100 parameters per command, the same as the <c>*Range</c>/<c>*RangeAsync</c> methods that execute it. Some of them
    /// capped only the size of each chunk within a command and left <c>batchSize</c> itself unbounded, so a command could
    /// combine several chunks and still exceed the limit. These tests build the commands and execute them for real, because
    /// the bug is only visible then: the commands themselves look fine, and only SQL Server's own count of bound parameters
    /// catches it.
    /// </summary>
    [Collection(SqlServerCollection.Name)]
    public class RangeCommandsParameterLimitTests
    {
        private readonly SqlServerFixture _fixture;

        public RangeCommandsParameterLimitTests(SqlServerFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static Widget NewWidget(int id) => new() { Id = id, Name = "limit", IsActive = true, Price = 1m };

        private static int Execute(IReadOnlyList<DbCommandInfo> commands, SqlServerFixture fixture)
        {
            int total = 0;

            foreach (DbCommandInfo command in commands)
            {
                total += fixture.Connection.Execute(command.Sql, command.Parameters);
            }

            return total;
        }

        [Fact]
        public async Task DeleteRangeCommands_WithIdsAndABatchSizeAboveTheParameterLimit_BuildsCommandsThatCanBeExecuted()
        {
            Widget[] widgets = [.. Enumerable.Range(860_000, 2_500).Select(NewWidget)];
            await _fixture.Connection.InsertRangeAsync(widgets, cancellationToken: Ct);
            int[] ids = [.. widgets.Select(w => w.Id)];

            IReadOnlyList<DbCommandInfo> commands = _fixture.Connection.DeleteRangeCommands<Widget>(ids, batchSize: 5_000);
            int deleted = Execute(commands, _fixture);

            Assert.Equal(2_500, deleted);
        }

        [Fact]
        public async Task DeleteRangeCommands_TypedWithIdsAndABatchSizeAboveTheParameterLimit_BuildsCommandsThatCanBeExecuted()
        {
            Widget[] widgets = [.. Enumerable.Range(870_000, 2_500).Select(NewWidget)];
            await _fixture.Connection.InsertRangeAsync(widgets, cancellationToken: Ct);
            int[] ids = [.. widgets.Select(w => w.Id)];

            IReadOnlyList<DbCommandInfo> commands = _fixture.Connection.DeleteRangeCommands<Widget, int>(ids, batchSize: 5_000);
            int deleted = Execute(commands, _fixture);

            Assert.Equal(2_500, deleted);
        }

        [Fact]
        public async Task DeleteRangeCommands_WithEntitiesAndABatchSizeAboveTheParameterLimit_BuildsCommandsThatCanBeExecuted()
        {
            Widget[] widgets = [.. Enumerable.Range(880_000, 2_500).Select(NewWidget)];
            await _fixture.Connection.InsertRangeAsync(widgets, cancellationToken: Ct);

            IReadOnlyList<DbCommandInfo> commands = _fixture.Connection.DeleteRangeCommands(widgets, batchSize: 5_000);
            int deleted = Execute(commands, _fixture);

            Assert.Equal(2_500, deleted);
        }

        [Fact]
        public void InsertRangeCommands_WithABatchSizeAboveTheParameterLimit_BuildsCommandsThatCanBeExecuted()
        {
            Widget[] widgets = [.. Enumerable.Range(890_000, 2_500).Select(NewWidget)];

            IReadOnlyList<DbCommandInfo> commands = _fixture.Connection.InsertRangeCommands(widgets, batchSize: 5_000);
            int inserted = Execute(commands, _fixture);

            Assert.Equal(2_500, inserted);
        }

        [Fact]
        public void UpdateRangeCommands_WithABatchSizeAboveTheParameterLimit_BuildsCommandsThatCanBeExecuted()
        {
            Widget[] widgets = [.. Enumerable.Range(900_000, 2_500).Select(NewWidget)];
            _fixture.Connection.InsertRange(widgets);

            foreach (Widget widget in widgets)
            {
                widget.Name = "updated";
            }

            IReadOnlyList<DbCommandInfo> commands = _fixture.Connection.UpdateRangeCommands(widgets, batchSize: 5_000);
            int updated = Execute(commands, _fixture);

            Assert.Equal(2_500, updated);
        }

        [Fact]
        public void UpsertRangeCommands_WithABatchSizeAboveTheParameterLimit_BuildsCommandsThatCanBeExecuted()
        {
            Widget[] widgets = [.. Enumerable.Range(910_000, 2_500).Select(NewWidget)];

            IReadOnlyList<DbCommandInfo> commands = _fixture.Connection.UpsertRangeCommands(widgets, batchSize: 5_000);
            int upserted = Execute(commands, _fixture);

            Assert.Equal(2_500, upserted);
        }
    }
}
