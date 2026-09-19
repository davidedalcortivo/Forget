using Forget.Core.Models;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reflection;


namespace Forget.Core.Caching
{
    internal static class DbColumnInfoCache<TEntity> where TEntity : class
    {
        private static readonly ConcurrentDictionary<string, IReadOnlyList<DbColumnInfo>> _listCache = new();
        private static readonly ConcurrentDictionary<string, IDictionary<string, DbColumnInfo>> _dictCache = new();
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

        public static IReadOnlyList<DbColumnInfo>? GetListValueOrDefault(string connectionId)
        {
            return _listCache.GetValueOrDefault(connectionId);
        }

        public static IDictionary<string, DbColumnInfo>? GetDictValueOrDefault(string connectionId)
        {
            return _dictCache.GetValueOrDefault(connectionId);
        }

        public static IReadOnlyList<DbColumnInfo> GetListValue(string connectionId)
        {
            IReadOnlyList<DbColumnInfo> columns = _listCache.GetValueOrDefault(connectionId) ??
                throw new InvalidOperationException($"Database cache is not initialized for entity '{typeof(TEntity).Name}' and connection '{connectionId}'. Call LoadDbCache before performing this operation.");
            
            return columns;
        }

        public static IDictionary<string, DbColumnInfo> GetDictValue(string connectionId)
        {
            IDictionary<string, DbColumnInfo> columns = _dictCache.GetValueOrDefault(connectionId) ??
                throw new InvalidOperationException($"Database cache is not initialized for entity '{typeof(TEntity).Name}' and connection '{connectionId}'. Call LoadDbCache before performing this operation.");

            return columns;
        }

        public static void Add(string connectionId, IReadOnlyList<DbColumnInfo> columns)
        {
            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;
            HashSet<string> columnNames = new(columns.Count, StringComparer.OrdinalIgnoreCase);

            foreach (DbColumnInfo column in columns)
                columnNames.Add(column.Name);

            foreach (PropertyInfo property in properties)
            {
                string columnName = columnNamesByPropertyName[property.Name];

                if (!columnNames.Contains(columnName))
                    throw new InvalidOperationException($"Database column mapping mismatch for entity '{typeof(TEntity).Name}'. The property '{property.Name}' is mapped to the column '{columnName}', which does not exist in the database table.");
            }

            _ = _listCache.TryAdd(connectionId, columns);
        }

        public static void Add(string connectionId, IDictionary<string, DbColumnInfo> columns)
        {
            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;
            ImmutableDictionary<string, string> columnNamesByPropertyName = EntityInfoCache<TEntity>.ColumnNamesByPropertyName;

            foreach (PropertyInfo property in properties)
            {
                string columnName = columnNamesByPropertyName[property.Name];

                if (!columns.ContainsKey(columnName))
                    throw new InvalidOperationException($"Database column mapping mismatch for entity '{typeof(TEntity).Name}'. The property '{property.Name}' is mapped to the column '{columnName}', which does not exist in the database table.");
            }

            _ = _dictCache.TryAdd(connectionId, columns);
        }

        public static SemaphoreSlim GetSemaphore(string connectionId)
        {
            return _semaphores.GetOrAdd(connectionId, static _ => new(1, 1));
        }
    }
}
