namespace Forget.Core.Abstractions.Models
{
    /// <summary>
    /// Represents a node in a filter tree that can be translated into a SQL predicate for <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity type the filter applies to.</typeparam>
    /// <remarks>
    /// This is a marker interface with no members. It is implemented by
    /// <see cref="Core.Models.FilterDescriptor{TEntity}"/>, which represents a single comparison, and
    /// <see cref="Core.Models.FilterGroup{TEntity}"/>, which combines multiple nodes with a logical
    /// operator. Composing instances of these two types allows arbitrarily nested filter trees (equivalent to
    /// grouped <c>AND</c>/<c>OR</c> conditions) to be built and reused without constructing a LINQ expression tree.
    /// <para>
    /// Every query and command method that accepts a predicate also accepts an <see cref="IFilterNode{TEntity}"/> as
    /// an alternative, so filters can be assembled dynamically (for example, from user input) and passed directly
    /// to the library.
    /// </para>
    /// </remarks>
    public interface IFilterNode<TEntity> where TEntity : class
    {

    }
}
