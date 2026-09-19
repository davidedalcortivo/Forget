using Forget.Core.Models;
using Forget.PostgreSql.Extensions;
using Forget.Tests.Core;


namespace Forget.Tests.PostgreSql
{
    /// <summary>
    /// Proves every CLR type in <see cref="TypeMatrixRow"/> survives a real PostgreSql round trip through the
    /// public API: single and multi-row insert/update/upsert, filtering on each type (including enum and
    /// <see cref="Guid"/> lists inside <c>IN</c>), and aggregates. <see cref="Widget"/> only covers int, string,
    /// bool and decimal; this is where date/time, <see cref="Guid"/>, enum, <see cref="byte"/>[], floating point
    /// and the wide integers get exercised against the engine itself instead of assumed.
    /// <para>
    /// Tests stay isolated on one shared container by using a disjoint <c>Id</c> range each.
    /// </para>
    /// </summary>
    [Collection(PostgreSqlCollection.Name)]
    public class TypeMatrixIntegrationTests
    {
        private readonly PostgreSqlFixture _fixture;

        public TypeMatrixIntegrationTests(PostgreSqlFixture fixture)
        {
            _fixture = fixture;
        }

        private static CancellationToken Ct => TestContext.Current.CancellationToken;

        private static TypeMatrixRow Sample(int id) => new()
        {
            Id = id,
            BigValue = 9_007_199_254_740_993L, // 2^53 + 1: not exactly representable as a double
            SmallValue = -32_000,
            DoubleValue = 3.141592653589793,
            SingleValue = 1.1f,
            DecimalValue = 123456789.1234567891m,
            Flag = true,
            Label = "Ünïcödé 日本語 🚀 100% a_b",
            Moment = new DateTime(2026, 9, 19, 12, 34, 56, DateTimeKind.Unspecified).AddTicks(1_234_560),
            OffsetMoment = new DateTimeOffset(2026, 9, 19, 12, 34, 56, TimeSpan.Zero).AddTicks(1_234_560),
            CalendarDay = new DateOnly(2026, 9, 19),
            TimeOfDay = new TimeOnly(12, 34, 56).Add(TimeSpan.FromTicks(1_234_560)),
            Identifier = Guid.NewGuid(),
            Kind = TypeMatrixKind.Beta,
            Payload = [0x00, 0x01, 0xFE, 0xFF],
            NullableBig = long.MinValue + 1,
            NullableMoment = new DateTime(1999, 12, 31, 23, 59, 59, DateTimeKind.Unspecified),
            NullableGuid = Guid.NewGuid(),
            NullableKind = TypeMatrixKind.Alpha,
            NullableLabel = "nullable label"
        };

        private static void ClearNullables(TypeMatrixRow row)
        {
            row.NullableBig = null;
            row.NullableMoment = null;
            row.NullableGuid = null;
            row.NullableKind = null;
            row.NullableLabel = null;
        }

        private static void ChangeEverything(TypeMatrixRow row)
        {
            row.BigValue = -9_007_199_254_740_993L;
            row.SmallValue = 32_000;
            row.DoubleValue = -2.718281828459045;
            row.SingleValue = -3.5f;
            row.DecimalValue = -0.0000000001m;
            row.Flag = false;
            row.Label = "changed ½ ünï";
            row.Moment = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            row.OffsetMoment = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
            row.CalendarDay = new DateOnly(2000, 1, 1);
            row.TimeOfDay = new TimeOnly(0, 0, 1);
            row.Identifier = Guid.NewGuid();
            row.Kind = TypeMatrixKind.Alpha;
            row.Payload = [0xAB];
            ClearNullables(row);
        }

