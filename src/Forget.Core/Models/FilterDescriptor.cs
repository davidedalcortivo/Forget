using Forget.Core.Abstractions.Models;
using Forget.Core.Utilities;
using System.Linq.Expressions;
using System.Reflection;


namespace Forget.Core.Models
{
    /// <summary>
    /// Represents a single comparison between an entity property and a value, as a leaf node in a filter tree.
    /// </summary>
    /// <typeparam name="TEntity">The entity type the filter applies to.</typeparam>
    /// <remarks>
    /// A <see cref="FilterDescriptor{TEntity}"/> is the leaf node of an <see cref="IFilterNode{TEntity}"/> tree.
    /// Combine multiple descriptors under a <see cref="FilterGroup{TEntity}"/> to build compound <c>AND</c>/<c>OR</c>
    /// conditions.
    /// <para>
    /// Construction only validates that the type of the provided value is compatible with the selected property's
    /// type (the same type, a <c>short</c>, <c>int</c> or <c>long</c> for an integer property, or one of them for a
    /// decimal, for instance), and never converts the value. Whether
    /// the combination of <see cref="Models.ComparisonOperator"/> and <see cref="Value"/> is
    /// actually supported — for example, <see cref="ComparisonOperator.In"/> requires a non-string collection, while
    /// <see cref="ComparisonOperator.Contains"/>, <see cref="ComparisonOperator.StartsWith"/> and
    /// <see cref="ComparisonOperator.EndsWith"/> require a <see cref="string"/> value — is only checked when the
    /// filter is translated into SQL, where an unsupported combination throws <see cref="NotSupportedException"/>.
    /// </para>
    /// </remarks>
    public sealed class FilterDescriptor<TEntity> : IFilterNode<TEntity> where TEntity : class
    {
        /// <summary>
        /// Gets the name of the entity property this filter compares.
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Gets the value <see cref="PropertyName"/> is compared against.
        /// </summary>
        /// <remarks>
        /// For <see cref="ComparisonOperator.In"/>, this is expected to be a non-string collection of values. For
        /// <see cref="ComparisonOperator.Contains"/>, <see cref="ComparisonOperator.StartsWith"/> and
        /// <see cref="ComparisonOperator.EndsWith"/>, it is expected to be a <see cref="string"/>.
        /// </remarks>
        public object? Value { get; }

        /// <summary>
        /// Gets or sets the operator used to compare <see cref="PropertyName"/> against <see cref="Value"/>.
        /// </summary>
        public ComparisonOperator ComparisonOperator { get; set; }

        /// <summary>
        /// Gets or sets whether the resulting condition is negated.
        /// </summary>
        public bool Not { get; set; }

        /// <summary>
        /// Gets or sets whether the comparison ignores case.
        /// </summary>
        /// <remarks>
        /// This only has an effect when <see cref="Value"/> is a <see cref="string"/>.
        /// </remarks>
        public bool IgnoreCase { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterDescriptor{TEntity}"/> class using a strongly-typed
        /// property selector.
        /// </summary>
        /// <param name="selector">An expression selecting the property to filter on, for example <c>x =&gt; x.Name</c>.</param>
        /// <param name="value">The value to compare the property against.</param>
        /// <param name="comparisonOperator">The comparison to apply. Defaults to <see cref="ComparisonOperator.Equal"/>.</param>
        /// <param name="not">Whether to negate the resulting condition.</param>
        /// <param name="ignoreCase">Whether to ignore case when <paramref name="value"/> is a <see cref="string"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="selector"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="selector"/> does not select a simple property, or the type of <paramref name="value"/> is not
        /// compatible with the selected property's type. A non-string collection is checked element by element, unless its
        /// own type is compatible with the property's (a <c>byte[]</c> for a <c>byte[]</c> property, for instance).
        /// </exception>
        public FilterDescriptor(Expression<Func<TEntity, object?>> selector, object? value, ComparisonOperator comparisonOperator = ComparisonOperator.Equal, bool not = false, bool ignoreCase = false)
        {
            PropertyInfo property = PropertyHelper.GetProperty(selector);
            PropertyHelper.EnsureValue<TEntity>(property, value);

            PropertyName = property.Name;
            Value = value;
            ComparisonOperator = comparisonOperator;
            Not = not;
            IgnoreCase = ignoreCase;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterDescriptor{TEntity}"/> class using a property name.
        /// </summary>
        /// <param name="propertyName">The name of the property to filter on.</param>
        /// <param name="value">The value to compare the property against.</param>
        /// <param name="comparisonOperator">The comparison to apply. Defaults to <see cref="ComparisonOperator.Equal"/>.</param>
        /// <param name="not">Whether to negate the resulting condition.</param>
        /// <param name="ignoreCase">Whether to ignore case when <paramref name="value"/> is a <see cref="string"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="propertyName"/> does not match a property on <typeparamref name="TEntity"/>, or the type of
        /// <paramref name="value"/> is not compatible with the property's type. A non-string collection is checked element
        /// by element, unless its own type is compatible with the property's (a <c>byte[]</c> for a <c>byte[]</c> property,
        /// for instance).
        /// </exception>
        public FilterDescriptor(string propertyName, object? value, ComparisonOperator comparisonOperator = ComparisonOperator.Equal, bool not = false, bool ignoreCase = false)
        {
            PropertyInfo property = PropertyHelper.GetProperty<TEntity>(propertyName);
            PropertyHelper.EnsureValue<TEntity>(property, value);

            PropertyName = property.Name;
            Value = value;
            ComparisonOperator = comparisonOperator;
            Not = not;
            IgnoreCase = ignoreCase;
        }
    }

    /// <summary>
    /// Specifies the comparison applied by a <see cref="FilterDescriptor{TEntity}"/>.
    /// </summary>
    public enum ComparisonOperator
    {
        /// <summary>The property equals the value.</summary>
        Equal,

        /// <summary>The property does not equal the value.</summary>
        NotEqual,

        /// <summary>The property is greater than the value.</summary>
        GreaterThan,

        /// <summary>The property is greater than or equal to the value.</summary>
        GreaterThanOrEqual,

        /// <summary>The property is less than the value.</summary>
        LessThan,

        /// <summary>The property is less than or equal to the value.</summary>
        LessThanOrEqual,

        /// <summary>The property, treated as a string, contains the value.</summary>
        Contains,

        /// <summary>The property, treated as a string, starts with the value.</summary>
        StartsWith,

        /// <summary>The property, treated as a string, ends with the value.</summary>
        EndsWith,

        /// <summary>The property matches one of the values in the collection.</summary>
        In
    }
}
