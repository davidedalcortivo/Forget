namespace Forget.Core.Utilities
{
    internal sealed class IdEqualityComparer : IEqualityComparer<object>
    {
        public static IdEqualityComparer Instance { get; } = new();

        private IdEqualityComparer() { }

        public new bool Equals(object? x, object? y)
        {
            if (x is byte[] left && y is byte[] right)
                return left.AsSpan().SequenceEqual(right);

            return object.Equals(x, y);
        }

        public int GetHashCode(object obj)
        {
            if (obj is not byte[] bytes)
                return obj.GetHashCode();

            HashCode hash = new();
            hash.AddBytes(bytes);

            return hash.ToHashCode();
        }
    }
}
