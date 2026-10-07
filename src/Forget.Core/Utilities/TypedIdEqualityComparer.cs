namespace Forget.Core.Utilities
{
    internal static class TypedIdEqualityComparer<TKey>
    {
        public static IEqualityComparer<TKey> Instance { get; } = typeof(TKey) == typeof(byte[])
            ? (IEqualityComparer<TKey>)(object)ByteArrayEqualityComparer.Instance
            : EqualityComparer<TKey>.Default;
    }

    internal sealed class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
    {
        public static ByteArrayEqualityComparer Instance { get; } = new();

        private ByteArrayEqualityComparer() { }

        public bool Equals(byte[]? x, byte[]? y)
        {
            if (x is null || y is null)
                return x == y;

            return x.AsSpan().SequenceEqual(y);
        }

        public int GetHashCode(byte[] obj)
        {
            HashCode hash = new();
            hash.AddBytes(obj);

            return hash.ToHashCode();
        }
    }
}
