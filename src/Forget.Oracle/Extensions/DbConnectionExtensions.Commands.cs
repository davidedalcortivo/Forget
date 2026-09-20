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
        /// Ensures the cached SQL commands and Oracle dialect settings for <typeparamref name="TEntity"/> are initialized.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to initialize the cache for.</typeparam>
        /// <param name="connection">The connection used to resolve Oracle-specific settings, such as the default schema.</param>
        /// <remarks>
        /// Every other extension method in this class calls this method automatically before doing its own work, so
        /// calling it directly is only useful to pay the one-time initialization cost ahead of time (for example,
        /// during application startup) rather than on the first real call.
        /// <para>
        /// This is distinct from <c>LoadDbCache</c>/<c>LoadDbCacheAsync</c>, which additionally reads
        /// metadata from the database. Only <see cref="UpdateRangeCommands{TEntity}(OracleConnection, IEnumerable{TEntity}, int)"/>,
        /// <see cref="InsertRangeCommands{TEntity}(OracleConnection, IEnumerable{TEntity}, int)"/>, and
        /// <see cref="UpsertRangeCommands{TEntity}(OracleConnection, IEnumerable{TEntity}, int)"/> require that metadata to have
        /// already been loaded; the other commands in this class do not.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static void LoadRuntimeCache<TEntity>(this OracleConnection connection) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
        }

        /// <summary>
        /// Builds the command that retrieves every <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, no
        /// explicit ordering is applied.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static DbCommandInfo GetAllCommand<TEntity>(this OracleConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetAllCommand(connection, (IFilterNode<TEntity>?)null, sortDescriptors);
        }

        /// <summary>
        /// Builds the command that retrieves every <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="predicate">
        /// An optional predicate the returned rows must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, no
        /// explicit ordering is applied.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo GetAllCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetAllCommand(connection, predicate, sortDescriptors);
        }

        /// <summary>
        /// Builds the command that retrieves every <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="filterNode">
        /// An optional filter the returned rows must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, no
        /// explicit ordering is applied.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo GetAllCommand<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetAllCommand(connection, filterNode, sortDescriptors);
        }

        /// <summary>
        /// Builds the command that retrieves the first <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// This overload and the corresponding <c>GetFirstOrDefaultCommand</c> overload generate the same SQL; the
        /// difference between requiring a match and allowing none only matters when the command is executed.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static DbCommandInfo GetFirstCommand<TEntity>(this OracleConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetFirstCommand(connection, (IFilterNode<TEntity>?)null, sortDescriptors);
        }

        /// <summary>
        /// Builds the command that retrieves the first <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="predicate">
        /// An optional predicate the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// This overload and the corresponding <c>GetFirstOrDefaultCommand</c> overload generate the same SQL; the
        /// difference between requiring a match and allowing none only matters when the command is executed.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo GetFirstCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetFirstCommand(connection, predicate, sortDescriptors);
        }

        /// <summary>
        /// Builds the command that retrieves the first <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="filterNode">
        /// An optional filter the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// This overload and the corresponding <c>GetFirstOrDefaultCommand</c> overload generate the same SQL; the
        /// difference between requiring a match and allowing none only matters when the command is executed.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo GetFirstCommand<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetFirstCommand(connection, filterNode, sortDescriptors);
        }

        /// <summary>
        /// Builds the command that retrieves the first <typeparamref name="TEntity"/> row, or none.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// This overload and the corresponding <c>GetFirstCommand</c> overload generate the same SQL; the difference
        /// between requiring a match and allowing none only matters when the command is executed.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static DbCommandInfo GetFirstOrDefaultCommand<TEntity>(this OracleConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetFirstOrDefaultCommand(connection, (IFilterNode<TEntity>?)null, sortDescriptors);
        }

        /// <summary>
        /// Builds the command that retrieves the first <typeparamref name="TEntity"/> row matching the specified
        /// predicate, or none.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="predicate">
        /// An optional predicate the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// This overload and the corresponding <c>GetFirstCommand</c> overload generate the same SQL; the difference
        /// between requiring a match and allowing none only matters when the command is executed.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo GetFirstOrDefaultCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetFirstOrDefaultCommand(connection, predicate, sortDescriptors);
        }

        /// <summary>
        /// Builds the command that retrieves the first <typeparamref name="TEntity"/> row matching the specified
        /// filter, or none.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="filterNode">
        /// An optional filter the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <param name="sortDescriptors">
        /// The properties to sort the results by, in order of precedence. When <see langword="null"/> or empty, the
        /// results are ordered by the identifier property instead, to ensure a deterministic result.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// This overload and the corresponding <c>GetFirstCommand</c> overload generate the same SQL; the difference
        /// between requiring a match and allowing none only matters when the command is executed.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo GetFirstOrDefaultCommand<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetFirstOrDefaultCommand(connection, filterNode, sortDescriptors);
        }

        /// <summary>
        /// Builds the command that retrieves the single <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command does not limit the number of returned rows at the SQL level; enforcing that
        /// exactly one row is returned is the responsibility of whichever code executes the command (for example,
        /// Dapper's <c>QuerySingle</c>).
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static DbCommandInfo GetSingleCommand<TEntity>(this OracleConnection connection) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetSingleCommand(connection, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that retrieves the single <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="predicate">
        /// An optional predicate the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command does not limit the number of returned rows at the SQL level; enforcing that
        /// exactly one row is returned is the responsibility of whichever code executes the command (for example,
        /// Dapper's <c>QuerySingle</c>).
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo GetSingleCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetSingleCommand(connection, predicate);
        }

        /// <summary>
        /// Builds the command that retrieves the single <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="filterNode">
        /// An optional filter the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command does not limit the number of returned rows at the SQL level; enforcing that
        /// exactly one row is returned is the responsibility of whichever code executes the command (for example,
        /// Dapper's <c>QuerySingle</c>).
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo GetSingleCommand<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetSingleCommand(connection, filterNode);
        }

        /// <summary>
        /// Builds the command that retrieves the single <typeparamref name="TEntity"/> row, or none.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command does not limit the number of returned rows at the SQL level; enforcing that at
        /// most one row is returned is the responsibility of whichever code executes the command (for example,
        /// Dapper's <c>QuerySingleOrDefault</c>).
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static DbCommandInfo GetSingleOrDefaultCommand<TEntity>(this OracleConnection connection) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetSingleOrDefaultCommand(connection, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that retrieves the single <typeparamref name="TEntity"/> row matching the specified
        /// predicate, or none.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="predicate">
        /// An optional predicate the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command does not limit the number of returned rows at the SQL level; enforcing that at
        /// most one row is returned is the responsibility of whichever code executes the command (for example,
        /// Dapper's <c>QuerySingleOrDefault</c>).
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo GetSingleOrDefaultCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetSingleOrDefaultCommand(connection, predicate);
        }

        /// <summary>
        /// Builds the command that retrieves the single <typeparamref name="TEntity"/> row matching the specified
        /// filter, or none.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="filterNode">
        /// An optional filter the returned row must satisfy. When <see langword="null"/>, no filter is applied.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command does not limit the number of returned rows at the SQL level; enforcing that at
        /// most one row is returned is the responsibility of whichever code executes the command (for example,
        /// Dapper's <c>QuerySingleOrDefault</c>).
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo GetSingleOrDefaultCommand<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetSingleOrDefaultCommand(connection, filterNode);
        }

        /// <summary>
        /// Builds the command that retrieves the <typeparamref name="TEntity"/> row with the specified identifier.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="id">
        /// The identifier of the row to retrieve. Its type must be compatible with the type of
        /// <typeparamref name="TEntity"/>'s identifier property (for instance the same type,
        /// or a <c>short</c>, <c>int</c> or <c>long</c> for an integer key).
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The type of <paramref name="id"/> is not compatible with the type of <typeparamref name="TEntity"/>'s identifier
        /// property.
        /// </exception>
        public static DbCommandInfo GetByIdCommand<TEntity>(this OracleConnection connection, object id) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetByIdCommand<TEntity>(connection, id);
        }

        /// <summary>
        /// Builds the command that retrieves a page of <typeparamref name="TEntity"/> rows.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
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
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// When <paramref name="skip"/> is <see langword="null"/> or negative and <paramref name="take"/> is
        /// specified, this generates the same command as the corresponding <c>GetFirstCommand</c> overload, using
        /// <paramref name="take"/> in place of a fixed limit of one row.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static DbCommandInfo GetPageCommand<TEntity>(this OracleConnection connection, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetPageCommand(connection, (IFilterNode<TEntity>?)null, sortDescriptors, skip, take);
        }

        /// <summary>
        /// Builds the command that retrieves a page of <typeparamref name="TEntity"/> rows matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
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
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// When <paramref name="skip"/> is <see langword="null"/> or negative and <paramref name="take"/> is
        /// specified, this generates the same command as the corresponding <c>GetFirstCommand</c> overload, using
        /// <paramref name="take"/> in place of a fixed limit of one row.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo GetPageCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetPageCommand(connection, predicate, sortDescriptors, skip, take);
        }

        /// <summary>
        /// Builds the command that retrieves a page of <typeparamref name="TEntity"/> rows matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
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
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// When <paramref name="skip"/> is <see langword="null"/> or negative and <paramref name="take"/> is
        /// specified, this generates the same command as the corresponding <c>GetFirstCommand</c> overload, using
        /// <paramref name="take"/> in place of a fixed limit of one row.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo GetPageCommand<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode, IEnumerable<SortDescriptor<TEntity>>? sortDescriptors = null, int? skip = null, int? take = null) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetPageCommand(connection, filterNode, sortDescriptors, skip, take);
        }

        /// <summary>
        /// Builds the command that updates every column of <paramref name="entity"/>'s row, except its identifier,
        /// any database-generated property, and any property marked <see cref="NotMappedAttribute"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="entity">The entity whose current property values are written back to its row.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the update.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        public static DbCommandInfo UpdateCommand<TEntity>(this OracleConnection connection, TEntity entity) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.UpdateCommand(connection, entity);
        }

        /// <summary>
        /// Builds the command that updates the specified columns for every <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="values">
        /// An object whose public properties specify the columns to update and their new values. A property that is
        /// the identifier — either one named "Id" (case-insensitive), or the property marked with
        /// <see cref="KeyAttribute"/> — or is marked as database-generated or <see cref="NotMappedAttribute"/>,
        /// is silently ignored rather than treated as a column to update.
        /// Every other property must correspond to an updatable property of <typeparamref name="TEntity"/>,
        /// and its type must fit that property's type: the same type, or a narrower numeric one (an <c>int</c> for a
        /// <c>long</c> property, for instance).
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the update.</returns>
        /// <remarks>
        /// The generated command has no <c>WHERE</c> clause and updates every row in the table. Use one of the
        /// overloads that also accepts a predicate or a filter to restrict which rows are updated.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="values"/> has no properties left to update once its identifier, database-generated, and
        /// <see cref="NotMappedAttribute"/>-marked properties are excluded; or one of its remaining properties
        /// does not correspond to an updatable property of <typeparamref name="TEntity"/>, or
        /// a remaining property's type does not fit the corresponding property's type.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// More than one property of <paramref name="values"/> is marked with <see cref="KeyAttribute"/>, or
        /// one of its property names is reserved for internal use by this library.
        /// </exception>
        public static DbCommandInfo UpdateCommand<TEntity>(this OracleConnection connection, object values) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.UpdateCommand(connection, values, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that updates the specified columns for every <typeparamref name="TEntity"/> row
        /// matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="values">
        /// An object whose public properties specify the columns to update and their new values. A property that is
        /// the identifier — either one named "Id" (case-insensitive), or the property marked with
        /// <see cref="KeyAttribute"/> — or is marked as database-generated or <see cref="NotMappedAttribute"/>,
        /// is silently ignored rather than treated as a column to update.
        /// Every other property must correspond to an updatable property of <typeparamref name="TEntity"/>,
        /// and its type must fit that property's type: the same type, or a narrower numeric one (an <c>int</c> for a
        /// <c>long</c> property, for instance).
        /// </param>
        /// <param name="predicate">
        /// An optional predicate the updated rows must satisfy. When <see langword="null"/>, every row is updated.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the update.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="values"/> has no properties left to update once its identifier, database-generated, and
        /// <see cref="NotMappedAttribute"/>-marked properties are excluded; or one of its remaining properties
        /// does not correspond to an updatable property of <typeparamref name="TEntity"/>, or
        /// a remaining property's type does not fit the corresponding property's type.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// More than one property of <paramref name="values"/> is marked with <see cref="KeyAttribute"/>, or
        /// one of its property names is reserved for internal use by this library.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo UpdateCommand<TEntity>(this OracleConnection connection, object values, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.UpdateCommand(connection, values, predicate);
        }

        /// <summary>
        /// Builds the command that updates the specified columns for every <typeparamref name="TEntity"/> row
        /// matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="values">
        /// An object whose public properties specify the columns to update and their new values. A property that is
        /// the identifier — either one named "Id" (case-insensitive), or the property marked with
        /// <see cref="KeyAttribute"/> — or is marked as database-generated or <see cref="NotMappedAttribute"/>,
        /// is silently ignored rather than treated as a column to update.
        /// Every other property must correspond to an updatable property of <typeparamref name="TEntity"/>,
        /// and its type must fit that property's type: the same type, or a narrower numeric one (an <c>int</c> for a
        /// <c>long</c> property, for instance).
        /// </param>
        /// <param name="filterNode">
        /// An optional filter the updated rows must satisfy. When <see langword="null"/>, every row is updated.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the update.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="values"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="values"/> has no properties left to update once its identifier, database-generated, and
        /// <see cref="NotMappedAttribute"/>-marked properties are excluded; or one of its remaining properties
        /// does not correspond to an updatable property of <typeparamref name="TEntity"/>, or
        /// a remaining property's type does not fit the corresponding property's type.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// More than one property of <paramref name="values"/> is marked with <see cref="KeyAttribute"/>, or
        /// one of its property names is reserved for internal use by this library.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo UpdateCommand<TEntity>(this OracleConnection connection, object values, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.UpdateCommand(connection, values, filterNode);
        }

        /// <summary>
        /// Builds the command that inserts <paramref name="entity"/> as a new row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to insert.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="entity">The entity to insert.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the insert.</returns>
        /// <remarks>
        /// Every property that is not marked <see cref="NotMappedAttribute"/> or as database-generated
        /// is included in the generated <c>INSERT</c>, including the identifier property
        /// unless it is itself database-generated.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        public static DbCommandInfo InsertCommand<TEntity>(this OracleConnection connection, TEntity entity) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.InsertCommand(connection, entity);
        }

        /// <summary>
        /// Builds the command that deletes <paramref name="entity"/>'s row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="entity">The entity whose row is deleted, matched by its identifier.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the delete.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        public static DbCommandInfo DeleteCommand<TEntity>(this OracleConnection connection, TEntity entity) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.DeleteCommand(connection, entity);
        }

        /// <summary>
        /// Builds the command that deletes the <typeparamref name="TEntity"/> row with the specified identifier.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="id">
        /// The identifier of the row to delete. Its type must be compatible with the type of
        /// <typeparamref name="TEntity"/>'s identifier property (for instance the same type,
        /// or a <c>short</c>, <c>int</c> or <c>long</c> for an integer key).
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the delete.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="id"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The type of <paramref name="id"/> is not compatible with the type of <typeparamref name="TEntity"/>'s identifier
        /// property.
        /// </exception>
        public static DbCommandInfo DeleteCommand<TEntity>(this OracleConnection connection, object id) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.DeleteCommand<TEntity>(connection, id);
        }

        /// <summary>
        /// Builds the command that deletes every <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete rows from.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the delete.</returns>
        /// <remarks>
        /// The generated command has no <c>WHERE</c> clause and deletes every row in the table. Use one of the
        /// overloads that accepts a predicate or a filter to restrict which rows are deleted.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static DbCommandInfo DeleteCommand<TEntity>(this OracleConnection connection) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.DeleteCommand(connection, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that deletes every <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete rows from.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="predicate">
        /// An optional predicate the deleted rows must satisfy. When <see langword="null"/>, every row is deleted.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the delete.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo DeleteCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.DeleteCommand(connection, predicate);
        }

        /// <summary>
        /// Builds the command that deletes every <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete rows from.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="filterNode">
        /// An optional filter the deleted rows must satisfy. When <see langword="null"/>, every row is deleted.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the delete.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo DeleteCommand<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.DeleteCommand(connection, filterNode);
        }

        /// <summary>
        /// Builds the command that inserts <paramref name="entity"/> as a new row, or updates its row if one
        /// already exists.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to upsert.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="entity">The entity to insert or update.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the upsert.</returns>
        /// <remarks>
        /// Every property that is not marked as database-generated or <see cref="NotMappedAttribute"/>
        /// is included in the <c>INSERT</c> portion of the statement;
        /// the <c>UPDATE</c> portion updates the same set of properties, except the identifier and
        /// any property marked <see cref="UpsertKeyAttribute"/>.
        /// <para>
        /// The generated command is a <c>MERGE</c> statement. An existing row is matched using the properties
        /// marked with <see cref="UpsertKeyAttribute"/> — or the entity's identifier, if
        /// none are marked — treating two <see langword="null"/> values in a key property as equal. On a match,
        /// every property is updated except the identifier, any database-generated property, and the key
        /// properties themselves; otherwise, a new row is inserted using every property that is not
        /// database-generated.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entity"/> is <see langword="null"/>.
        /// </exception>
        public static DbCommandInfo UpsertCommand<TEntity>(this OracleConnection connection, TEntity entity) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.UpsertCommand(connection, entity);
        }

        /// <summary>
        /// Builds the commands that retrieve the <typeparamref name="TEntity"/> rows with the specified identifiers.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the commands.</param>
        /// <param name="ids">
        /// The identifiers of the rows to retrieve. Neither the sequence nor any of its elements may be
        /// <see langword="null"/>, and all of them must have the same type, which must be compatible with the type of
        /// <typeparamref name="TEntity"/>'s identifier property (for instance the same type,
        /// or a <c>short</c>, <c>int</c> or <c>long</c> for an integer key).
        /// </param>
        /// <param name="batchSize">
        /// The maximum number of identifiers included in a single command. When less than or equal to zero, every
        /// identifier is included in a single command.
        /// </param>
        /// <returns>
        /// The <see cref="DbCommandInfo"/> instances for the query, one per batch of up to <paramref name="batchSize"/>
        /// identifiers.
        /// </returns>
        /// <remarks>
        /// Oracle limits a single <c>IN</c> clause to 1,000 items. When a batch is larger than that, it is built
        /// internally from multiple <c>UNION ALL</c>'d sub-queries rather than a single <c>IN</c> clause, so the
        /// number of commands returned still depends only on <paramref name="batchSize"/>.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="ids"/> is <see langword="null"/>, or one of its
        /// elements is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The elements of <paramref name="ids"/> do not all have the same type, or that type is not compatible with the type of
        /// <typeparamref name="TEntity"/>'s identifier property.
        /// </exception>
        public static IReadOnlyList<DbCommandInfo> GetByIdRangeCommands<TEntity>(this OracleConnection connection, IEnumerable ids, int batchSize = 500) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.GetByIdRangeCommands<TEntity>(connection, ids, batchSize, SqlDialectStrategy.Instance.MaxInValueCount);
        }

        /// <summary>
        /// Builds the commands that update every column, except the identifier, any database-generated property,
        /// and any property marked <see cref="NotMappedAttribute"/>, of each of the specified entities' rows.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to update.</typeparam>
        /// <param name="connection">The connection used to build the commands.</param>
        /// <param name="entities">The entities whose current property values are written back to their rows.</param>
        /// <param name="batchSize">
        /// The maximum number of entities updated by a single command. When less than or equal to zero, every
        /// entity is updated by a single command.
        /// </param>
        /// <returns>
        /// The <see cref="DbCommandInfo"/> instances for the update, one per batch of up to
        /// <paramref name="batchSize"/> entities.
        /// </returns>
        /// <remarks>
        /// Each generated command is a <c>MERGE</c> statement that matches existing rows by identifier and updates
        /// them; it has no <c>WHEN NOT MATCHED</c> clause, so rows with no match in the table are left untouched
        /// and no new rows are inserted.
        /// <para>
        /// Building these commands requires the database's metadata for <typeparamref name="TEntity"/> to
        /// already be loaded on <paramref name="connection"/>. Call <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c>
        /// first; the corresponding <c>UpdateRange</c>/<c>UpdateRangeAsync</c> execution methods do this
        /// automatically and do not have this requirement.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The metadata for <typeparamref name="TEntity"/> has not been loaded on <paramref name="connection"/>.
        /// </exception>
        public static IReadOnlyList<DbCommandInfo> UpdateRangeCommands<TEntity>(this OracleConnection connection, IEnumerable<TEntity> entities, int batchSize = 500) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.UpdateRangeCommands(connection, entities, batchSize, 0);
        }

        /// <summary>
        /// Builds the commands that insert each of the specified entities as a new row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to insert.</typeparam>
        /// <param name="connection">The connection used to build the commands.</param>
        /// <param name="entities">The entities to insert.</param>
        /// <param name="batchSize">
        /// The maximum number of entities inserted by a single command. When less than or equal to zero, every
        /// entity is inserted by a single command.
        /// </param>
        /// <returns>
        /// The <see cref="DbCommandInfo"/> instances for the insert, one per batch of up to
        /// <paramref name="batchSize"/> entities.
        /// </returns>
        /// <remarks>
        /// Every property that is not marked <see cref="NotMappedAttribute"/> or as database-generated
        /// is included in the generated <c>INSERT</c>, including the identifier property
        /// unless it is itself database-generated.
        /// <para>
        /// Building these commands requires the database's metadata for <typeparamref name="TEntity"/> to
        /// already be loaded on <paramref name="connection"/>. Call <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c>
        /// first; the corresponding <c>InsertRange</c>/<c>InsertRangeAsync</c> execution methods do this
        /// automatically and do not have this requirement.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The metadata for <typeparamref name="TEntity"/> has not been loaded on <paramref name="connection"/>.
        /// </exception>
        public static IReadOnlyList<DbCommandInfo> InsertRangeCommands<TEntity>(this OracleConnection connection, IEnumerable<TEntity> entities, int batchSize = 500) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.InsertRangeCommands(connection, entities, batchSize, 0);
        }

        /// <summary>
        /// Builds the commands that delete each of the specified entities' rows.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection used to build the commands.</param>
        /// <param name="entities">The entities whose rows are deleted, matched by their identifiers.</param>
        /// <param name="batchSize">
        /// The maximum number of identifiers included in a single command, capped at 1,000 — Oracle's limit for a
        /// single <c>IN</c> clause. When less than or equal to zero, the cap of 1,000 is used.
        /// </param>
        /// <returns>
        /// The <see cref="DbCommandInfo"/> instances for the delete, one per batch of up to
        /// <paramref name="batchSize"/> identifiers.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/>.
        /// </exception>
        public static IReadOnlyList<DbCommandInfo> DeleteRangeCommands<TEntity>(this OracleConnection connection, IEnumerable<TEntity> entities, int batchSize = 500) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            batchSize = batchSize > 0 ? Math.Min(batchSize, SqlDialectStrategy.Instance.MaxInValueCount) : SqlDialectStrategy.Instance.MaxInValueCount;
            return DbCommandStrategy.Instance.DeleteRangeCommands(connection, entities, batchSize, 0);
        }

        /// <summary>
        /// Builds the commands that delete the <typeparamref name="TEntity"/> rows with the specified identifiers.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to delete.</typeparam>
        /// <param name="connection">The connection used to build the commands.</param>
        /// <param name="ids">
        /// The identifiers of the rows to delete. Neither the sequence nor any of its elements may be
        /// <see langword="null"/>, and all of them must have the same type, which must be compatible with the type of
        /// <typeparamref name="TEntity"/>'s identifier property (for instance the same type,
        /// or a <c>short</c>, <c>int</c> or <c>long</c> for an integer key).
        /// </param>
        /// <param name="batchSize">
        /// The maximum number of identifiers included in a single command, capped at 1,000 — Oracle's limit for a
        /// single <c>IN</c> clause. When less than or equal to zero, the cap of 1,000 is used.
        /// </param>
        /// <returns>
        /// The <see cref="DbCommandInfo"/> instances for the delete, one per batch of up to
        /// <paramref name="batchSize"/> identifiers.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="ids"/> is <see langword="null"/>, or one of its
        /// elements is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The elements of <paramref name="ids"/> do not all have the same type, or that type is not compatible with the type of
        /// <typeparamref name="TEntity"/>'s identifier property.
        /// </exception>
        public static IReadOnlyList<DbCommandInfo> DeleteRangeCommands<TEntity>(this OracleConnection connection, IEnumerable ids, int batchSize = 500) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            batchSize = batchSize > 0 ? Math.Min(batchSize, SqlDialectStrategy.Instance.MaxInValueCount) : SqlDialectStrategy.Instance.MaxInValueCount;
            return DbCommandStrategy.Instance.DeleteRangeCommands<TEntity>(connection, ids, batchSize, 0);
        }

        /// <summary>
        /// Builds the commands that insert each of the specified entities as a new row, or update its row if one
        /// already exists.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to upsert.</typeparam>
        /// <param name="connection">The connection used to build the commands.</param>
        /// <param name="entities">The entities to insert or update.</param>
        /// <param name="batchSize">
        /// The maximum number of entities upserted by a single command. When less than or equal to zero, every
        /// entity is upserted by a single command.
        /// </param>
        /// <returns>
        /// The <see cref="DbCommandInfo"/> instances for the upsert, one per batch of up to
        /// <paramref name="batchSize"/> entities.
        /// </returns>
        /// <remarks>
        /// Every property that is not marked as database-generated or <see cref="NotMappedAttribute"/>
        /// is included in the <c>INSERT</c> portion of each row;
        /// the <c>UPDATE</c> portion updates the same set of properties, except the identifier and
        /// any property marked <see cref="UpsertKeyAttribute"/>.
        /// <para>
        /// Each generated command is a <c>MERGE</c> statement. An existing row is matched using the properties
        /// marked with <see cref="UpsertKeyAttribute"/> — or the entity's identifier, if
        /// none are marked — treating two <see langword="null"/> values in a key property as equal. On a match,
        /// every property is updated except the identifier, any database-generated property, and the key
        /// properties themselves; otherwise, a new row is inserted using every property that is not
        /// database-generated.
        /// </para>
		/// <para>
        /// Building these commands requires the database's metadata for <typeparamref name="TEntity"/> to
        /// already be loaded on <paramref name="connection"/>. Call <c>LoadDbCache</c> or <c>LoadDbCacheAsync</c>
        /// first; the corresponding <c>UpsertRange</c>/<c>UpsertRangeAsync</c> execution methods do this
        /// automatically and do not have this requirement.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="entities"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// The metadata for <typeparamref name="TEntity"/> has not been loaded on <paramref name="connection"/>.
        /// </exception>
        public static IReadOnlyList<DbCommandInfo> UpsertRangeCommands<TEntity>(this OracleConnection connection, IEnumerable<TEntity> entities, int batchSize = 500) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.UpsertRangeCommands(connection, entities, batchSize, 0);
        }

        /// <summary>
        /// Builds the command that determines whether any <typeparamref name="TEntity"/> row exists.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static DbCommandInfo ExistsCommand<TEntity>(this OracleConnection connection) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.ExistsCommand(connection, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that determines whether any <typeparamref name="TEntity"/> row matching the specified
        /// predicate exists.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="predicate">
        /// An optional predicate the matched row must satisfy. When <see langword="null"/>, any row counts as a match.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo ExistsCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.ExistsCommand(connection, predicate);
        }

        /// <summary>
        /// Builds the command that determines whether any <typeparamref name="TEntity"/> row matching the specified
        /// filter exists.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="filterNode">
        /// An optional filter the matched row must satisfy. When <see langword="null"/>, any row counts as a match.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo ExistsCommand<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.ExistsCommand(connection, filterNode);
        }

        /// <summary>
        /// Builds the command that counts every <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        public static DbCommandInfo CountCommand<TEntity>(this OracleConnection connection) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.CountCommand(connection, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that counts every <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="predicate">
        /// An optional predicate the counted rows must satisfy. When <see langword="null"/>, every row is counted.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo CountCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.CountCommand(connection, predicate);
        }

        /// <summary>
        /// Builds the command that counts every <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="filterNode">
        /// An optional filter the counted rows must satisfy. When <see langword="null"/>, every row is counted.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="connection"/> is <see langword="null"/>.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo CountCommand<TEntity>(this OracleConnection connection, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.CountCommand(connection, filterNode);
        }

        /// <summary>
        /// Builds the command that counts the <typeparamref name="TEntity"/> rows in which the selected property is
        /// not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to count, for example <c>x =&gt; x.Discount</c>.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        public static DbCommandInfo CountCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, object?>> selector) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.CountCommand(connection, selector, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that counts the <typeparamref name="TEntity"/> rows, matching the specified predicate,
        /// in which the selected property is not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to count, for example <c>x =&gt; x.Discount</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo CountCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, object?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.CountCommand(connection, selector, predicate);
        }

        /// <summary>
        /// Builds the command that counts the <typeparamref name="TEntity"/> rows, matching the specified filter,
        /// in which the selected property is not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to count, for example <c>x =&gt; x.Discount</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo CountCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, object?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.CountCommand(connection, selector, filterNode);
        }

        /// <summary>
        /// Builds the command that counts the <typeparamref name="TEntity"/> rows in which the named property is not
        /// <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to count.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>.
        /// </exception>
        public static DbCommandInfo CountCommand<TEntity>(this OracleConnection connection, string propertyName) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.CountCommand(connection, propertyName, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that counts the <typeparamref name="TEntity"/> rows, matching the specified predicate,
        /// in which the named property is not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to count.</param>
        /// <param name="predicate">
        /// An optional predicate the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo CountCommand<TEntity>(this OracleConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.CountCommand(connection, propertyName, predicate);
        }

        /// <summary>
        /// Builds the command that counts the <typeparamref name="TEntity"/> rows, matching the specified filter, in
        /// which the named property is not <see langword="null"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to count.</param>
        /// <param name="filterNode">
        /// An optional filter the counted rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo CountCommand<TEntity>(this OracleConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.CountCommand(connection, propertyName, filterNode);
        }

        /// <summary>
        /// Builds the command that computes the average of the selected property across every <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to average, for example <c>x =&gt; x.Price</c>.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command formats the computed average as a string using <c>TO_CHAR</c>; parsing it back
        /// into a <see cref="decimal"/> is the responsibility of whichever code executes the command (the
        /// library's own execution methods do this automatically).
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        public static DbCommandInfo AvgCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.AvgCommand(connection, selector, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that computes the average of the selected property across every
        /// <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to average, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command formats the computed average as a string using <c>TO_CHAR</c>; parsing it back
        /// into a <see cref="decimal"/> is the responsibility of whichever code executes the command (the
        /// library's own execution methods do this automatically).
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo AvgCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.AvgCommand(connection, selector, predicate);
        }

        /// <summary>
        /// Builds the command that computes the average of the selected property across every
        /// <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to average, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command formats the computed average as a string using <c>TO_CHAR</c>; parsing it back
        /// into a <see cref="decimal"/> is the responsibility of whichever code executes the command (the
        /// library's own execution methods do this automatically).
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo AvgCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.AvgCommand(connection, selector, filterNode);
        }

        /// <summary>
        /// Builds the command that computes the average of the named property across every
        /// <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to average.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command formats the computed average as a string using <c>TO_CHAR</c>; parsing it back
        /// into a <see cref="decimal"/> is the responsibility of whichever code executes the command (the
        /// library's own execution methods do this automatically).
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>.
        /// </exception>
        public static DbCommandInfo AvgCommand<TEntity>(this OracleConnection connection, string propertyName) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.AvgCommand(connection, propertyName, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that computes the average of the named property across every
        /// <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to average.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command formats the computed average as a string using <c>TO_CHAR</c>; parsing it back
        /// into a <see cref="decimal"/> is the responsibility of whichever code executes the command (the
        /// library's own execution methods do this automatically).
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo AvgCommand<TEntity>(this OracleConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.AvgCommand(connection, propertyName, predicate);
        }

        /// <summary>
        /// Builds the command that computes the average of the named property across every
        /// <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to average.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <remarks>
        /// The generated command formats the computed average as a string using <c>TO_CHAR</c>; parsing it back
        /// into a <see cref="decimal"/> is the responsibility of whichever code executes the command (the
        /// library's own execution methods do this automatically).
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo AvgCommand<TEntity>(this OracleConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.AvgCommand(connection, propertyName, filterNode);
        }

        /// <summary>
        /// Builds the command that computes the sum of the selected property across every <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to sum, for example <c>x =&gt; x.Price</c>.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        public static DbCommandInfo SumCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.SumCommand(connection, selector, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that computes the sum of the selected property across every
        /// <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to sum, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo SumCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.SumCommand(connection, selector, predicate);
        }

        /// <summary>
        /// Builds the command that computes the sum of the selected property across every
        /// <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to sum, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo SumCommand<TEntity>(this OracleConnection connection, Expression<Func<TEntity, decimal?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.SumCommand(connection, selector, filterNode);
        }

        /// <summary>
        /// Builds the command that computes the sum of the named property across every <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to sum.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>.
        /// </exception>
        public static DbCommandInfo SumCommand<TEntity>(this OracleConnection connection, string propertyName) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.SumCommand(connection, propertyName, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that computes the sum of the named property across every <typeparamref name="TEntity"/>
        /// row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to sum.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo SumCommand<TEntity>(this OracleConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.SumCommand(connection, propertyName, predicate);
        }

        /// <summary>
        /// Builds the command that computes the sum of the named property across every <typeparamref name="TEntity"/>
        /// row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to sum.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo SumCommand<TEntity>(this OracleConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.SumCommand(connection, propertyName, filterNode);
        }

        /// <summary>
        /// Builds the command that computes the minimum value of the selected property across every
        /// <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        public static DbCommandInfo MinCommand<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MinCommand(connection, selector, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that computes the minimum value of the selected property across every
        /// <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo MinCommand<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MinCommand(connection, selector, predicate);
        }

        /// <summary>
        /// Builds the command that computes the minimum value of the selected property across every
        /// <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo MinCommand<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MinCommand(connection, selector, filterNode);
        }

        /// <summary>
        /// Builds the command that computes the minimum value of the named property across every
        /// <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type the result is read as: the property's own type, or a wider numeric type.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or <typeparamref name="TProperty"/>
        /// cannot hold the values of that property.
        /// </exception>
        public static DbCommandInfo MinCommand<TEntity, TProperty>(this OracleConnection connection, string propertyName) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MinCommand<TEntity, TProperty>(connection, propertyName, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that computes the minimum value of the named property across every
        /// <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type the result is read as: the property's own type, or a wider numeric type.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or <typeparamref name="TProperty"/>
        /// cannot hold the values of that property.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo MinCommand<TEntity, TProperty>(this OracleConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MinCommand<TEntity, TProperty>(connection, propertyName, predicate);
        }

        /// <summary>
        /// Builds the command that computes the minimum value of the named property across every
        /// <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type the result is read as: the property's own type, or a wider numeric type.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or <typeparamref name="TProperty"/>
        /// cannot hold the values of that property.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo MinCommand<TEntity, TProperty>(this OracleConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MinCommand<TEntity, TProperty>(connection, propertyName, filterNode);
        }

        /// <summary>
        /// Builds the command that computes the maximum value of the selected property across every
        /// <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        public static DbCommandInfo MaxCommand<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MaxCommand(connection, selector, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that computes the maximum value of the selected property across every
        /// <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo MaxCommand<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MaxCommand(connection, selector, predicate);
        }

        /// <summary>
        /// Builds the command that computes the maximum value of the selected property across every
        /// <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type of the selected property.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="selector">An expression selecting the property to evaluate, for example <c>x =&gt; x.Price</c>.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="selector"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException"><paramref name="selector"/> does not select a simple property.</exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo MaxCommand<TEntity, TProperty>(this OracleConnection connection, Expression<Func<TEntity, TProperty?>> selector, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MaxCommand(connection, selector, filterNode);
        }

        /// <summary>
        /// Builds the command that computes the maximum value of the named property across every
        /// <typeparamref name="TEntity"/> row.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type the result is read as: the property's own type, or a wider numeric type.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or <typeparamref name="TProperty"/>
        /// cannot hold the values of that property.
        /// </exception>
        public static DbCommandInfo MaxCommand<TEntity, TProperty>(this OracleConnection connection, string propertyName) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MaxCommand<TEntity, TProperty>(connection, propertyName, (IFilterNode<TEntity>?)null);
        }

        /// <summary>
        /// Builds the command that computes the maximum value of the named property across every
        /// <typeparamref name="TEntity"/> row matching the specified predicate.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type the result is read as: the property's own type, or a wider numeric type.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="predicate">
        /// An optional predicate the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or <typeparamref name="TProperty"/>
        /// cannot hold the values of that property.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="predicate"/> uses an expression shape that the SQL translator does not support.
        /// </exception>
        public static DbCommandInfo MaxCommand<TEntity, TProperty>(this OracleConnection connection, string propertyName, Expression<Func<TEntity, bool>>? predicate) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MaxCommand<TEntity, TProperty>(connection, propertyName, predicate);
        }

        /// <summary>
        /// Builds the command that computes the maximum value of the named property across every
        /// <typeparamref name="TEntity"/> row matching the specified filter.
        /// </summary>
        /// <typeparam name="TEntity">The entity type to query.</typeparam>
        /// <typeparam name="TProperty">The type the result is read as: the property's own type, or a wider numeric type.</typeparam>
        /// <param name="connection">The connection used to build the command.</param>
        /// <param name="propertyName">The name of the property to evaluate.</param>
        /// <param name="filterNode">
        /// An optional filter the considered rows must satisfy. When <see langword="null"/>, every row is considered.
        /// </param>
        /// <returns>The <see cref="DbCommandInfo"/> for the query.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="connection"/> or <paramref name="propertyName"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or <typeparamref name="TProperty"/>
        /// cannot hold the values of that property.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="filterNode"/> is not one of the node types defined by this library, and the SQL
        /// translator does not know how to translate it.
        /// </exception>
        public static DbCommandInfo MaxCommand<TEntity, TProperty>(this OracleConnection connection, string propertyName, IFilterNode<TEntity>? filterNode) where TEntity : class
        {
            DbCommandStrategy.Instance.LoadRuntimeCache<TEntity>(connection);
            return DbCommandStrategy.Instance.MaxCommand<TEntity, TProperty>(connection, propertyName, filterNode);
        }
    }
}
