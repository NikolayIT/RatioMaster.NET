namespace RatioMaster.Core.Abstractions
{
    /// <summary>Source of random numbers, replaceable in tests.</summary>
    public interface IRandomSource
    {
        /// <summary>A non-negative integer below <paramref name="maxExclusive"/>.</summary>
        int Next(int maxExclusive);

        /// <summary>An integer in [<paramref name="minInclusive"/>, <paramref name="maxExclusive"/>).</summary>
        int Next(int minInclusive, int maxExclusive);

        /// <summary>A double in [0, 1).</summary>
        double NextDouble();
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        public static SystemRandomSource Instance { get; } = new();

        public int Next(int maxExclusive) => Random.Shared.Next(maxExclusive);

        public int Next(int minInclusive, int maxExclusive) => Random.Shared.Next(minInclusive, maxExclusive);

        public double NextDouble() => Random.Shared.NextDouble();
    }
}