        private static void AssertSame(TypeMatrixRow expected, TypeMatrixRow? actual)
        {
            Assert.NotNull(actual);
            Assert.Equal(expected.Id, actual!.Id);
            Assert.Equal(expected.BigValue, actual.BigValue);
            Assert.Equal(expected.SmallValue, actual.SmallValue);
            Assert.Equal(expected.DoubleValue, actual.DoubleValue);
            Assert.Equal(expected.SingleValue, actual.SingleValue);
            Assert.Equal(expected.DecimalValue, actual.DecimalValue);
            Assert.Equal(expected.Flag, actual.Flag);
            Assert.Equal(expected.Label, actual.Label);
            Assert.Equal(expected.Moment.Ticks, actual.Moment.Ticks);
            Assert.Equal(expected.OffsetMoment.UtcTicks, actual.OffsetMoment.UtcTicks);
            Assert.Equal(expected.CalendarDay, actual.CalendarDay);
            Assert.Equal(expected.TimeOfDay, actual.TimeOfDay);
            Assert.Equal(expected.Identifier, actual.Identifier);
            Assert.Equal(expected.Kind, actual.Kind);
            Assert.Equal(expected.Payload, actual.Payload);
            Assert.Equal(expected.NullableBig, actual.NullableBig);
            Assert.Equal(expected.NullableMoment?.Ticks, actual.NullableMoment?.Ticks);
            Assert.Equal(expected.NullableGuid, actual.NullableGuid);
            Assert.Equal(expected.NullableKind, actual.NullableKind);
            Assert.Equal(expected.NullableLabel, actual.NullableLabel);
        }

        private static List<int> Ids(IEnumerable<TypeMatrixRow?> rows) => rows.Where(r => r is not null).Select(r => r!.Id).Order().ToList();

        [Fact]
        public async Task InsertAsync_ThenGetByIdAsync_RoundTripsEveryColumnType()
        {
            TypeMatrixRow row = Sample(1);

            int affected = await _fixture.Connection.InsertAsync(row, cancellationToken: Ct);
            TypeMatrixRow? fetched = await _fixture.Connection.GetByIdAsync<TypeMatrixRow>(1, cancellationToken: Ct);

            Assert.Equal(1, affected);
            AssertSame(row, fetched);
        }

        [Fact]
        public void Insert_ThenGetById_SyncOverloads_RoundTripEveryColumnType()
        {
            TypeMatrixRow row = Sample(2);

            _fixture.Connection.Insert(row);
            TypeMatrixRow? fetched = _fixture.Connection.GetById<TypeMatrixRow>(2);

            AssertSame(row, fetched);
        }

        [Fact]
        public async Task InsertAsync_WithEveryNullableColumnNull_RoundTripsNulls()
        {
            TypeMatrixRow row = Sample(3);
            ClearNullables(row);

            await _fixture.Connection.InsertAsync(row, cancellationToken: Ct);
            TypeMatrixRow? fetched = await _fixture.Connection.GetByIdAsync<TypeMatrixRow>(3, cancellationToken: Ct);

            AssertSame(row, fetched);
        }

        [Fact]
        public async Task UpdateAsync_ChangesEveryColumnType()
        {
            TypeMatrixRow row = Sample(4);
            await _fixture.Connection.InsertAsync(row, cancellationToken: Ct);

            ChangeEverything(row);
            int affected = await _fixture.Connection.UpdateAsync(row, cancellationToken: Ct);
            TypeMatrixRow? fetched = await _fixture.Connection.GetByIdAsync<TypeMatrixRow>(4, cancellationToken: Ct);

            Assert.Equal(1, affected);
            AssertSame(row, fetched);
        }

        [Fact]
        public async Task UpsertAsync_MatchesOnTheGuidNaturalKey_InsertsThenUpdates()
        {
            TypeMatrixRow first = Sample(5);
            await _fixture.Connection.UpsertAsync(first, cancellationToken: Ct);

            TypeMatrixRow second = Sample(5);
            second.Identifier = first.Identifier;
            second.Label = "upserted";
            second.BigValue = 42;
            second.Moment = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Unspecified);
            await _fixture.Connection.UpsertAsync(second, cancellationToken: Ct);

            long rowsWithKey = await _fixture.Connection.CountAsync<TypeMatrixRow>(r => r.Identifier == first.Identifier, cancellationToken: Ct);
            TypeMatrixRow? fetched = await _fixture.Connection.GetByIdAsync<TypeMatrixRow>(5, cancellationToken: Ct);

            Assert.Equal(1, rowsWithKey);
            AssertSame(second, fetched);
        }

