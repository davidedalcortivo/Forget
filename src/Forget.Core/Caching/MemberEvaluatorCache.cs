using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;


namespace Forget.Core.Caching
{
    internal static class MemberEvaluatorCache
    {
        private static readonly ConcurrentDictionary<MemberInfo, Func<object?, object?>> _cache = new();

        public static bool TryEvaluate(MemberExpression expr, out object? value, out Exception? failure)
        {
            value = null;
            failure = null;

            if (expr.Expression is null)
            {
                switch (expr.Member)
                {
                    case FieldInfo field when field.IsStatic:
                        value = field.GetValue(null);
                        return true;

                    case PropertyInfo prop when prop.GetMethod?.IsStatic == true:
                        value = prop.GetValue(null);
                        return true;

                    default:
                        return false;
                }
            }

            MemberInfo member = expr.Member;

            Func<object?, object?> getter = _cache.GetOrAdd(member, m =>
            {
                try
                {
                    ParameterExpression param = Expression.Parameter(typeof(object), "x");
                    Expression target = Expression.Convert(
                        Expression.MakeMemberAccess(Expression.Convert(param, expr.Expression.Type), member),
                        typeof(object)
                    );

                    return Expression.Lambda<Func<object?, object?>>(target, param).Compile();
                }
                catch
                {
                    return _ => throw new NotSupportedException($"Unable to compile expression '{expr}'.");
                }
            });

            try
            {
                value = getter(EvaluateRoot(expr.Expression));
                return true;
            }
            catch (Exception ex)
            {
                failure = ex is TargetInvocationException { InnerException: not null } ? ex.InnerException : ex;
                return false;
            }
        }

        private static object? EvaluateRoot(Expression root)
        {
            if (root is ConstantExpression constant)
            {
                return constant.Value;
            }

            if (root is MemberExpression member)
            {
                if (TryEvaluate(member, out object? value, out Exception? failure))
                {
                    return value;
                }

                if (failure is not null)
                {
                    throw failure;
                }
            }

            return Expression.Lambda(root).Compile().DynamicInvoke();
        }
    }
}
