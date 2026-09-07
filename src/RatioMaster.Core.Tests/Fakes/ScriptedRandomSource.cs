namespace RatioMaster.Core.Tests.Fakes
{
    using RatioMaster.Core.Abstractions;

    /// <summary>An <see cref="IRandomSource"/> that returns pre-scripted values, for exact-output tests.</summary>
    internal sealed class ScriptedRandomSource : IRandomSource
    {
        private readonly Queue<int> ints;
        private readonly Queue<double> doubles;

        public ScriptedRandomSource(IEnumerable<int>? ints = null, IEnumerable<double>? doubles = null)
        {
            this.ints = new Queue<int>(ints ?? []);
            this.doubles = new Queue<double>(doubles ?? []);
        }

        public int Next(int maxExclusive) => this.ints.Count > 0 ? this.ints.Dequeue() : 0;

        public int Next(int minInclusive, int maxExclusive) => this.ints.Count > 0 ? this.ints.Dequeue() : minInclusive;

        public double NextDouble() => this.doubles.Count > 0 ? this.doubles.Dequeue() : 0.0;
    }
}
