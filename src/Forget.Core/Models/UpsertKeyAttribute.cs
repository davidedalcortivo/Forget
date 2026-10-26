namespace Forget.Core.Models
{
    /// <summary>
    /// Marks a property as (part of) the key used to identify an existing row during an upsert operation.
    /// </summary>
    /// <remarks>
    /// Apply this attribute to one or more properties to define a natural key for upsert matching, in place of the
    /// entity's identifier property. When applied to more than one property on the same entity, they together form
    /// a composite key.
    /// <para>
    /// If no property on the entity is marked with this attribute, the entity's identifier property is used as the
    /// upsert key instead.
    /// </para>
    /// <para>
    /// The exact matching semantics (in particular, how <see langword="null"/> values are compared) depend on the
    /// database provider in use.
    /// </para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class UpsertKeyAttribute : Attribute
    {

    }
}
