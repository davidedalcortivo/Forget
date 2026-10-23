using Forget.Core.Models;
using Forget.Core.Utilities;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;


namespace Forget.Core.Caching
{
    internal static class EntityInfoCache<TEntity> where TEntity : class
    {
        public static string TableName { get; }
        public static string? SchemaName { get; }
        public static PropertyInfo IdProperty { get; }
        public static ImmutableArray<PropertyInfo> Properties { get; }
        public static ImmutableArray<PropertyInfo> UpdateProperties { get; }
        public static ImmutableArray<PropertyInfo> InsertProperties { get; }
        public static ImmutableArray<PropertyInfo> UpsertProperties { get; }
        public static ImmutableArray<PropertyInfo> UpsertKeyProperties { get; }
        public static ImmutableDictionary<string, string> ColumnNamesByPropertyName { get; }
        public static ImmutableDictionary<string, PropertyInfo> PropertiesByPropertyName { get; }
        public static ImmutableDictionary<string, PropertyInfo> PropertiesByColumnName { get; }
        public static ImmutableDictionary<string, Func<TEntity, object?>> PropertyGettersByPropertyName { get; }
        public static ImmutableDictionary<string, PropertyInfo> UpdatePropertiesByPropertyName { get; }

        static EntityInfoCache()
        {
            Type entityType = typeof(TEntity);
            PropertyInfo? idNamedProperty = null;

            HashSet<string> seenPropertyNames = new(StringComparer.OrdinalIgnoreCase);
            Stack<PropertyInfo> stack = new();
            int keyAttributeCount = 0;

            for (Type? type = entityType; type is not null; type = type.BaseType)
            {
                foreach (PropertyInfo property in type
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
                            throw new InvalidOperationException($"Multiple properties in the entity '{entityType.Name}' are marked with the [Key] attribute. Only one property can be marked as the key.");

                        IdProperty = property;
                    }

                    if (property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                        idNamedProperty = property;

                    stack.Push(property);
                }
            }

            if (IdProperty is null)
            {
                if (idNamedProperty is null)
                    throw new InvalidOperationException($"No property in the entity '{entityType.Name}' is marked with the [Key] attribute or named 'Id'. One property must be marked as the key or named 'Id' to be used as the identifier for the entity.");

                IdProperty = idNamedProperty;
            }

            TableAttribute? tableAttribute = entityType.GetCustomAttribute<TableAttribute>();
            TableName = tableAttribute?.Name ?? entityType.Name;
            SchemaName = tableAttribute?.Schema;

            IdentifierHelper.EnsureValid("table name", TableName);

            if (SchemaName is not null)
                IdentifierHelper.EnsureValid("schema name", SchemaName);

            Properties = [.. stack];
            UpdateProperties = [.. Properties.Where(x => !(x == IdProperty || x.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption > DatabaseGeneratedOption.None))];
            InsertProperties = [.. Properties.Where(x => !(x.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption > DatabaseGeneratedOption.None))];
            UpsertProperties = [.. Properties.Where(x => !(x == IdProperty || x.GetCustomAttribute<DatabaseGeneratedAttribute>()?.DatabaseGeneratedOption > DatabaseGeneratedOption.None) && !x.IsDefined(typeof(UpsertKeyAttribute), true))];
            UpsertKeyProperties = [.. Properties.Where(x => x.IsDefined(typeof(UpsertKeyAttribute), true))];

            if (UpsertKeyProperties.Length == 0)
                UpsertKeyProperties = [IdProperty];

            ImmutableDictionary<string, string>.Builder columnNamesByPropertyNameBuilder = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
            ImmutableDictionary<string, PropertyInfo>.Builder propertiesByPropertyNameBuilder = ImmutableDictionary.CreateBuilder<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            ImmutableDictionary<string, PropertyInfo>.Builder propertiesByColumnNameBuilder = ImmutableDictionary.CreateBuilder<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
            ImmutableDictionary<string, Func<TEntity, object?>>.Builder propertyGettersByPropertyNameBuilder = ImmutableDictionary.CreateBuilder<string, Func<TEntity, object?>>(StringComparer.OrdinalIgnoreCase);

            foreach (PropertyInfo property in Properties)
            {
                string columnName = property.GetCustomAttribute<ColumnAttribute>()?.Name ?? property.Name;

                IdentifierHelper.EnsureValid("column name", columnName);

                columnNamesByPropertyNameBuilder[property.Name] = columnName;
                propertiesByPropertyNameBuilder[property.Name] = property;
                propertiesByColumnNameBuilder[columnName] = property;

                MethodInfo? getMethod = property.GetMethod;

                if (getMethod is not null)
                    propertyGettersByPropertyNameBuilder[property.Name] = PropertyHelper.BuildGetterExpression<TEntity>(property);
            }

            ColumnNamesByPropertyName = columnNamesByPropertyNameBuilder.ToImmutable();
            PropertiesByPropertyName = propertiesByPropertyNameBuilder.ToImmutable();
            PropertiesByColumnName = propertiesByColumnNameBuilder.ToImmutable();
            PropertyGettersByPropertyName = propertyGettersByPropertyNameBuilder.ToImmutable();
            UpdatePropertiesByPropertyName = UpdateProperties.ToImmutableDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        }
    }
}