        [Fact]
        public async Task InsertRangeAsync_WithNullsAndValuesInTheSameColumn_RoundTripsEveryRow()
        {
            // Same column holding a value in one row and NULL in another is what breaks naive multi-row inserts on
            // engines that infer a column's type from the first row.
            TypeMatrixRow filled = Sample(10);
            TypeMatrixRow nulls = Sample(11);
            ClearNullables(nulls);
            TypeMatrixRow mixed = Sample(12);
            mixed.NullableGuid = null;
            mixed.NullableLabel = null;
            TypeMatrixRow[] rows = [filled, nulls, mixed];

            int affected = await _fixture.Connection.InsertRangeAsync(rows, cancellationToken: Ct);
            IReadOnlyList<TypeMatrixRow?> fetched = await _fixture.Connection.GetByIdRangeAsync<TypeMatrixRow>(new[] { 10, 11, 12 }, cancellationToken: Ct);

            Assert.Equal(3, affected);
            foreach (TypeMatrixRow expected in rows)
            {
                AssertSame(expected, fetched.Single(r => r?.Id == expected.Id));
            }
        }

        [Fact]
        public async Task UpdateRangeAsync_ChangesEveryColumnTypeOnEveryRow()
        {
            TypeMatrixRow a = Sample(20);
            TypeMatrixRow b = Sample(21);
            await _fixture.Connection.InsertRangeAsync([a, b], cancellationToken: Ct);

            ChangeEverything(a);
            ChangeEverything(b);
            int affected = await _fixture.Connection.UpdateRangeAsync([a, b], cancellationToken: Ct);
            IReadOnlyList<TypeMatrixRow?> fetched = await _fixture.Connection.GetByIdRangeAsync<TypeMatrixRow>(new[] { 20, 21 }, cancellationToken: Ct);

            Assert.Equal(2, affected);
            AssertSame(a, fetched.Single(r => r?.Id == 20));
            AssertSame(b, fetched.Single(r => r?.Id == 21));
        }

        [Fact]
        public async Task UpsertRangeAsync_MixOfExistingAndNewRows_UpdatesAndInserts()
        {
            TypeMatrixRow existing = Sample(30);
            await _fixture.Connection.InsertAsync(existing, cancellationToken: Ct);

            TypeMatrixRow updated = Sample(30);
            updated.Identifier = existing.Identifier;
            updated.Label = "range upserted";
            ClearNullables(updated);
            TypeMatrixRow brandNew = Sample(31);

            await _fixture.Connection.UpsertRangeAsync([updated, brandNew], cancellationToken: Ct);
            IReadOnlyList<TypeMatrixRow?> fetched = await _fixture.Connection.GetByIdRangeAsync<TypeMatrixRow>(new[] { 30, 31 }, cancellationToken: Ct);

            AssertSame(updated, fetched.Single(r => r?.Id == 30));
            AssertSame(brandNew, fetched.Single(r => r?.Id == 31));
        }

