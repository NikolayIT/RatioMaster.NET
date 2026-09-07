namespace RatioMaster.Core.Tests.Fakes
{
    using RatioMaster.Core.Abstractions;

    /// <summary>A deterministic <see cref="IRandomSource"/> wrapping <see cref="Random"/> so runs are reproducible.</summary>
    internal sealed class DeterministicRandomSource(int seed) : IRandomSource
    {
        private readonly Random random = new(seed);

        public int Next(int maxExclusive) => this.random.Next(maxExclusive);

        public int Next(int minInclusive, int maxExclusive) => this.random.Next(minInclusive, maxExclusive);

        public double NextDouble() => this.random.NextDouble();
    }
}
