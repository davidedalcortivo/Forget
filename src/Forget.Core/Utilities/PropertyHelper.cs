using Forget.Core.Caching;
using System.Collections;
using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Reflection;


namespace Forget.Core.Utilities
{
    internal static class PropertyHelper
    {
        private static readonly HashSet<Type> _integralTypes = [typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong)];
        private static readonly HashSet<Type> _bindableIntegralTypes = [typeof(byte), typeof(short), typeof(int), typeof(long)];
        private static readonly Dictionary<Type, Type[]> _widenings = new()
        {
            [typeof(sbyte)] = [typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
            [typeof(byte)] = [typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
            [typeof(short)] = [typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal)],
            [typeof(ushort)] = [typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal)],
            [typeof(int)] = [typeof(long), typeof(double), typeof(decimal)],
            [typeof(uint)] = [typeof(long), typeof(ulong), typeof(double), typeof(decimal)],
            [typeof(long)] = [typeof(decimal)],
            [typeof(ulong)] = [typeof(decimal)]
        };

        public static PropertyInfo GetProperty<TEntity, TSelector>(Expression<Func<TEntity, TSelector>> selector) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(selector);
            Expression body = selector.Body;

            if (body is UnaryExpression unaryExpression && body.NodeType == ExpressionType.Convert)
                body = unaryExpression.Operand;

            if (body is not MemberExpression memberExpression)
                throw new ArgumentException("The provided selector is not valid. Expected a simple member access expression.");

            return GetProperty<TEntity>(memberExpression.Member.Name);
        }

        public static PropertyInfo GetProperty<TEntity>(string propertyName) where TEntity : class
        {
            ArgumentNullException.ThrowIfNull(propertyName);
            ImmutableDictionary<string, PropertyInfo> propertiesByPropertyName = EntityInfoCache<TEntity>.PropertiesByPropertyName;

            if (!propertiesByPropertyName.TryGetValue(propertyName, out PropertyInfo? property))
                throw new ArgumentException($"The property '{propertyName}' does not exist on entity '{typeof(TEntity).Name}'.");

            return property;
        }

        public static void EnsureValue<TEntity>(PropertyInfo property, object? value)
        {
            if (value is not null)
            {
                Type valueType = value.GetType();

                if (value is IEnumerable enumerable && value is not string && !IsCompatible(property.PropertyType, valueType))
                {
                    foreach (object? _value in enumerable)
                    {
                        if (_value is byte)
                            throw new ArgumentException($"A list of bytes cannot be used as the value of the property '{property.Name}' for the entity '{typeof(TEntity).Name}': the drivers bind a byte array as one binary value.");

                        if (_value is not null)
                            EnsureValueType<TEntity>(property, _value.GetType());
                    }
                }
                else
                {
                    EnsureValueType<TEntity>(property, valueType);
                }
            }
        }

        public static void EnsureValueType<TEntity>(PropertyInfo property, Type valueType)
        {
            if (!IsCompatible(property.PropertyType, valueType))
                throw new ArgumentException($"The type of the provided value '{valueType}' does not match the type of the property '{property.Name}' ('{property.PropertyType}') for the entity '{typeof(TEntity).Name}'.");
        }

        public static void EnsureStorableType<TEntity>(PropertyInfo property, Type valueType)
        {
            if (!CanStore(property.PropertyType, valueType))
                throw new ArgumentException($"The type of the provided value '{valueType}' does not fit the type of the property '{property.Name}' ('{property.PropertyType}') for the entity '{typeof(TEntity).Name}'.");
        }

        public static void EnsureResultType<TEntity>(PropertyInfo property, Type resultType)
        {
            if (!CanHold(resultType, property.PropertyType))
                throw new ArgumentException($"The type '{resultType}' cannot hold the values of the property '{property.Name}' ('{property.PropertyType}') for the entity '{typeof(TEntity).Name}'.");
        }

        public static bool IsCompatible(Type propertyType, Type valueType)
        {
            propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;

            if (propertyType.IsAssignableFrom(valueType))
                return true;

            if (propertyType.IsEnum && valueType.IsEnum)
                return false;

            if (propertyType.IsEnum)
                propertyType = Enum.GetUnderlyingType(propertyType);

            if (valueType.IsEnum)
                valueType = Enum.GetUnderlyingType(valueType);

            if (propertyType == valueType)
                return true;

            return IsBindable(valueType)
                && ((_integralTypes.Contains(propertyType) && _integralTypes.Contains(valueType)) || IsWidening(valueType, propertyType));
        }

        public static bool CanStore(Type propertyType, Type valueType)
        {
            propertyType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            valueType = Nullable.GetUnderlyingType(valueType) ?? valueType;

            return CanHold(propertyType, valueType) && (propertyType == valueType || IsBindable(valueType));
        }

        public static bool CanHold(Type holder, Type held)
        {
            holder = Nullable.GetUnderlyingType(holder) ?? holder;
            held = Nullable.GetUnderlyingType(held) ?? held;

            if (holder.IsAssignableFrom(held))
                return true;

            if (holder.IsEnum && held.IsEnum)
                return false;

            if (holder.IsEnum)
                holder = Enum.GetUnderlyingType(holder);

            if (held.IsEnum)
                held = Enum.GetUnderlyingType(held);

            return holder == held || IsWidening(held, holder);
        }

        private static bool IsBindable(Type type)
        {
            if (type.IsEnum)
                type = Enum.GetUnderlyingType(type);

            return !_integralTypes.Contains(type) || _bindableIntegralTypes.Contains(type);
        }

        private static bool IsWidening(Type from, Type to)
        {
            return _widenings.TryGetValue(from, out Type[]? targets) && Array.IndexOf(targets, to) >= 0;
        }

        public static Func<T, object?> BuildGetterExpression<T>(PropertyInfo property) where T : class
        {
            Type type = typeof(T);
            ParameterExpression instanceParam = Expression.Parameter(type, "instance");
            Expression instanceCast = instanceParam;

            if (property.DeclaringType is not null && property.DeclaringType != type)
                instanceCast = Expression.Convert(instanceParam, property.DeclaringType);

            Expression propertyAccess = Expression.Property(instanceCast, property);
            UnaryExpression convertResult = Expression.Convert(propertyAccess, typeof(object));

            return Expression.Lambda<Func<T, object?>>(convertResult, instanceParam).Compile();
        }
    }
}
