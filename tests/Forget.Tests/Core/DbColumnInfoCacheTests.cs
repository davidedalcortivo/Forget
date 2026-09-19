using Forget.Core.Caching;
using Forget.Core.Models;


namespace Forget.Tests.Core
{
    /// <summary>
    /// The column cache holds what the database reports for an entity's table. An entity maps the columns it
    /// declares, not necessarily the whole table (audit or legacy columns are common), so the only thing the cache
    /// has to reject is a mapped property whose column is missing.
    /// </summary>
    public class DbColumnInfoCacheTests
    {
        private sealed class PartialDictionaryEntity
        {
            public int Id { get; set; }
            public int Qty { get; set; }
        }

        private sealed class PartialListEntity
        {
            public int Id { get; set; }
            public int Qty { get; set; }
        }

        private sealed class MissingColumnDictionaryEntity
        {
            public int Id { get; set; }
            public int Qty { get; set; }
        }

        private sealed class MissingColumnListEntity
        {
            public int Id { get; set; }
            public int Qty { get; set; }
        }

        private static DbColumnInfo Column(string name) => new() { Name = name };

        [Fact]
        public void Add_Dictionary_AcceptsATableWithColumnsTheEntityDoesNotMap()
        {
            Dictionary<string, DbColumnInfo> columns = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = Column("Id"),
                ["Legacy"] = Column("Legacy"),
                ["Qty"] = Column("Qty"),
                ["Audit"] = Column("Audit")
            };

            DbColumnInfoCache<PartialDictionaryEntity>.Add("partial", columns);

            Assert.Same(columns, DbColumnInfoCache<PartialDictionaryEntity>.GetDictValue("partial"));
        }

        [Fact]
        public void Add_List_AcceptsATableWithColumnsTheEntityDoesNotMap()
        {
            List<DbColumnInfo> columns = [Column("Id"), Column("Legacy"), Column("Qty"), Column("Audit")];

            DbColumnInfoCache<PartialListEntity>.Add("partial", columns);

            Assert.Same(columns, DbColumnInfoCache<PartialListEntity>.GetListValue("partial"));
        }

        [Fact]
        public void Add_Dictionary_RejectsAMappedPropertyWhoseColumnIsMissing()
        {
            Dictionary<string, DbColumnInfo> columns = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Id"] = Column("Id"),
                ["Audit"] = Column("Audit")
            };

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => DbColumnInfoCache<MissingColumnDictionaryEntity>.Add("missing", columns));

            Assert.Contains(nameof(MissingColumnDictionaryEntity), ex.Message);
            Assert.Contains("'Qty'", ex.Message);
            Assert.Contains("does not exist", ex.Message);
            Assert.Null(DbColumnInfoCache<MissingColumnDictionaryEntity>.GetDictValueOrDefault("missing"));
        }

        [Fact]
        public void Add_List_RejectsAMappedPropertyWhoseColumnIsMissing()
        {
            List<DbColumnInfo> columns = [Column("Id"), Column("Audit")];

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => DbColumnInfoCache<MissingColumnListEntity>.Add("missing", columns));

            Assert.Contains(nameof(MissingColumnListEntity), ex.Message);
            Assert.Contains("'Qty'", ex.Message);
            Assert.Contains("does not exist", ex.Message);
            Assert.Null(DbColumnInfoCache<MissingColumnListEntity>.GetListValueOrDefault("missing"));
        }
    }
}
