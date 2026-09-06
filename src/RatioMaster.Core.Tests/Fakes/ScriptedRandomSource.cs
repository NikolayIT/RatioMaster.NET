using RatioMaster.Core.Abstractions;

namespace RatioMaster.Core.Tests.Fakes;

/// <summary>An <see cref="IRandomSource"/> that returns pre-scripted values, for exact-output tests.</summary>
internal sealed class ScriptedRandomSource : IRandomSource
{
    private readonly Queue<int> _ints;
    private readonly Queue<double> _doubles;

    public ScriptedRandomSource(IEnumerable<int>? ints = null, IEnumerable<double>? doubles = null)
    {
        _ints = new Queue<int>(ints ?? []);
        _doubles = new Queue<double>(doubles ?? []);
    }

    public int Next(int maxExclusive) => _ints.Count > 0 ? _ints.Dequeue() : 0;

    public int Next(int minInclusive, int maxExclusive) => _ints.Count > 0 ? _ints.Dequeue() : minInclusive;

    public double NextDouble() => _doubles.Count > 0 ? _doubles.Dequeue() : 0.0;
}

/// <summary>A deterministic <see cref="IRandomSource"/> wrapping <see cref="Random"/> so runs are reproducible.</summary>
internal sealed class DeterministicRandomSource(int seed) : IRandomSource
{
    private readonly Random _random = new(seed);

    public int Next(int maxExclusive) => _random.Next(maxExclusive);

    public int Next(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);

    public double NextDouble() => _random.NextDouble();
}
