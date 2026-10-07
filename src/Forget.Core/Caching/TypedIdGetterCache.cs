using Forget.Core.Utilities;


namespace Forget.Core.Caching
{
    internal static class TypedIdGetterCache<TEntity, TKey> where TEntity : class where TKey : notnull
    {
        public static Func<TEntity, TKey> Getter { get; } = PropertyHelper.BuildTypedGetterExpression<TEntity, TKey>(EntityInfoCache<TEntity>.IdProperty);
    }
}
