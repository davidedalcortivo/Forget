using Forget.Core.Abstractions.Models;
using Forget.Core.Models;
using Forget.MySql.Strategies;
using MySqlConnector;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;


namespace Forget.MySql.Extensions
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
        /// For MySQL, this does no additional work beyond what
        /// <see cref="LoadRuntimeCache{TEntity}(MySqlConnection)"/> already does, since the MySQL provider does not
        /// need to read metadata from the database. This method exists for parity with the other providers,
        /// some of which do use it to preload that metadata; calling either method is equivalent for MySQL.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task LoadDbCacheAsync<TEntity>(this MySqlConnection connection, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>(this MySqlConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<IReadOnlyList<TEntity>> GetAllAsync<TEntity>(this MySqlConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity> GetFirstAsync<TEntity>(this MySqlConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity> GetFirstAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity> GetFirstAsync<TEntity>(this MySqlConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity?> GetFirstOrDefaultAsync<TEntity>(this MySqlConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity?> GetFirstOrDefaultAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity?> GetFirstOrDefaultAsync<TEntity>(this MySqlConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity> GetSingleAsync<TEntity>(this MySqlConnection connection, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity> GetSingleAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity> GetSingleAsync<TEntity>(this MySqlConnection connection, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity?> GetSingleOrDefaultAsync<TEntity>(this MySqlConnection connection, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity?> GetSingleOrDefaultAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity?> GetSingleOrDefaultAsync<TEntity>(this MySqlConnection connection, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TEntity?> GetByIdAsync<TEntity>(this MySqlConnection connection, object id, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<IReadOnlyList<TEntity>> GetPageAsync<TEntity>(this MySqlConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<IReadOnlyList<TEntity>> GetPageAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<IReadOnlyList<TEntity>> GetPageAsync<TEntity>(this MySqlConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<int> UpdateAsync<TEntity>(this MySqlConnection connection, TEntity entity, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<int> UpdateAsync<TEntity>(this MySqlConnection connection, object values, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<int> UpdateAsync<TEntity>(this MySqlConnection connection, object values, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<int> UpdateAsync<TEntity>(this MySqlConnection connection, object values, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<int> InsertAsync<TEntity>(this MySqlConnection connection, TEntity entity, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<int> DeleteAsync<TEntity>(this MySqlConnection connection, TEntity entity, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<int> DeleteAsync<TEntity>(this MySqlConnection connection, object id, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<int> DeleteAsync<TEntity>(this MySqlConnection connection, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<int> DeleteAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<int> DeleteAsync<TEntity>(this MySqlConnection connection, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        /// This relies on MySQL's native <c>INSERT ... ON DUPLICATE KEY UPDATE</c> statement, which detects an
        /// existing row through the table's own <c>UNIQUE</c> or primary key constraints. Marking a property with
        /// <see cref="UpsertKeyAttribute"/> only excludes it from the generated
        /// <c>UPDATE</c> portion of the statement; it does not, by itself, make MySQL treat that column as the
        /// row's key. For this method to update rather than duplicate an existing row, the columns you intend to
        /// match on must already be covered by a <c>UNIQUE</c> or primary key constraint in the database.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="transaction"/> does not belong to <paramref name="connection"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<int> UpsertAsync<TEntity>(this MySqlConnection connection, TEntity entity, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        /// <param name="batchSize">
        /// The maximum number of identifiers queried by a single round trip. When less than or equal to zero, every
        /// identifier is queried in a single round trip.
        /// </param>
        /// <param name="transaction">The transaction to execute the query within, or <see langword="null"/> to execute it outside of an explicit transaction.</param>
        /// <param name="commandTimeout">The number of seconds to wait before timing out, or <see langword="null"/> to use the default timeout.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the rows that exist, each once.
        /// Their order is not defined, and an identifier with no row does not appear in the result, which can therefore be
        /// shorter than <paramref name="ids"/> (or empty).
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
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was canceled.</exception>
        public static async Task<IReadOnlyList<TEntity>> GetByIdRangeAsync<TEntity>(this MySqlConnection connection, IEnumerable ids, int batchSize = 500, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.GetByIdRangeImplAsync<TEntity>(connection, false, ids, batchSize, 0, transaction, commandTimeout, cancellationToken);
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
        public static async Task<int> UpdateRangeAsync<TEntity>(this MySqlConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
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
        public static async Task<int> InsertRangeAsync<TEntity>(this MySqlConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.InsertRangeImplAsync(connection, false, entities, batchSize, 0, transaction, commandTimeout, cancellationToken);
        }

        /// <summary>
        /// Deletes each of the specified entities' rows, asynchronously.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection to execute the delete on.</param>
        /// <param name="entities">The entities whose rows are deleted, matched by their identifiers.</param>
        /// <param name="batchSize">
        /// The maximum number of identifiers included in a single round trip. When less than or equal to zero,
        /// every identifier is included in a single round trip.
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
        public static async Task<int> DeleteRangeAsync<TEntity>(this MySqlConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
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
        /// The maximum number of identifiers included in a single round trip. When less than or equal to zero,
        /// every identifier is included in a single round trip.
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
        public static async Task<int> DeleteRangeAsync<TEntity>(this MySqlConnection connection, IEnumerable ids, int batchSize = 500, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
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
        /// This relies on MySQL's native <c>INSERT ... ON DUPLICATE KEY UPDATE</c> statement, which detects an
        /// existing row through the table's own <c>UNIQUE</c> or primary key constraints. Marking a property with
        /// <see cref="UpsertKeyAttribute"/> only excludes it from the generated
        /// <c>UPDATE</c> portion of the statement; it does not, by itself, make MySQL treat that column as the
        /// row's key. For this method to update rather than duplicate existing rows, the columns you intend to
        /// match on must already be covered by a <c>UNIQUE</c> or primary key constraint in the database.
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
        public static async Task<int> UpsertRangeAsync<TEntity>(this MySqlConnection connection, IEnumerable<TEntity> entities, int batchSize = 500, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
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
        public static async Task<bool> ExistsAsync<TEntity>(this MySqlConnection connection, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<bool> ExistsAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<bool> ExistsAsync<TEntity>(this MySqlConnection connection, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<long> CountAsync<TEntity>(this MySqlConnection connection, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<long> CountAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<long> CountAsync<TEntity>(this MySqlConnection connection, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<long> CountAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, object?>> selector, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<long> CountAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, object?>> selector, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<long> CountAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, object?>> selector, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<long> CountAsync<TEntity>(this MySqlConnection connection, string propertyName, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<long> CountAsync<TEntity>(this MySqlConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<long> CountAsync<TEntity>(this MySqlConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> AvgAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, decimal?>> selector, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> AvgAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> AvgAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> AvgAsync<TEntity>(this MySqlConnection connection, string propertyName, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> AvgAsync<TEntity>(this MySqlConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> AvgAsync<TEntity>(this MySqlConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> SumAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, decimal?>> selector, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> SumAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> SumAsync<TEntity>(this MySqlConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> SumAsync<TEntity>(this MySqlConnection connection, string propertyName, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> SumAsync<TEntity>(this MySqlConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<decimal?> SumAsync<TEntity>(this MySqlConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this MySqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this MySqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this MySqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this MySqlConnection connection, string propertyName, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this MySqlConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MinAsync<TEntity, TProperty>(this MySqlConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this MySqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this MySqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this MySqlConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this MySqlConnection connection, string propertyName, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this MySqlConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
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
        public static async Task<TProperty?> MaxAsync<TEntity, TProperty>(this MySqlConnection connection, string propertyName, IFilterNode<TEntity>? filterNode, MySqlTransaction? transaction = null, int? commandTimeout = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return await DbExecutionStrategy.Instance.MaxImplAsync<TEntity, TProperty>(connection, false, propertyName, filterNode, transaction, commandTimeout, cancellationToken);
        }
    }
}
