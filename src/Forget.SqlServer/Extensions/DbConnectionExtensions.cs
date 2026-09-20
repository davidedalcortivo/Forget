using Forget.Core.Abstractions.Models;
using Forget.Core.Caching;
using Forget.Core.Models;
using Forget.SqlServer.Strategies;
using Microsoft.Data.SqlClient;
using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;


namespace Forget.SqlServer.Extensions
{
    /// <summary>
    /// Extension methods on <see cref="SqlConnection"/> providing type-safe CRUD (single-row or multi-row),
    /// dynamic filtering, sorting, paging, and aggregation against SQL Server, for any entity mapped with the
    /// attributes in <c>Forget.Core.Models</c>.
    /// </summary>
    public static partial class DbConnectionExtensions
    {
        /// <summary>
        /// Ensures the SQL command cache and any database-derived metadata for <typeparamref name="TEntity"/> are
        /// loaded, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to load the cache for.</typeparam>
        /// <param name="connection">The connection used to load the cache.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <remarks>
        /// SQL Server needs to know a column's actual data type to decide whether computing <c>Avg</c>/<c>AvgAsync</c> or
        /// <c>Sum</c>/<c>SumAsync</c> over it needs a safety cast first — for example, a narrow integer type (<c>int</c>,
        /// <c>smallint</c>, or <c>tinyint</c>) may need to be cast to <c>bigint</c> before summing, to avoid integer overflow during
        /// the aggregation. The first call for a given <typeparamref name="TEntity"/> and connection queries SQL Server's system catalogs
        /// and caches the result; later calls for the same combination return immediately without hitting the database again.
        /// <para>
        /// Calling this ahead of time is required before using
        /// <see cref="AvgCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> or
        /// <see cref="SumCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (or their
        /// overloads) directly. The corresponding <c>Avg</c>/<c>AvgAsync</c> and <c>Sum</c>/<c>SumAsync</c>
        /// execution methods in this class call it automatically and do not need it called first.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static void LoadDbCache<TEntity>(this SqlConnection connection, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, no
        /// explicit ordering is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The matching rows. Never <see langword="null"/>; an empty list if none match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static IReadOnlyList<TEntity> GetAll<TEntity>(this SqlConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetAllImplAsync(connection, true, (IFilterNode<TEntity>?)null, sortDescriptors, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves every <typeparamref name="TEntity"/> row matching the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the returned rows must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, no
        /// explicit ordering is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The matching rows. Never <see langword="null"/>; an empty list if none match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static IReadOnlyList<TEntity> GetAll<TEntity>(this SqlConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetAllImplAsync(connection, true, predicate, sortDescriptors, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves every <typeparamref name="TEntity"/> row matching the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the returned rows must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, no
        /// explicit ordering is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The matching rows. Never <see langword="null"/>; an empty list if none match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static IReadOnlyList<TEntity> GetAll<TEntity>(this SqlConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetAllImplAsync(connection, true, filterNode, sortDescriptors, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The first matching row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="InvalidOperationException">No row exists.</exception>
        public static TEntity GetFirst<TEntity>(this SqlConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetFirstImplAsync(connection, true, (IFilterNode<TEntity>?)null, sortDescriptors, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row matching the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The first matching row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="InvalidOperationException">No row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static TEntity GetFirst<TEntity>(this SqlConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetFirstImplAsync(connection, true, predicate, sortDescriptors, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row matching the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The first matching row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="InvalidOperationException">No row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static TEntity GetFirst<TEntity>(this SqlConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetFirstImplAsync(connection, true, filterNode, sortDescriptors, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row, or <see langword="null"/> if none exists, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The first matching row, or <see langword="null"/> if none exists.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static TEntity? GetFirstOrDefault<TEntity>(this SqlConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetFirstOrDefaultImplAsync(connection, true, (IFilterNode<TEntity>?)null, sortDescriptors, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row matching the specified predicate, or
        /// <see langword="null"/> if none matches, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The first matching row, or <see langword="null"/> if none matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static TEntity? GetFirstOrDefault<TEntity>(this SqlConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetFirstOrDefaultImplAsync(connection, true, predicate, sortDescriptors, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row matching the specified filter, or
        /// <see langword="null"/> if none matches, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The first matching row, or <see langword="null"/> if none matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static TEntity? GetFirstOrDefault<TEntity>(this SqlConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetFirstOrDefaultImplAsync(connection, true, filterNode, sortDescriptors, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The single row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="InvalidOperationException">No row exists, or more than one row exists.</exception>
        public static TEntity GetSingle<TEntity>(this SqlConnection connection, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetSingleImplAsync(connection, true, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row matching the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The single matching row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="InvalidOperationException">No row matches, or more than one row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static TEntity GetSingle<TEntity>(this SqlConnection connection, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetSingleImplAsync(connection, true, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row matching the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The single matching row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="InvalidOperationException">No row matches, or more than one row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static TEntity GetSingle<TEntity>(this SqlConnection connection, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetSingleImplAsync(connection, true, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row, or <see langword="null"/> if none exists, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The single row, or <see langword="null"/> if none exists.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="InvalidOperationException">More than one row exists.</exception>
        public static TEntity? GetSingleOrDefault<TEntity>(this SqlConnection connection, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetSingleOrDefaultImplAsync(connection, true, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row matching the specified predicate, or
        /// <see langword="null"/> if none matches, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The single matching row, or <see langword="null"/> if none matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="InvalidOperationException">More than one row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static TEntity? GetSingleOrDefault<TEntity>(this SqlConnection connection, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetSingleOrDefaultImplAsync(connection, true, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row matching the specified filter, or
        /// <see langword="null"/> if none matches, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The single matching row, or <see langword="null"/> if none matches.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="InvalidOperationException">More than one row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static TEntity? GetSingleOrDefault<TEntity>(this SqlConnection connection, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetSingleOrDefaultImplAsync(connection, true, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the <typeparamref name="TEntity"/> row with the specified identifier, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="id">
        /// The identifier of the row to retrieve. Its runtime type must exactly match the type of
        /// <typeparamref name="TEntity"/>'s identifier property.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The matching row, or <see langword="null"/> if no row has that identifier.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The type of <paramref name="id"/> does not match the type of <typeparamref name="TEntity"/>'s identifier
        /// property, or <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        public static TEntity? GetById<TEntity>(this SqlConnection connection, object id, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetByIdImplAsync<TEntity>(connection, true, id, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves a page of <typeparamref name="TEntity"/> rows, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty and
        /// <paramref name="skip"/> or <paramref name="take"/> is specified, the results are ordered by the
        /// identifier property instead, to ensure stable pagination.
        /// </param>
        /// <param name="skip">The number of matching rows to skip. When <see langword="null"/> or negative, no rows are skipped.</param>
        /// <param name="take">
        /// The maximum number of rows to return. When <see langword="null"/> or negative, every row is returned,
        /// starting after <paramref name="skip"/> if specified.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The matching rows for the requested page. Never <see langword="null"/>; an empty list if none match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static IReadOnlyList<TEntity> GetPage<TEntity>(this SqlConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetPageImplAsync(connection, true, (IFilterNode<TEntity>?)null, sortDescriptors, skip, take, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves a page of <typeparamref name="TEntity"/> rows matching the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the returned rows must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty and
        /// <paramref name="skip"/> or <paramref name="take"/> is specified, the results are ordered by the
        /// identifier property instead, to ensure stable pagination.
        /// </param>
        /// <param name="skip">The number of matching rows to skip. When <see langword="null"/> or negative, no rows are skipped.</param>
        /// <param name="take">
        /// The maximum number of rows to return. When <see langword="null"/> or negative, every row is returned,
        /// starting after <paramref name="skip"/> if specified.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The matching rows for the requested page. Never <see langword="null"/>; an empty list if none match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static IReadOnlyList<TEntity> GetPage<TEntity>(this SqlConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetPageImplAsync(connection, true, predicate, sortDescriptors, skip, take, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves a page of <typeparamref name="TEntity"/> rows matching the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the returned rows must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty and
        /// <paramref name="skip"/> or <paramref name="take"/> is specified, the results are ordered by the
        /// identifier property instead, to ensure stable pagination.
        /// </param>
        /// <param name="skip">The number of matching rows to skip. When <see langword="null"/> or negative, no rows are skipped.</param>
        /// <param name="take">
        /// The maximum number of rows to return. When <see langword="null"/> or negative, every row is returned,
        /// starting after <paramref name="skip"/> if specified.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The matching rows for the requested page. Never <see langword="null"/>; an empty list if none match.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static IReadOnlyList<TEntity> GetPage<TEntity>(this SqlConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.GetPageImplAsync(connection, true, filterNode, sortDescriptors, skip, take, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates every column of <paramref name="entity"/>'s row, except its identifier, any
        /// database-generated property, and any property marked <see cref="NotMappedAttribute"/>, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection to execute the update on.</param>
        /// <param name="entity">The entity whose current property values are written back to its row.</param>
        /// <param name="transaction">The transaction to execute the update within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static int Update<TEntity>(this SqlConnection connection, TEntity entity, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.UpdateImplAsync(connection, true, entity, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates the specified columns for every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection to execute the update on.</param>
        /// <param name="values">
        /// An object whose public properties specify the columns to update and their new values. A property that is
        /// the identifier — either one named "Id" (case-insensitive), or the property marked with
        /// <see cref="KeyAttribute"/> — or is marked as database-generated or <see cref="NotMappedAttribute"/>,
        /// is silently ignored rather than treated as a column to update.
        /// Every other property must correspond to an updatable property of <typeparamref name="TEntity"/>,
        /// and its value must be assignable to that property's type.
        /// </param>
        /// <param name="transaction">The transaction to execute the update within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <remarks>
        /// This updates every row in the table. Use one of the overloads that also accepts a predicate or a filter
        /// to restrict which rows are updated.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="values"/> has no properties left to update once its identifier, database-generated, and
        /// <see cref="NotMappedAttribute"/>-marked properties are excluded; one of its remaining properties
        /// does not correspond to an updatable property of <typeparamref name="TEntity"/>;
        /// a remaining property's value is not assignable to the corresponding property's type;
        /// or <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// More than one property of <paramref name="values"/> is marked with <see cref="KeyAttribute"/>, or
        /// one of its property names is reserved for internal use by this library.
        /// </exception>
        public static int Update<TEntity>(this SqlConnection connection, object values, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.UpdateImplAsync(connection, true, values, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates the specified columns for every <typeparamref name="TEntity"/> row matching the specified
        /// predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection to execute the update on.</param>
        /// <param name="values">
        /// An object whose public properties specify the columns to update and their new values. A property that is
        /// the identifier — either one named "Id" (case-insensitive), or the property marked with
        /// <see cref="KeyAttribute"/> — or is marked as database-generated or <see cref="NotMappedAttribute"/>,
        /// is silently ignored rather than treated as a column to update.
        /// Every other property must correspond to an updatable property of <typeparamref name="TEntity"/>,
        /// and its value must be assignable to that property's type.
        /// </param>
        /// <param name="predicate">
        /// An optional predicate the updated rows must satisfy. When <see langword="null"/>, every row is updated.
        /// </param>
        /// <param name="transaction">The transaction to execute the update within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="values"/> has no properties left to update once its identifier, database-generated, and
        /// <see cref="NotMappedAttribute"/>-marked properties are excluded; one of its remaining properties
        /// does not correspond to an updatable property of <typeparamref name="TEntity"/>;
        /// a remaining property's value is not assignable to the corresponding property's type;
        /// or <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// More than one property of <paramref name="values"/> is marked with <see cref="KeyAttribute"/>, or
        /// one of its property names is reserved for internal use by this library.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static int Update<TEntity>(this SqlConnection connection, object values, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.UpdateImplAsync(connection, true, values, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates the specified columns for every <typeparamref name="TEntity"/> row matching the specified
        /// filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection to execute the update on.</param>
        /// <param name="values">
        /// An object whose public properties specify the columns to update and their new values. A property that is
        /// the identifier — either one named "Id" (case-insensitive), or the property marked with
        /// <see cref="KeyAttribute"/> — or is marked as database-generated or <see cref="NotMappedAttribute"/>,
        /// is silently ignored rather than treated as a column to update.
        /// Every other property must correspond to an updatable property of <typeparamref name="TEntity"/>,
        /// and its value must be assignable to that property's type.
        /// </param>
        /// <param name="filterNode">
        /// An optional filter the updated rows must satisfy. When <see langword="null"/>, every row is updated.
        /// </param>
        /// <param name="transaction">The transaction to execute the update within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="values"/> has no properties left to update once its identifier, database-generated, and
        /// <see cref="NotMappedAttribute"/>-marked properties are excluded; one of its remaining properties
        /// does not correspond to an updatable property of <typeparamref name="TEntity"/>;
        /// a remaining property's value is not assignable to the corresponding property's type;
        /// or <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// More than one property of <paramref name="values"/> is marked with <see cref="KeyAttribute"/>, or
        /// one of its property names is reserved for internal use by this library.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static int Update<TEntity>(this SqlConnection connection, object values, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.UpdateImplAsync(connection, true, values, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Inserts <paramref name="entity"/> as a new row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to insert.</typeparam>
        /// <param name="connection">The connection to execute the insert on.</param>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="transaction">The transaction to execute the insert within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <remarks>
        /// Every property that is not marked <see cref="NotMappedAttribute"/> or as database-generated
        /// is included in the generated <c>INSERT</c>, including the identifier property
        /// unless it is itself database-generated.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static int Insert<TEntity>(this SqlConnection connection, TEntity entity, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.InsertImplAsync(connection, true, entity, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes <paramref name="entity"/>'s row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="entity">The entity whose row is deleted, matched by its identifier.</param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static int Delete<TEntity>(this SqlConnection connection, TEntity entity, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.DeleteImplAsync(connection, true, entity, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes the <typeparamref name="TEntity"/> row with the specified identifier, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="id">
        /// The identifier of the row to delete. Its runtime type must exactly match the type of
        /// <typeparamref name="TEntity"/>'s identifier property.
        /// </param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The type of <paramref name="id"/> does not match the type of <typeparamref name="TEntity"/>'s identifier
        /// property, or <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        public static int Delete<TEntity>(this SqlConnection connection, object id, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.DeleteImplAsync<TEntity>(connection, true, id, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete rows from.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <remarks>
        /// This deletes every row in the table. Use one of the overloads that accepts a predicate or a filter to
        /// restrict which rows are deleted.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static int Delete<TEntity>(this SqlConnection connection, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.DeleteImplAsync(connection, true, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes every <typeparamref name="TEntity"/> row matching the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete rows from.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="predicate">
        /// An optional predicate the deleted rows must satisfy. When <see langword="null"/>, every row is deleted.
        /// </param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static int Delete<TEntity>(this SqlConnection connection, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.DeleteImplAsync(connection, true, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes every <typeparamref name="TEntity"/> row matching the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete rows from.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="filterNode">
        /// An optional filter the deleted rows must satisfy. When <see langword="null"/>, every row is deleted.
        /// </param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static int Delete<TEntity>(this SqlConnection connection, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.DeleteImplAsync(connection, true, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Inserts <paramref name="entity"/> as a new row, or updates its row if one already exists, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to upsert.</typeparam>
        /// <param name="connection">The connection to execute the upsert on.</param>
        /// <param name="entity">The entity to insert or update.</param>
        /// <param name="transaction">
        /// The transaction to execute the upsert within, or <see langword="null"/> to have this method manage its
        /// own transaction (see remarks).
        /// </param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows affected.</returns>
        /// <remarks>
        /// Every property that is not marked as database-generated or <see cref="NotMappedAttribute"/>
        /// is included in the <c>INSERT</c> portion of the statement;
        /// the <c>UPDATE</c> portion updates the same set of properties, except the identifier and
        /// any property marked <see cref="UpsertKeyAttribute"/>.
        /// <para>
        /// Unlike this library's other providers, this executes two separate statements rather than a single atomic
        /// one: an <c>UPDATE</c> that matches an existing row using the properties marked with
        /// <see cref="UpsertKeyAttribute"/> — or the entity's identifier, if none are
        /// marked — treating two <see langword="null"/> values in a key property as equal, followed by an
        /// <c>INSERT</c> that runs only if the <c>UPDATE</c> affected no rows. On a match, every property is
        /// updated except the identifier, any database-generated property, and the key properties themselves;
        /// otherwise, a new row is inserted using every property that is not database-generated.
        /// </para>
        /// <para>
        /// Because correctness under concurrent access depends on both statements running in the same transaction,
        /// when <paramref name="transaction"/> is <see langword="null"/>, this method begins one, commits it on
        /// success, and rolls it back if execution fails — passing an explicit <paramref name="transaction"/> opts
        /// out of this, making it the caller's responsibility instead. If <paramref name="connection"/> is closed
        /// when this method is called and it owns the transaction, it also opens and closes the connection for the
        /// duration of the operation.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static int Upsert<TEntity>(this SqlConnection connection, TEntity entity, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.UpsertImplAsync(connection, true, entity, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Retrieves the <typeparamref name="TEntity"/> rows with the specified identifiers, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="ids">
        /// The identifiers of the rows to retrieve. Neither the sequence nor any of its elements may be
        /// <see langword="null"/>, and each identifier's runtime type must exactly match the type of
        /// <typeparamref name="TEntity"/>'s identifier property.
        /// </param>
        /// <param name="batchSize">
        /// The maximum number of identifiers queried by a single round trip, capped according to SQL Server's limit of
        /// roughly 2,100 parameters per command. When less than or equal to zero, the maximum number allowed by
        /// the parameter limit is used.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>
        /// The rows that exist, each once. Their order is not defined, and an identifier with no row does not appear in the
        /// result, which can therefore be shorter than <paramref name="ids"/> (or empty).
        /// </returns>
        /// <remarks>
        /// Each row is returned once, however many of <paramref name="ids"/> match it. The identifier is assumed to be unique
        /// in the table.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="ids"/> is <see langword="null"/>, or one of its
        /// elements is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The type of one of the elements of <paramref name="ids"/> does not match the type of
        /// <typeparamref name="TEntity"/>'s identifier property, or <paramref name="transaction"/> does not belong
        /// to <paramref name="connection"/>.
        /// </exception>
        public static IReadOnlyList<TEntity> GetByIdRange<TEntity>(this SqlConnection connection, IEnumerable ids, int batchSize = 500, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            batchSize = batchSize > 0 ? Math.Min(batchSize, SqlDialectStrategy.Instance.MaxParameterCount) : SqlDialectStrategy.Instance.MaxParameterCount;
            return DbExecutionStrategy.Instance.GetByIdRangeImplAsync<TEntity>(connection, true, ids, batchSize, 0, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Updates every column, except the identifier, any database-generated property, and any property marked
        /// <see cref="NotMappedAttribute"/>, of each of the specified entities' rows, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection to execute the update on.</param>
        /// <param name="entities">The entities whose current property values are written back to their rows.</param>
        /// <param name="batchSize">
        /// The maximum number of entities updated by a single round trip, capped according to SQL Server's limit of
        /// roughly 2,100 parameters per command. When less than or equal to zero, the maximum number allowed by
        /// the parameter limit is used.
        /// </param>
        /// <param name="transaction">The transaction to execute the update within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The total number of rows affected across all round trips.</returns>
        /// <remarks>
        /// Each round trip executes an <c>UPDATE ... FROM</c> statement that joins the target table to a
        /// <c>VALUES</c>-derived source table on identifier, and updates matched rows; rows with no match in the
        /// table are left untouched, and no new rows are inserted.
        /// <para>
        /// When no <paramref name="transaction"/> is provided, this method begins one, commits it on success, and
        /// rolls it back if execution fails — ensuring that the entire operation remains atomic regardless of
        /// how many statements or round trips are required. Passing an explicit <paramref name="transaction"/>
        /// opts out of this; committing or rolling back is then the caller's responsibility.
        /// If <paramref name="connection"/> is closed when this method is called and it owns the transaction,
        /// it also opens and closes the connection for the duration of the operation.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static int UpdateRange<TEntity>(this SqlConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;
            batchSize = batchSize > 0 ? Math.Min(batchSize, SqlDialectStrategy.Instance.MaxParameterCount / properties.Length) : SqlDialectStrategy.Instance.MaxParameterCount / properties.Length;

            return DbExecutionStrategy.Instance.UpdateRangeImplAsync(connection, true, entities, batchSize, 0, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Inserts each of the specified entities as a new row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to insert.</typeparam>
        /// <param name="connection">The connection to execute the insert on.</param>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="batchSize">
        /// The maximum number of entities inserted by a single round trip, capped according to SQL Server's limits of
        /// 1,000 rows per <c>INSERT ... VALUES</c> statement and roughly 2,100 parameters per command. When less than
        /// or equal to zero, the maximum number allowed by these limits is used.
        /// </param>
        /// <param name="transaction">The transaction to execute the insert within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The total number of rows affected across all round trips.</returns>
        /// <remarks>
        /// Every property that is not marked <see cref="NotMappedAttribute"/> or as database-generated
        /// is included in the generated <c>INSERT</c>, including the identifier property
        /// unless it is itself database-generated.
        /// <para>
        /// When no <paramref name="transaction"/> is provided, this method begins one, commits it on success, and
        /// rolls it back if execution fails — ensuring that the entire operation remains atomic regardless of
        /// how many statements or round trips are required. Passing an explicit <paramref name="transaction"/>
        /// opts out of this; committing or rolling back is then the caller's responsibility.
        /// If <paramref name="connection"/> is closed when this method is called and it owns the transaction,
        /// it also opens and closes the connection for the duration of the operation.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static int InsertRange<TEntity>(this SqlConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            ImmutableArray<PropertyInfo> insertProperties = EntityInfoCache<TEntity>.InsertProperties;
            batchSize = batchSize > 0 ? Math.Min(batchSize, SqlDialectStrategy.Instance.MaxParameterCount / insertProperties.Length) : SqlDialectStrategy.Instance.MaxParameterCount / insertProperties.Length;

            return DbExecutionStrategy.Instance.InsertRangeImplAsync(connection, true, entities, batchSize, SqlDialectStrategy.Instance.MaxInsertRowCount, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes each of the specified entities' rows, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="entities">The entities whose rows are deleted, matched by their identifiers.</param>
        /// <param name="batchSize">
        /// The maximum number of identifiers included in a single round trip, capped according to SQL Server's limit of
        /// roughly 2,100 parameters per command. When less than or equal to zero, the maximum number allowed by
        /// the parameter limit is used.
        /// </param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The total number of rows affected across all round trips.</returns>
        /// <remarks>
        /// When no <paramref name="transaction"/> is provided, this method begins one, commits it on success, and
        /// rolls it back if execution fails — ensuring that the entire operation remains atomic regardless of
        /// how many statements or round trips are required. Passing an explicit <paramref name="transaction"/>
        /// opts out of this; committing or rolling back is then the caller's responsibility.
        /// If <paramref name="connection"/> is closed when this method is called and it owns the transaction,
        /// it also opens and closes the connection for the duration of the operation.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static int DeleteRange<TEntity>(this SqlConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            batchSize = batchSize > 0 ? Math.Min(batchSize, SqlDialectStrategy.Instance.MaxParameterCount) : SqlDialectStrategy.Instance.MaxParameterCount;
            return DbExecutionStrategy.Instance.DeleteRangeImplAsync(connection, true, entities, batchSize, 0, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Deletes the <typeparamref name="TEntity"/> rows with the specified identifiers, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="ids">
        /// The identifiers of the rows to delete. Neither the sequence nor any of its elements may be
        /// <see langword="null"/>, and each identifier's runtime type must exactly match the type of
        /// <typeparamref name="TEntity"/>'s identifier property.
        /// </param>
        /// <param name="batchSize">
        /// The maximum number of identifiers included in a single round trip, capped according to SQL Server's limit of
        /// roughly 2,100 parameters per command. When less than or equal to zero, the maximum number allowed by
        /// the parameter limit is used.
        /// </param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The total number of rows affected across all round trips.</returns>
        /// <remarks>
        /// When no <paramref name="transaction"/> is provided, this method begins one, commits it on success, and
        /// rolls it back if execution fails — ensuring that the entire operation remains atomic regardless of
        /// how many statements or round trips are required. Passing an explicit <paramref name="transaction"/>
        /// opts out of this; committing or rolling back is then the caller's responsibility.
        /// If <paramref name="connection"/> is closed when this method is called and it owns the transaction,
        /// it also opens and closes the connection for the duration of the operation.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="ids"/> is <see langword="null"/>, or one of its
        /// elements is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The type of one of the elements of <paramref name="ids"/> does not match the type of
        /// <typeparamref name="TEntity"/>'s identifier property, or <paramref name="transaction"/> does not belong
        /// to <paramref name="connection"/>.
        /// </exception>
        public static int DeleteRange<TEntity>(this SqlConnection connection, IEnumerable ids, int batchSize = 500, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            batchSize = batchSize > 0 ? Math.Min(batchSize, SqlDialectStrategy.Instance.MaxParameterCount) : SqlDialectStrategy.Instance.MaxParameterCount;
            return DbExecutionStrategy.Instance.DeleteRangeImplAsync<TEntity>(connection, true, ids, batchSize, 0, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Inserts each of the specified entities as a new row, or updates its row if one already exists, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to upsert.</typeparam>
        /// <param name="connection">The connection to execute the upsert on.</param>
        /// <param name="entities">The entities to insert or update.</param>
        /// <param name="batchSize">
        /// The maximum number of entities upserted by a single round trip, capped according to SQL Server's limit of
        /// roughly 2,100 parameters per command. When less than or equal to zero, the maximum number allowed by
        /// the parameter limit is used.
        /// </param>
        /// <param name="transaction">
        /// The transaction to execute the upsert within, or <see langword="null"/> to have this method manage its
        /// own transaction (see remarks).
        /// </param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The total number of rows affected across all round trips.</returns>
        /// <remarks>
        /// Every property that is not marked as database-generated or <see cref="NotMappedAttribute"/>
        /// is included in the <c>INSERT</c> portion of each row;
        /// the <c>UPDATE</c> portion updates the same set of properties, except the identifier and
        /// any property marked <see cref="UpsertKeyAttribute"/>.
        /// <para>
        /// Unlike this library's other providers, each round trip executes two separate statements rather than a
        /// single atomic one: an <c>UPDATE ... FROM</c> that joins the target table to a <c>VALUES</c>-derived
        /// source table, matching rows using the properties marked with
        /// <see cref="UpsertKeyAttribute"/> — or the entity's identifier, if none are
        /// marked — treating two <see langword="null"/> values in a key property as equal, followed by an
        /// <c>INSERT ... WHERE NOT EXISTS</c> that inserts only the entities with no matching row. On a match,
        /// every property is updated except the identifier, any database-generated property, and the key
        /// properties themselves.
        /// </para>
        /// <para>
        /// When no <paramref name="transaction"/> is provided, this method begins one, commits it on success, and
        /// rolls it back if execution fails — ensuring that the entire operation remains atomic regardless of
        /// how many statements or round trips are required. Passing an explicit <paramref name="transaction"/>
        /// opts out of this; committing or rolling back is then the caller's responsibility.
        /// If <paramref name="connection"/> is closed when this method is called and it owns the transaction,
        /// it also opens and closes the connection for the duration of the operation.
        /// </para>
        /// <para>
        /// When several connections upsert overlapping keys at the same moment, the database can choose one of them as the
        /// victim of a deadlock (error 1205) and roll its transaction back. Forget does not retry. If your workload can do
        /// this, handle that error and call again. No key ends up with two rows. When this method owns the transaction, a
        /// failed call applies none of its rows.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static int UpsertRange<TEntity>(this SqlConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            ImmutableArray<PropertyInfo> properties = EntityInfoCache<TEntity>.Properties;
            batchSize = batchSize > 0 ? Math.Min(batchSize, SqlDialectStrategy.Instance.MaxParameterCount / properties.Length) : SqlDialectStrategy.Instance.MaxParameterCount / properties.Length;

            return DbExecutionStrategy.Instance.UpsertRangeImplAsync(connection, true, entities, batchSize, 0, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Determines whether any <typeparamref name="TEntity"/> row exists, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns><see langword="true"/> if any row exists; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static bool Exists<TEntity>(this SqlConnection connection, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.ExistsImplAsync(connection, true, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Determines whether any <typeparamref name="TEntity"/> row matching the specified predicate exists, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the matched row must satisfy. When <see langword="null"/>, any row counts as a match.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns><see langword="true"/> if a matching row exists; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static bool Exists<TEntity>(this SqlConnection connection, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.ExistsImplAsync(connection, true, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Determines whether any <typeparamref name="TEntity"/> row matching the specified filter exists, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the matched row must satisfy. When <see langword="null"/>, any row counts as a match.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns><see langword="true"/> if a matching row exists; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static bool Exists<TEntity>(this SqlConnection connection, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.ExistsImplAsync(connection, true, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Counts every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        public static long Count<TEntity>(this SqlConnection connection, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.CountImplAsync(connection, true, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Counts every <typeparamref name="TEntity"/> row matching the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the counted rows must satisfy. When <see langword="null"/>, every row is counted.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of matching rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static long Count<TEntity>(this SqlConnection connection, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.CountImplAsync(connection, true, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Counts every <typeparamref name="TEntity"/> row matching the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the counted rows must satisfy. When <see langword="null"/>, every row is counted.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of matching rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static long Count<TEntity>(this SqlConnection connection, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.CountImplAsync(connection, true, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows in which the selected property is not
        /// <see langword="null"/>, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to count, for example <c>x =&gt; x.Discount</c>.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows in which the selected property is not <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        public static long Count<TEntity>(this SqlConnection connection, Expression<Func<TEntity, object?>> selector, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.CountImplAsync(connection, true, selector, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows, matching the specified predicate, in which the selected
        /// property is not <see langword="null"/>, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to count, for example <c>x =&gt; x.Discount</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of matching rows in which the selected property is not <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static long Count<TEntity>(this SqlConnection connection, Expression<Func<TEntity, object?>> selector, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.CountImplAsync(connection, true, selector, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows, matching the specified filter, in which the selected
        /// property is not <see langword="null"/>, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to count, for example <c>x =&gt; x.Discount</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of matching rows in which the selected property is not <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static long Count<TEntity>(this SqlConnection connection, Expression<Func<TEntity, object?>> selector, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.CountImplAsync(connection, true, selector, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows in which the named property is not <see langword="null"/>, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to count.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of rows in which the named property is not <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        public static long Count<TEntity>(this SqlConnection connection, string propertyName, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.CountImplAsync(connection, true, propertyName, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows, matching the specified predicate, in which the named
        /// property is not <see langword="null"/>, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to count.</param>
        /// <param name="predicate">
        /// An optional predicate the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of matching rows in which the named property is not <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static long Count<TEntity>(this SqlConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.CountImplAsync(connection, true, propertyName, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows, matching the specified filter, in which the named
        /// property is not <see langword="null"/>, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to count.</param>
        /// <param name="filterNode">
        /// An optional filter the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The number of matching rows in which the named property is not <see langword="null"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static long Count<TEntity>(this SqlConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.CountImplAsync(connection, true, propertyName, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the average of the selected property across every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to average, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The average, or <see langword="null"/> if no row exists.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="AvgCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// <para>
        /// When the averaged column is <c>int</c>/<c>smallint</c>/<c>tinyint</c>/<c>bigint</c>-typed: SQL Server's
        /// own <c>AVG</c> returns the same exact numeric type as its input, so this still returns a truncated,
        /// whole-number result — identical to what a hand-written <c>AVG(column)</c> query returns. This library
        /// does not alter that native behavior.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        public static decimal? Avg<TEntity>(this SqlConnection connection, Expression<Func<TEntity, decimal?>> selector, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.AvgImplAsync(connection, true, selector, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the average of the selected property across every <typeparamref name="TEntity"/> row matching
        /// the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to average, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The average, or <see langword="null"/> if no row matches.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="AvgCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// <para>
        /// When the averaged column is <c>int</c>/<c>smallint</c>/<c>tinyint</c>/<c>bigint</c>-typed: SQL Server's
        /// own <c>AVG</c> returns the same exact numeric type as its input, so this still returns a truncated,
        /// whole-number result — identical to what a hand-written <c>AVG(column)</c> query returns. This library
        /// does not alter that native behavior.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static decimal? Avg<TEntity>(this SqlConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.AvgImplAsync(connection, true, selector, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the average of the selected property across every <typeparamref name="TEntity"/> row matching
        /// the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to average, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The average, or <see langword="null"/> if no row matches.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="AvgCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// <para>
        /// When the averaged column is <c>int</c>/<c>smallint</c>/<c>tinyint</c>/<c>bigint</c>-typed: SQL Server's
        /// own <c>AVG</c> returns the same exact numeric type as its input, so this still returns a truncated,
        /// whole-number result — identical to what a hand-written <c>AVG(column)</c> query returns. This library
        /// does not alter that native behavior.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static decimal? Avg<TEntity>(this SqlConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.AvgImplAsync(connection, true, selector, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the average of the named property across every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to average.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The average, or <see langword="null"/> if no row exists.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="AvgCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// <para>
        /// When the averaged column is <c>int</c>/<c>smallint</c>/<c>tinyint</c>/<c>bigint</c>-typed: SQL Server's
        /// own <c>AVG</c> returns the same exact numeric type as its input, so this still returns a truncated,
        /// whole-number result — identical to what a hand-written <c>AVG(column)</c> query returns. This library
        /// does not alter that native behavior.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        public static decimal? Avg<TEntity>(this SqlConnection connection, string propertyName, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.AvgImplAsync(connection, true, propertyName, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the average of the named property across every <typeparamref name="TEntity"/> row matching the
        /// specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to average.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The average, or <see langword="null"/> if no row matches.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="AvgCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// <para>
        /// When the averaged column is <c>int</c>/<c>smallint</c>/<c>tinyint</c>/<c>bigint</c>-typed: SQL Server's
        /// own <c>AVG</c> returns the same exact numeric type as its input, so this still returns a truncated,
        /// whole-number result — identical to what a hand-written <c>AVG(column)</c> query returns. This library
        /// does not alter that native behavior.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static decimal? Avg<TEntity>(this SqlConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.AvgImplAsync(connection, true, propertyName, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the average of the named property across every <typeparamref name="TEntity"/> row matching the
        /// specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to average.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The average, or <see langword="null"/> if no row matches.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="AvgCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// <para>
        /// When the averaged column is <c>int</c>/<c>smallint</c>/<c>tinyint</c>/<c>bigint</c>-typed: SQL Server's
        /// own <c>AVG</c> returns the same exact numeric type as its input, so this still returns a truncated,
        /// whole-number result — identical to what a hand-written <c>AVG(column)</c> query returns. This library
        /// does not alter that native behavior.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static decimal? Avg<TEntity>(this SqlConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.AvgImplAsync(connection, true, propertyName, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the sum of the selected property across every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to sum, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The sum, or <see langword="null"/> if no row exists.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="SumCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        public static decimal? Sum<TEntity>(this SqlConnection connection, Expression<Func<TEntity, decimal?>> selector, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.SumImplAsync(connection, true, selector, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the sum of the selected property across every <typeparamref name="TEntity"/> row matching the
        /// specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to sum, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The sum, or <see langword="null"/> if no row matches.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="SumCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static decimal? Sum<TEntity>(this SqlConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.SumImplAsync(connection, true, selector, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the sum of the selected property across every <typeparamref name="TEntity"/> row matching the
        /// specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to sum, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The sum, or <see langword="null"/> if no row matches.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="SumCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static decimal? Sum<TEntity>(this SqlConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.SumImplAsync(connection, true, selector, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the sum of the named property across every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to sum.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The sum, or <see langword="null"/> if no row exists.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="SumCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        public static decimal? Sum<TEntity>(this SqlConnection connection, string propertyName, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.SumImplAsync(connection, true, propertyName, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the sum of the named property across every <typeparamref name="TEntity"/> row matching the
        /// specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to sum.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The sum, or <see langword="null"/> if no row matches.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="SumCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static decimal? Sum<TEntity>(this SqlConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.SumImplAsync(connection, true, propertyName, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the sum of the named property across every <typeparamref name="TEntity"/> row matching the
        /// specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to sum.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The sum, or <see langword="null"/> if no row matches.</returns>
        /// <remarks>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="SumCommand{TEntity}(SqlConnection, Expression{Func{TEntity, decimal?}})"/> (and its overloads) — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static decimal? Sum<TEntity>(this SqlConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, true, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
            return DbExecutionStrategy.Instance.SumImplAsync(connection, true, propertyName, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the minimum value of the selected property across every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The minimum value, or <see langword="null"/> if no row exists.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        public static TProperty? Min<TEntity, TProperty>(this SqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MinImplAsync(connection, true, selector, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the minimum value of the selected property across every <typeparamref name="TEntity"/> row
        /// matching the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The minimum value, or <see langword="null"/> if no row matches.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static TProperty? Min<TEntity, TProperty>(this SqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MinImplAsync(connection, true, selector, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the minimum value of the selected property across every <typeparamref name="TEntity"/> row
        /// matching the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The minimum value, or <see langword="null"/> if no row matches.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static TProperty? Min<TEntity, TProperty>(this SqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MinImplAsync(connection, true, selector, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the minimum value of the named property across every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the named property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The minimum value, or <see langword="null"/> if no row exists.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        public static TProperty? Min<TEntity, TProperty>(this SqlConnection connection, string propertyName, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MinImplAsync<TEntity, TProperty>(connection, true, propertyName, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the minimum value of the named property across every <typeparamref name="TEntity"/> row
        /// matching the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the named property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The minimum value, or <see langword="null"/> if no row matches.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static TProperty? Min<TEntity, TProperty>(this SqlConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MinImplAsync<TEntity, TProperty>(connection, true, propertyName, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the minimum value of the named property across every <typeparamref name="TEntity"/> row
        /// matching the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the named property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The minimum value, or <see langword="null"/> if no row matches.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static TProperty? Min<TEntity, TProperty>(this SqlConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MinImplAsync<TEntity, TProperty>(connection, true, propertyName, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the maximum value of the selected property across every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The maximum value, or <see langword="null"/> if no row exists.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        public static TProperty? Max<TEntity, TProperty>(this SqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MaxImplAsync(connection, true, selector, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the maximum value of the selected property across every <typeparamref name="TEntity"/> row
        /// matching the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The maximum value, or <see langword="null"/> if no row matches.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static TProperty? Max<TEntity, TProperty>(this SqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MaxImplAsync(connection, true, selector, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the maximum value of the selected property across every <typeparamref name="TEntity"/> row
        /// matching the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The maximum value, or <see langword="null"/> if no row matches.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static TProperty? Max<TEntity, TProperty>(this SqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MaxImplAsync(connection, true, selector, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the maximum value of the named property across every <typeparamref name="TEntity"/> row, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the named property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The maximum value, or <see langword="null"/> if no row exists.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        public static TProperty? Max<TEntity, TProperty>(this SqlConnection connection, string propertyName, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MaxImplAsync<TEntity, TProperty>(connection, true, propertyName, (IFilterNode<TEntity>?)null, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the maximum value of the named property across every <typeparamref name="TEntity"/> row
        /// matching the specified predicate, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the named property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The maximum value, or <see langword="null"/> if no row matches.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static TProperty? Max<TEntity, TProperty>(this SqlConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MaxImplAsync<TEntity, TProperty>(connection, true, propertyName, predicate, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Computes the maximum value of the named property across every <typeparamref name="TEntity"/> row
        /// matching the specified filter, synchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the named property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <returns>The maximum value, or <see langword="null"/> if no row matches.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static TProperty? Max<TEntity, TProperty>(this SqlConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, SqlTransaction? transaction = null, int? commandTimeout = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbExecutionStrategy.Instance.MaxImplAsync<TEntity, TProperty>(connection, true, propertyName, filterNode, transaction, commandTimeout, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