        [Fact]
        public async Task GetAllAsync_FiltersOnEveryColumnType()
        {
            TypeMatrixRow r40 = Sample(40);

            TypeMatrixRow r41 = Sample(41);
            r41.Kind = TypeMatrixKind.Alpha;
            r41.BigValue = 5;
            r41.DecimalValue = 1.5m;
            r41.Flag = false;
            r41.Moment = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            r41.CalendarDay = new DateOnly(2020, 1, 1);
            r41.NullableGuid = null;
            r41.Label = "plain";

            TypeMatrixRow r42 = Sample(42);
            r42.Kind = TypeMatrixKind.None;
            r42.BigValue = 7;
            r42.DecimalValue = 2.5m;
            r42.Flag = true;
            r42.Moment = new DateTime(2019, 6, 1, 0, 0, 0, DateTimeKind.Unspecified);
            r42.CalendarDay = new DateOnly(2019, 6, 1);
            r42.Label = "plain 2";

            await _fixture.Connection.InsertRangeAsync([r40, r41, r42], cancellationToken: Ct);

            Guid guid = r40.Identifier;
            DateTime cutoff = new(2021, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            DateTime exactly = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
            DateOnly day = new(2020, 1, 1);
            long big = 9_007_199_254_740_993L;

            Assert.Equal([40], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Identifier == guid, cancellationToken: Ct)));
            Assert.Equal([41], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 40 && r.Id <= 42 && r.Kind == TypeMatrixKind.Alpha, cancellationToken: Ct)));
            Assert.Equal([40], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 40 && r.Id <= 42 && r.Moment > cutoff, cancellationToken: Ct)));
            Assert.Equal([41, 42], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 40 && r.Id <= 42 && r.Moment <= exactly, cancellationToken: Ct)));
            Assert.Equal([41], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 40 && r.Id <= 42 && r.CalendarDay == day, cancellationToken: Ct)));
            Assert.Equal([40, 42], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 40 && r.Id <= 42 && r.DecimalValue >= 2m, cancellationToken: Ct)));
            Assert.Equal([40, 42], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 40 && r.Id <= 42 && r.Flag, cancellationToken: Ct)));
            Assert.Equal([41], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 40 && r.Id <= 42 && r.NullableGuid == null, cancellationToken: Ct)));
            Assert.Equal([40], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 40 && r.Id <= 42 && r.Label.Contains("日本"), cancellationToken: Ct)));
            Assert.Equal([40], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 40 && r.Id <= 42 && r.BigValue == big, cancellationToken: Ct)));
        }

        [Fact]
        public async Task GetAllAsync_ContainsOverGuidAndEnumLists_BindsEveryElement()
        {
            TypeMatrixRow r45 = Sample(45);
            TypeMatrixRow r46 = Sample(46);
            r46.Kind = TypeMatrixKind.Alpha;
            TypeMatrixRow r47 = Sample(47);
            r47.Kind = TypeMatrixKind.None;
            await _fixture.Connection.InsertRangeAsync([r45, r46, r47], cancellationToken: Ct);

            List<Guid> guids = [r45.Identifier, r47.Identifier];
            List<TypeMatrixKind> kinds = [TypeMatrixKind.Alpha, TypeMatrixKind.None];

            Assert.Equal([45, 47], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 45 && r.Id <= 47 && guids.Contains(r.Identifier), cancellationToken: Ct)));
            Assert.Equal([46, 47], Ids(await _fixture.Connection.GetAllAsync<TypeMatrixRow>(r => r.Id >= 45 && r.Id <= 47 && kinds.Contains(r.Kind), cancellationToken: Ct)));
        }

        [Fact]
        public async Task GetAllAsync_FilterDescriptorInOverEnumList_BindsEveryElement()
        {
            // The FilterDescriptor path builds its IN/ANY array separately from the expression path.
            TypeMatrixRow a = Sample(60);
            a.Kind = TypeMatrixKind.Alpha;
            TypeMatrixRow b = Sample(61);
            b.Kind = TypeMatrixKind.Beta;
            TypeMatrixRow c = Sample(62);
            c.Kind = TypeMatrixKind.None;
            await _fixture.Connection.InsertRangeAsync([a, b, c], cancellationToken: Ct);

            FilterDescriptor<TypeMatrixRow> filter = new("Kind", new List<TypeMatrixKind> { TypeMatrixKind.Alpha, TypeMatrixKind.None }, ComparisonOperator.In);
            IReadOnlyList<TypeMatrixRow> rows = await _fixture.Connection.GetAllAsync(filter, cancellationToken: Ct);

            Assert.Equal([60, 62], Ids(rows.Where(r => r.Id >= 60 && r.Id <= 62)));
        }

        [Fact]
        public async Task GetByIdRangeAsync_AndDeleteRangeAsync_WithAnEnumKey_BindEveryId()
        {
            await _fixture.Connection.InsertRangeAsync([new EnumKeyed { Id = TypeMatrixKind.Alpha, Name = "a" }, new EnumKeyed { Id = TypeMatrixKind.Beta, Name = "b" }], cancellationToken: Ct);
            TypeMatrixKind[] ids = [TypeMatrixKind.Alpha, TypeMatrixKind.Beta];

            IReadOnlyList<EnumKeyed?> fetched = await _fixture.Connection.GetByIdRangeAsync<EnumKeyed>(ids, cancellationToken: Ct);
            int deleted = await _fixture.Connection.DeleteRangeAsync<EnumKeyed>(ids, cancellationToken: Ct);

            Assert.Equal(2, fetched.Count(r => r is not null));
            Assert.Equal(2, deleted);
        }

        [Fact]
        public async Task GetByIdRangeAsync_AndDeleteRangeAsync_WithAGuidKey_BindEveryId()
        {
            Guid first = Guid.NewGuid();
            Guid second = Guid.NewGuid();
            await _fixture.Connection.InsertRangeAsync([new GuidKeyed { Id = first, Name = "a" }, new GuidKeyed { Id = second, Name = "b" }], cancellationToken: Ct);
            Guid[] ids = [first, second];

            IReadOnlyList<GuidKeyed?> fetched = await _fixture.Connection.GetByIdRangeAsync<GuidKeyed>(ids, cancellationToken: Ct);
            int deleted = await _fixture.Connection.DeleteRangeAsync<GuidKeyed>(ids, cancellationToken: Ct);

            Assert.Equal(2, fetched.Count(r => r is not null));
            Assert.Equal(2, deleted);
        }

        [Fact]
        public async Task Aggregates_OverEveryNumericAndDateTypeShape_ReturnTheExpectedValues()
        {
            TypeMatrixRow a = Sample(50);
            a.BigValue = 10; a.DecimalValue = 1.1234567891m; a.DoubleValue = 1.5;
            a.Moment = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Unspecified); a.CalendarDay = new DateOnly(2020, 1, 1);
            TypeMatrixRow b = Sample(51);
            b.BigValue = 20; b.DecimalValue = 2.2222222222m; b.DoubleValue = 2.5;
            b.Moment = new DateTime(2022, 2, 2, 0, 0, 0, DateTimeKind.Unspecified); b.CalendarDay = new DateOnly(2022, 2, 2);
            TypeMatrixRow c = Sample(52);
            c.BigValue = 30; c.DecimalValue = 3.0000000001m; c.DoubleValue = 3.5;
            c.Moment = new DateTime(2021, 3, 3, 0, 0, 0, DateTimeKind.Unspecified); c.CalendarDay = new DateOnly(2021, 3, 3);
            await _fixture.Connection.InsertRangeAsync([a, b, c], cancellationToken: Ct);

            decimal? sum = await _fixture.Connection.SumAsync<TypeMatrixRow>(r => r.DecimalValue, r => r.Id >= 50 && r.Id <= 52, cancellationToken: Ct);
            decimal? avg = await _fixture.Connection.AvgAsync<TypeMatrixRow>(r => r.DecimalValue, r => r.Id >= 50 && r.Id <= 52, cancellationToken: Ct);
            long? minBig = await _fixture.Connection.MinAsync<TypeMatrixRow, long>(r => r.BigValue, r => r.Id >= 50 && r.Id <= 52, cancellationToken: Ct);
            long? maxBig = await _fixture.Connection.MaxAsync<TypeMatrixRow, long>(r => r.BigValue, r => r.Id >= 50 && r.Id <= 52, cancellationToken: Ct);
            double? maxDouble = await _fixture.Connection.MaxAsync<TypeMatrixRow, double>(r => r.DoubleValue, r => r.Id >= 50 && r.Id <= 52, cancellationToken: Ct);
            DateTime? maxMoment = await _fixture.Connection.MaxAsync<TypeMatrixRow, DateTime>(r => r.Moment, r => r.Id >= 50 && r.Id <= 52, cancellationToken: Ct);
            DateOnly? minDay = await _fixture.Connection.MinAsync<TypeMatrixRow, DateOnly>(r => r.CalendarDay, r => r.Id >= 50 && r.Id <= 52, cancellationToken: Ct);

            Assert.Equal(6.3456790114m, sum);
            Assert.InRange(avg!.Value, 2.1152263371m, 2.1152263372m);
            Assert.Equal(10L, minBig);
            Assert.Equal(30L, maxBig);
            Assert.Equal(3.5, maxDouble);
            Assert.Equal(new DateTime(2022, 2, 2, 0, 0, 0, DateTimeKind.Unspecified).Ticks, maxMoment!.Value.Ticks);
            Assert.Equal(new DateOnly(2020, 1, 1), minDay);
        }
    }
}
