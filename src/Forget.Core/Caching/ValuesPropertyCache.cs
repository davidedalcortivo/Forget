using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;


namespace Forget.Core.Caching
{
    internal static class ValuesPropertyCache<TEntity> where TEntity : class
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _cache = new();

        public static PropertyInfo[] Get(object values)
        {
            ArgumentNullException.ThrowIfNull(values);

            return Get(values.GetType());
        }

        public static PropertyInfo[] Get(Type type)
        {
            return _cache.GetOrAdd(type, Create);
        }

        private static PropertyInfo[] Create(Type type)
        {
            ImmutableDictionary<string, PropertyInfo> updatePropertiesByPropertyName = EntityInfoCache<TEntity>.UpdatePropertiesByPropertyName;

            PropertyInfo? idProperty = null;
            PropertyInfo? idNamedProperty = null;

            HashSet<string> seenPropertyNames = new(StringComparer.OrdinalIgnoreCase);
            Stack<PropertyInfo> stack = new();
            int keyAttributeCount = 0;

            for (Type? _type = type; _type is not null; _type = _type.BaseType)
            {
                foreach (PropertyInfo property in _type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(x => !x.IsDefined(typeof(NotMappedAttribute), true))
                    .Reverse())
                {
                    if (SqlTranslationContext.ParameterRegex().IsMatch(property.Name))
                        throw new InvalidOperationException($"The property '{property.Name}' uses a reserved parameter name.");

                    if (!seenPropertyNames.Add(property.Name))
                        continue;

                    if (property.IsDefined(typeof(KeyAttribute), true))
                    {
                        if (++keyAttributeCount > 1)
                            throw new InvalidOperationException($"Multiple properties in the values type '{type.Name}' are marked with the [Key] attribute. Only one property can be marked as the key.");

                        idProperty = property;
                    }

                    if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                        idNamedProperty = property;

                    stack.Push(property);
                }
            }

            idProperty ??= idNamedProperty;
            PropertyInfo[] updateProperties = [.. stack.Where(x => !(x == idProperty || x.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption > DatabaseGeneratedOption.None))];

            if (updateProperties.Length == 0)
                throw new ArgumentException($"The provided values type '{type.Name}' does not contain any valid update properties.");

            foreach (PropertyInfo updateProperty in updateProperties)
            {
                if (!updatePropertiesByPropertyName.TryGetValue(updateProperty.Name, out PropertyInfo? property))
                    throw new ArgumentException($"The property '{updateProperty.Name}' is not a valid update property for entity '{typeof(TEntity).Name}'.");

                PropertyHelper.EnsureStorableType<TEntity>(property, updateProperty.PropertyType);
            }

            return updateProperties;
        }
    }
}
