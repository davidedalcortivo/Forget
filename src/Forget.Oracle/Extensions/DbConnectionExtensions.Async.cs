using Forget.Core.Abstractions.Models;
using Forget.Core.Models;
using Forget.Oracle.Strategies;
using Oracle.ManagedDataAccess.Client;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;


namespace Forget.Oracle.Extensions
{
    public static partial class DbConnectionExtensions
    {
        /// <summary>
        /// Ensures the SQL command cache and any database-derived metadata for <typeparamref name="TEntity"/> are
        /// loaded, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to load the cache for.</typeparam>
        /// <param name="connection">The connection used to load the cache.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// Unlike the other providers this library supports, Oracle needs to know each column's actual data type to
        /// build safe <c>CAST</c> expressions for its update-range, insert-range, and upsert-range commands. The
        /// first call for a given <typeparamref name="TEntity"/> and connection queries Oracle's data dictionary
        /// and caches the result; later calls for the same combination return immediately without hitting the
        /// database again. If <paramref name="connection"/> is closed when this runs, it is opened and closed for
        /// the duration of the query.
        /// <para>
        /// Calling this ahead of time is required before using <see cref="UpdateRangeCommands{TEntity}(OracleConnection, IEnumerable{TEntity}, int)"/>,
        /// <see cref="InsertRangeCommands{TEntity}(OracleConnection, IEnumerable{TEntity}, int)"/>, or
        /// <see cref="UpsertRangeCommands{TEntity}(OracleConnection, IEnumerable{TEntity}, int)"/> directly. The
        /// corresponding <c>UpdateRange</c>/<c>UpdateRangeAsync</c>, <c>InsertRange</c>/<c>InsertRangeAsync</c>, and
        /// <c>UpsertRange</c>/<c>UpsertRangeAsync</c> execution methods in this class call it automatically and do not need it called first.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task LoadDbCacheAsync<TEntity>(this OracleConnection connection, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            await DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, false, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, no
        /// explicit ordering is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the matching rows; never
        /// <see langword="null"/>, an empty list if none match.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>(this OracleConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetAllImplAsync(connection, false, (IFilterNode<TEntity>?)null, sortDescriptors, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves every <typeparamref name="TEntity"/> row matching the specified predicate, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the matching rows; never
        /// <see langword="null"/>, an empty list if none match.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetAllImplAsync(connection, false, predicate, sortDescriptors, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves every <typeparamref name="TEntity"/> row matching the specified filter, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the matching rows; never
        /// <see langword="null"/>, an empty list if none match.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetAllImplAsync(connection, false, filterNode, sortDescriptors, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first matching row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">No row exists.</exception>
        public static async Task<TEntity> GetFirstAsync<TEntity>(this OracleConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetFirstImplAsync(connection, false, (IFilterNode<TEntity>?)null, sortDescriptors, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row matching the specified predicate, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first matching row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">No row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<TEntity> GetFirstAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetFirstImplAsync(connection, false, predicate, sortDescriptors, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row matching the specified filter, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the first matching row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">No row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<TEntity> GetFirstAsync<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetFirstImplAsync(connection, false, filterNode, sortDescriptors, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row, or <see langword="null"/> if none exists, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the first matching row, or
        /// <see langword="null"/> if none exists.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<TEntity?> GetFirstOrDefaultAsync<TEntity>(this OracleConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetFirstOrDefaultImplAsync(connection, false, (IFilterNode<TEntity>?)null, sortDescriptors, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row matching the specified predicate, or
        /// <see langword="null"/> if none matches, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the first matching row, or
        /// <see langword="null"/> if none matches.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<TEntity?> GetFirstOrDefaultAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetFirstOrDefaultImplAsync(connection, false, predicate, sortDescriptors, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the first <typeparamref name="TEntity"/> row matching the specified filter, or
        /// <see langword="null"/> if none matches, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the first matching row, or
        /// <see langword="null"/> if none matches.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<TEntity?> GetFirstOrDefaultAsync<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetFirstOrDefaultImplAsync(connection, false, filterNode, sortDescriptors, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the single row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">No row exists, or more than one row exists.</exception>
        public static async Task<TEntity> GetSingleAsync<TEntity>(this OracleConnection connection, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetSingleImplAsync(connection, false, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row matching the specified predicate, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the single matching row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">No row matches, or more than one row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<TEntity> GetSingleAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetSingleImplAsync(connection, false, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row matching the specified filter, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the single matching row.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">No row matches, or more than one row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<TEntity> GetSingleAsync<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetSingleImplAsync(connection, false, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row, or <see langword="null"/> if none exists, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the single row, or
        /// <see langword="null"/> if none exists.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">More than one row exists.</exception>
        public static async Task<TEntity?> GetSingleOrDefaultAsync<TEntity>(this OracleConnection connection, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetSingleOrDefaultImplAsync(connection, false, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row matching the specified predicate, or
        /// <see langword="null"/> if none matches, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the single matching row, or
        /// <see langword="null"/> if none matches.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">More than one row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<TEntity?> GetSingleOrDefaultAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetSingleOrDefaultImplAsync(connection, false, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the single <typeparamref name="TEntity"/> row matching the specified filter, or
        /// <see langword="null"/> if none matches, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the single matching row, or
        /// <see langword="null"/> if none matches.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="InvalidOperationException">More than one row matches.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<TEntity?> GetSingleOrDefaultAsync<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetSingleOrDefaultImplAsync(connection, false, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the <typeparamref name="TEntity"/> row with the specified identifier, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="id">
        /// The identifier of the row to retrieve. Its runtime type must exactly match the type of
        /// <typeparamref name="TEntity"/>'s identifier property.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the matching row, or
        /// <see langword="null"/> if no row has that identifier.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The type of <paramref name="id"/> does not match the type of <typeparamref name="TEntity"/>'s identifier
        /// property, or <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<TEntity?> GetByIdAsync<TEntity>(this OracleConnection connection, object id, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetByIdImplAsync<TEntity>(connection, false, id, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves a page of <typeparamref name="TEntity"/> rows, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the matching rows for the
        /// requested page; never <see langword="null"/>, an empty list if none match.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<IReadOnlyList<TEntity>> GetPageAsync<TEntity>(this OracleConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetPageImplAsync(connection, false, (IFilterNode<TEntity>?)null, sortDescriptors, skip, take, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves a page of <typeparamref name="TEntity"/> rows matching the specified predicate, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the matching rows for the
        /// requested page; never <see langword="null"/>, an empty list if none match.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<IReadOnlyList<TEntity>> GetPageAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetPageImplAsync(connection, false, predicate, sortDescriptors, skip, take, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves a page of <typeparamref name="TEntity"/> rows matching the specified filter, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the matching rows for the
        /// requested page; never <see langword="null"/>, an empty list if none match.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<IReadOnlyList<TEntity>> GetPageAsync<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetPageImplAsync(connection, false, filterNode, sortDescriptors, skip, take, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Updates every column of <paramref name="entity"/>'s row, except its identifier, any
        /// database-generated property, and any property marked <see cref="NotMappedAttribute"/>, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection to execute the update on.</param>
        /// <param name="entity">The entity whose current property values are written back to its row.</param>
        /// <param name="transaction">The transaction to execute the update within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> UpdateAsync<TEntity>(this OracleConnection connection, TEntity entity, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.UpdateImplAsync(connection, false, entity, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Updates the specified columns for every <typeparamref name="TEntity"/> row, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
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
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> UpdateAsync<TEntity>(this OracleConnection connection, object values, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.UpdateImplAsync(connection, false, values, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Updates the specified columns for every <typeparamref name="TEntity"/> row matching the specified
        /// predicate, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
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
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<int> UpdateAsync<TEntity>(this OracleConnection connection, object values, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.UpdateImplAsync(connection, false, values, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Updates the specified columns for every <typeparamref name="TEntity"/> row matching the specified
        /// filter, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
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
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<int> UpdateAsync<TEntity>(this OracleConnection connection, object values, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.UpdateImplAsync(connection, false, values, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Inserts <paramref name="entity"/> as a new row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to insert.</typeparam>
        /// <param name="connection">The connection to execute the insert on.</param>
        /// <param name="entity">The entity to insert.</param>
        /// <param name="transaction">The transaction to execute the insert within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        /// <remarks>
        /// Every property that is not marked <see cref="NotMappedAttribute"/> or as database-generated
        /// is included in the generated <c>INSERT</c>, including the identifier property
        /// unless it is itself database-generated.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> InsertAsync<TEntity>(this OracleConnection connection, TEntity entity, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.InsertImplAsync(connection, false, entity, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Deletes <paramref name="entity"/>'s row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="entity">The entity whose row is deleted, matched by its identifier.</param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> DeleteAsync<TEntity>(this OracleConnection connection, TEntity entity, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.DeleteImplAsync(connection, false, entity, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Deletes the <typeparamref name="TEntity"/> row with the specified identifier, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="id">
        /// The identifier of the row to delete. Its runtime type must exactly match the type of
        /// <typeparamref name="TEntity"/>'s identifier property.
        /// </param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The type of <paramref name="id"/> does not match the type of <typeparamref name="TEntity"/>'s identifier
        /// property, or <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> DeleteAsync<TEntity>(this OracleConnection connection, object id, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.DeleteImplAsync<TEntity>(connection, false, id, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Deletes every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete rows from.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        /// <remarks>
        /// This deletes every row in the table. Use one of the overloads that accepts a predicate or a filter to
        /// restrict which rows are deleted.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> DeleteAsync<TEntity>(this OracleConnection connection, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.DeleteImplAsync(connection, false, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Deletes every <typeparamref name="TEntity"/> row matching the specified predicate, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete rows from.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="predicate">
        /// An optional predicate the deleted rows must satisfy. When <see langword="null"/>, every row is deleted.
        /// </param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<int> DeleteAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.DeleteImplAsync(connection, false, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Deletes every <typeparamref name="TEntity"/> row matching the specified filter, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete rows from.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="filterNode">
        /// An optional filter the deleted rows must satisfy. When <see langword="null"/>, every row is deleted.
        /// </param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<int> DeleteAsync<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.DeleteImplAsync(connection, false, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Inserts <paramref name="entity"/> as a new row, or updates its row if one already exists, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to upsert.</typeparam>
        /// <param name="connection">The connection to execute the upsert on.</param>
        /// <param name="entity">The entity to insert or update.</param>
        /// <param name="transaction">The transaction to execute the upsert within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows affected.</returns>
        /// <remarks>
        /// Every property that is not marked as database-generated or <see cref="NotMappedAttribute"/>
        /// is included in the <c>INSERT</c> portion of the statement;
        /// the <c>UPDATE</c> portion updates the same set of properties, except the identifier and
        /// any property marked <see cref="UpsertKeyAttribute"/>.
        /// <para>
        /// This executes a <c>MERGE</c> statement. An existing row is matched using the properties marked with
        /// <see cref="UpsertKeyAttribute"/> — or the entity's identifier, if none are
        /// marked — treating two <see langword="null"/> values in a key property as equal. On a match, every
        /// property is updated except the identifier, any database-generated property, and the key properties
        /// themselves; otherwise, a new row is inserted using every property that is not database-generated.
        /// </para>
        /// <para>
        /// A <c>MERGE</c> is not atomic against a concurrent insert of the same key: when two sessions upsert a key
        /// that does not exist yet at the same moment, both decide it is not matched, and the second insert fails with
        /// <c>ORA-00001</c> (unique constraint violated). Forget does not retry. If your workload can do this, handle that
        /// error and call again: by then the row exists, and it is updated.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> UpsertAsync<TEntity>(this OracleConnection connection, TEntity entity, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.UpsertImplAsync(connection, false, entity, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Retrieves the <typeparamref name="TEntity"/> rows with the specified identifiers, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="ids">
        /// The identifiers of the rows to retrieve. Neither the sequence nor any of its elements may be
        /// <see langword="null"/>, and each identifier's runtime type must exactly match the type of
        /// <typeparamref name="TEntity"/>'s identifier property.
        /// </param>
        /// <param name="preserveDuplicates">
        /// Whether an identifier repeated in <paramref name="ids"/> should produce the same entity more than once
        /// in the result. When <see langword="false"/>, duplicate identifiers are queried once.
        /// </param>
        /// <param name="preserveNulls">
        /// Whether an identifier with no matching row should produce a <see langword="null"/> entry in the result,
        /// keeping the result the same length as (the possibly deduplicated) <paramref name="ids"/>. When
        /// <see langword="false"/>, unmatched identifiers are omitted and the result may be shorter.
        /// </param>
        /// <param name="batchSize">
        /// The maximum number of identifiers queried by a single round trip. When less than or equal to zero, every
        /// identifier is queried in a single round trip.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the matching rows. The
        /// result preserves the order of <paramref name="ids"/>; see <paramref name="preserveDuplicates"/> and
        /// <paramref name="preserveNulls"/> for how duplicates and misses are represented.
        /// </returns>
        /// <remarks>
        /// Oracle limits a single <c>IN</c> clause to 1,000 items. When a round trip is larger than that, it is
        /// built internally from multiple <c>UNION ALL</c>'d sub-queries rather than a single <c>IN</c> clause, so
        /// the number of round trips still depends only on <paramref name="batchSize"/>.
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
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<IReadOnlyList<TEntity?>> GetByIdRangeAsync<TEntity>(this OracleConnection connection, IEnumerable ids, bool preserveDuplicates = false, bool preserveNulls = false, int batchSize = 500, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetByIdRangeImplAsync<TEntity>(connection, false, ids, preserveDuplicates, preserveNulls, batchSize, SqlDialectStrategy.Instance.MaxInValueCount, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Updates every column, except the identifier, any database-generated property, and any property marked
        /// <see cref="NotMappedAttribute"/>, of each of the specified entities' rows, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection to execute the update on.</param>
        /// <param name="entities">The entities whose current property values are written back to their rows.</param>
        /// <param name="batchSize">
        /// The maximum number of entities updated by a single round trip. When less than or equal to zero, every
        /// entity is updated in a single round trip.
        /// </param>
        /// <param name="transaction">The transaction to execute the update within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the total number of rows
        /// affected across all round trips.
        /// </returns>
        /// <remarks>
        /// Each round trip executes a <c>MERGE</c> statement that matches existing rows by identifier and updates
        /// them; rows with no match in the table are left untouched, and no new rows are inserted.
        /// <para>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="UpdateRangeCommands{TEntity}(OracleConnection, IEnumerable{TEntity}, int)"/> — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// </para>
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
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> UpdateRangeAsync<TEntity>(this OracleConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            await DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, false, commandTimeout, cancellationToken);
            return await DbExecutionStrategy.Instance.UpdateRangeImplAsync(connection, false, entities, batchSize, 0, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Inserts each of the specified entities as a new row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to insert.</typeparam>
        /// <param name="connection">The connection to execute the insert on.</param>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="batchSize">
        /// The maximum number of entities inserted by a single round trip. When less than or equal to zero, every
        /// entity is inserted in a single round trip.
        /// </param>
        /// <param name="transaction">The transaction to execute the insert within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the total number of rows
        /// affected across all round trips.
        /// </returns>
        /// <remarks>
        /// Every property that is not marked <see cref="NotMappedAttribute"/> or as database-generated
        /// is included in the generated <c>INSERT</c>, including the identifier property
        /// unless it is itself database-generated.
        /// <para>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="InsertRangeCommands{TEntity}(OracleConnection, IEnumerable{TEntity}, int)"/> — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
        /// </para>
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
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> InsertRangeAsync<TEntity>(this OracleConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            await DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, false, commandTimeout, cancellationToken);
            return await DbExecutionStrategy.Instance.InsertRangeImplAsync(connection, false, entities, batchSize, 0, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Deletes each of the specified entities' rows, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="entities">The entities whose rows are deleted, matched by their identifiers.</param>
        /// <param name="batchSize">
        /// The maximum number of identifiers included in a single round trip, capped at 1,000 — Oracle's limit for
        /// a single <c>IN</c> clause. When less than or equal to zero, the cap of 1,000 is used.
        /// </param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the total number of rows
        /// affected across all round trips.
        /// </returns>
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
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> DeleteRangeAsync<TEntity>(this OracleConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            batchSize = batchSize > 0 ? Math.Min(batchSize, SqlDialectStrategy.Instance.MaxInValueCount) : SqlDialectStrategy.Instance.MaxInValueCount;
            return await DbExecutionStrategy.Instance.DeleteRangeImplAsync(connection, false, entities, batchSize, 0, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Deletes the <typeparamref name="TEntity"/> rows with the specified identifiers, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="ids">
        /// The identifiers of the rows to delete. Neither the sequence nor any of its elements may be
        /// <see langword="null"/>, and each identifier's runtime type must exactly match the type of
        /// <typeparamref name="TEntity"/>'s identifier property.
        /// </param>
        /// <param name="batchSize">
        /// The maximum number of identifiers included in a single round trip, capped at 1,000 — Oracle's limit for
        /// a single <c>IN</c> clause. When less than or equal to zero, the cap of 1,000 is used.
        /// </param>
        /// <param name="transaction">The transaction to execute the delete within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the total number of rows
        /// affected across all round trips.
        /// </returns>
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
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> DeleteRangeAsync<TEntity>(this OracleConnection connection, IEnumerable ids, int batchSize = 500, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            batchSize = batchSize > 0 ? Math.Min(batchSize, SqlDialectStrategy.Instance.MaxInValueCount) : SqlDialectStrategy.Instance.MaxInValueCount;
            return await DbExecutionStrategy.Instance.DeleteRangeImplAsync<TEntity>(connection, false, ids, batchSize, 0, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Inserts each of the specified entities as a new row, or updates its row if one already exists, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to upsert.</typeparam>
        /// <param name="connection">The connection to execute the upsert on.</param>
        /// <param name="entities">The entities to insert or update.</param>
        /// <param name="batchSize">
        /// The maximum number of entities upserted by a single round trip. When less than or equal to zero, every
        /// entity is upserted in a single round trip.
        /// </param>
        /// <param name="transaction">The transaction to execute the upsert within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the total number of rows
        /// affected across all round trips.
        /// </returns>
        /// <remarks>
        /// Every property that is not marked as database-generated or <see cref="NotMappedAttribute"/>
        /// is included in the <c>INSERT</c> portion of each row;
        /// the <c>UPDATE</c> portion updates the same set of properties, except the identifier and
        /// any property marked <see cref="UpsertKeyAttribute"/>.
        /// <para>
        /// Each round trip executes a <c>MERGE</c> statement. An existing row is matched using the properties
        /// marked with <see cref="UpsertKeyAttribute"/> — or the entity's identifier, if
        /// none are marked — treating two <see langword="null"/> values in a key property as equal. On a match,
        /// every property is updated except the identifier, any database-generated property, and the key
        /// properties themselves; otherwise, a new row is inserted using every property that is not
        /// database-generated.
        /// </para>
        /// <para>
        /// This automatically ensures the database's metadata for <typeparamref name="TEntity"/> is loaded
        /// on <paramref name="connection"/> before building any commands, so — unlike
        /// <see cref="UpsertRangeCommands{TEntity}(OracleConnection, IEnumerable{TEntity}, int)"/> — calling
        /// <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c> first is not required.
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
        /// A <c>MERGE</c> is not atomic against a concurrent insert of the same key. When two sessions upsert a key
        /// that does not exist yet, both decide it is not matched, and the second insert fails with <c>ORA-00001</c>
        /// (unique constraint violated). Overlapping keys can also make a session the victim of a deadlock
        /// (<c>ORA-00060</c>). Forget does not retry: handle these errors and call again, and the other session's rows
        /// are updated. When this method owns the transaction, a failed call applies none of its rows.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> UpsertRangeAsync<TEntity>(this OracleConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            await DbExecutionStrategy.Instance.LoadDbCacheImplAsync<TEntity>(connection, false, commandTimeout, cancellationToken);
            return await DbExecutionStrategy.Instance.UpsertRangeImplAsync(connection, false, entities, batchSize, 0, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Determines whether any <typeparamref name="TEntity"/> row exists, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is <see langword="true"/> if any row
        /// exists; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<bool> ExistsAsync<TEntity>(this OracleConnection connection, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.ExistsImplAsync(connection, false, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Determines whether any <typeparamref name="TEntity"/> row matching the specified predicate exists, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the matched row must satisfy. When <see langword="null"/>, any row counts as a match.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is <see langword="true"/> if a
        /// matching row exists; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<bool> ExistsAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.ExistsImplAsync(connection, false, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Determines whether any <typeparamref name="TEntity"/> row matching the specified filter exists, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the matched row must satisfy. When <see langword="null"/>, any row counts as a match.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result is <see langword="true"/> if a
        /// matching row exists; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<bool> ExistsAsync<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.ExistsImplAsync(connection, false, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Counts every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<long> CountAsync<TEntity>(this OracleConnection connection, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.CountImplAsync(connection, false, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Counts every <typeparamref name="TEntity"/> row matching the specified predicate, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="predicate">
        /// An optional predicate the counted rows must satisfy. When <see langword="null"/>, every row is counted.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of matching rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<long> CountAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.CountImplAsync(connection, false, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Counts every <typeparamref name="TEntity"/> row matching the specified filter, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="filterNode">
        /// An optional filter the counted rows must satisfy. When <see langword="null"/>, every row is counted.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of matching rows.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<long> CountAsync<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.CountImplAsync(connection, false, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows in which the selected property is not
        /// <see langword="null"/>, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to count, for example <c>x =&gt; x.Discount</c>.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the number of rows in which
        /// the selected property is not <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<long> CountAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, object?>> selector, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.CountImplAsync(connection, false, selector, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows, matching the specified predicate, in which the selected
        /// property is not <see langword="null"/>, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to count, for example <c>x =&gt; x.Discount</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the number of matching rows
        /// in which the selected property is not <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<long> CountAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, object?>> selector, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.CountImplAsync(connection, false, selector, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows, matching the specified filter, in which the selected
        /// property is not <see langword="null"/>, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to count, for example <c>x =&gt; x.Discount</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the number of matching rows
        /// in which the selected property is not <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<long> CountAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, object?>> selector, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.CountImplAsync(connection, false, selector, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows in which the named property is not <see langword="null"/>, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to count.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the number of rows in which
        /// the named property is not <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<long> CountAsync<TEntity>(this OracleConnection connection, string propertyName, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.CountImplAsync(connection, false, propertyName, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows, matching the specified predicate, in which the named
        /// property is not <see langword="null"/>, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to count.</param>
        /// <param name="predicate">
        /// An optional predicate the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the number of matching rows
        /// in which the named property is not <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<long> CountAsync<TEntity>(this OracleConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.CountImplAsync(connection, false, propertyName, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Counts the <typeparamref name="TEntity"/> rows, matching the specified filter, in which the named
        /// property is not <see langword="null"/>, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to count.</param>
        /// <param name="filterNode">
        /// An optional filter the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the number of matching rows
        /// in which the named property is not <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<long> CountAsync<TEntity>(this OracleConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.CountImplAsync(connection, false, propertyName, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the average of the selected property across every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to average, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the average, or
        /// <see langword="null"/> if no row exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<decimal?> AvgAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.AvgImplAsync(connection, false, selector, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the average of the selected property across every <typeparamref name="TEntity"/> row matching
        /// the specified predicate, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to average, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the average, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<decimal?> AvgAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.AvgImplAsync(connection, false, selector, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the average of the selected property across every <typeparamref name="TEntity"/> row matching
        /// the specified filter, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to average, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the average, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<decimal?> AvgAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.AvgImplAsync(connection, false, selector, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the average of the named property across every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to average.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the average, or
        /// <see langword="null"/> if no row exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<decimal?> AvgAsync<TEntity>(this OracleConnection connection, string propertyName, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.AvgImplAsync(connection, false, propertyName, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the average of the named property across every <typeparamref name="TEntity"/> row matching the
        /// specified predicate, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to average.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the average, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<decimal?> AvgAsync<TEntity>(this OracleConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.AvgImplAsync(connection, false, propertyName, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the average of the named property across every <typeparamref name="TEntity"/> row matching the
        /// specified filter, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to average.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the average, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OverflowException">The computed average does not fit in a <see cref="decimal"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<decimal?> AvgAsync<TEntity>(this OracleConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.AvgImplAsync(connection, false, propertyName, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the selected property across every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to sum, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the sum, or
        /// <see langword="null"/> if no row exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<decimal?> SumAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.SumImplAsync(connection, false, selector, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the selected property across every <typeparamref name="TEntity"/> row matching the
        /// specified predicate, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to sum, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the sum, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<decimal?> SumAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.SumImplAsync(connection, false, selector, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the selected property across every <typeparamref name="TEntity"/> row matching the
        /// specified filter, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to sum, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the sum, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<decimal?> SumAsync<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.SumImplAsync(connection, false, selector, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the named property across every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to sum.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the sum, or
        /// <see langword="null"/> if no row exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<decimal?> SumAsync<TEntity>(this OracleConnection connection, string propertyName, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.SumImplAsync(connection, false, propertyName, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the named property across every <typeparamref name="TEntity"/> row matching the
        /// specified predicate, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to sum.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the sum, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<decimal?> SumAsync<TEntity>(this OracleConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.SumImplAsync(connection, false, propertyName, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the sum of the named property across every <typeparamref name="TEntity"/> row matching the
        /// specified filter, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to sum.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the sum, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or
        /// <paramref name="transaction"/> does not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<decimal?> SumAsync<TEntity>(this OracleConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.SumImplAsync(connection, false, propertyName, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the minimum value of the selected property across every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the minimum value, or
        /// <see langword="null"/> if no row exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MinImplAsync(connection, false, selector, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the minimum value of the selected property across every <typeparamref name="TEntity"/> row
        /// matching the specified predicate, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the minimum value, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MinImplAsync(connection, false, selector, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the minimum value of the selected property across every <typeparamref name="TEntity"/> row
        /// matching the specified filter, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the minimum value, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MinImplAsync(connection, false, selector, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the minimum value of the named property across every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the named property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the minimum value, or
        /// <see langword="null"/> if no row exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this OracleConnection connection, string propertyName, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MinImplAsync<TEntity, TProperty>(connection, false, propertyName, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the minimum value of the named property across every <typeparamref name="TEntity"/> row
        /// matching the specified predicate, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the minimum value, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this OracleConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MinImplAsync<TEntity, TProperty>(connection, false, propertyName, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the minimum value of the named property across every <typeparamref name="TEntity"/> row
        /// matching the specified filter, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the minimum value, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this OracleConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MinImplAsync<TEntity, TProperty>(connection, false, propertyName, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the maximum value of the selected property across every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the maximum value, or
        /// <see langword="null"/> if no row exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MaxImplAsync(connection, false, selector, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the maximum value of the selected property across every <typeparamref name="TEntity"/> row
        /// matching the specified predicate, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the maximum value, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MaxImplAsync(connection, false, selector, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the maximum value of the selected property across every <typeparamref name="TEntity"/> row
        /// matching the specified filter, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the maximum value, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or <paramref name="transaction"/> does
        /// not belong to <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MaxImplAsync(connection, false, selector, filterNode, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the maximum value of the named property across every <typeparamref name="TEntity"/> row, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the named property.</typeparam>
        /// <param name="connection">The connection to query.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the maximum value, or
        /// <see langword="null"/> if no row exists.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this OracleConnection connection, string propertyName, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MaxImplAsync<TEntity, TProperty>(connection, false, propertyName, (IFilterNode<TEntity>?)null, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the maximum value of the named property across every <typeparamref name="TEntity"/> row
        /// matching the specified predicate, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the maximum value, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this OracleConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MaxImplAsync<TEntity, TProperty>(connection, false, propertyName, predicate, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Computes the maximum value of the named property across every <typeparamref name="TEntity"/> row
        /// matching the specified filter, asynchronously.
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
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the maximum value, or
        /// <see langword="null"/> if no row matches.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, its type
        /// does not match <typeparamref name="TProperty"/>, or <paramref name="transaction"/> does not belong to
        /// <paramref name="connection"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this OracleConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, OracleTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MaxImplAsync<TEntity, TProperty>(connection, false, propertyName, filterNode, transaction, commandTimeout, cancellationToken);
        }
    }
}
