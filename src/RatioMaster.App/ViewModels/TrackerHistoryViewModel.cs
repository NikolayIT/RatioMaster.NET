using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using RatioMaster.Core.Tracker;

namespace RatioMaster.App.ViewModels;

/// <summary>The Tracker tab: the recent announce and scrape round-trips with their request and response.</summary>
public sealed partial class TrackerHistoryViewModel : ViewModelBase
{
    private const int MaxExchanges = 500;

    private readonly ConcurrentQueue<TrackerExchange> _pending = new();

    [ObservableProperty]
    private TrackerExchange? _selected;

    /// <summary>Newest first.</summary>
    public ObservableCollection<TrackerExchange> Exchanges { get; } = [];

    /// <summary>Called from the engine's thread.</summary>
    public void Enqueue(TrackerExchange exchange) => _pending.Enqueue(exchange);

    /// <summary>Called on the UI thread by the refresh timer.</summary>
    public void Drain()
    {
        while (_pending.TryDequeue(out var exchange))
        {
            Exchanges.Insert(0, exchange);
            Selected ??= exchange;
        }

        while (Exchanges.Count > MaxExchanges)
        {
            Exchanges.RemoveAt(Exchanges.Count - 1);
        }
    }
}
