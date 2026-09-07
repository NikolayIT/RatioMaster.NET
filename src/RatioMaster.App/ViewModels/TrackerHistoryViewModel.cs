namespace RatioMaster.App.ViewModels
{
    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;

    using CommunityToolkit.Mvvm.ComponentModel;
    using RatioMaster.Core.Tracker;

    /// <summary>The Tracker tab: the recent announce and scrape round-trips with their request and response.</summary>
    public sealed partial class TrackerHistoryViewModel : ViewModelBase
    {
        private const int MaxExchanges = 500;

        private readonly ConcurrentQueue<TrackerExchange> pending = new();

        [ObservableProperty]
        private TrackerExchange? selected;

        /// <summary>Gets the newest first.</summary>
        public ObservableCollection<TrackerExchange> Exchanges { get; } = [];

        /// <summary>Called from the engine's thread.</summary>
        public void Enqueue(TrackerExchange exchange) => this.pending.Enqueue(exchange);

        /// <summary>Called on the UI thread by the refresh timer.</summary>
        public void Drain()
        {
            while (this.pending.TryDequeue(out var exchange))
            {
                this.Exchanges.Insert(0, exchange);
                this.Selected ??= exchange;
            }

            while (this.Exchanges.Count > MaxExchanges)
            {
                this.Exchanges.RemoveAt(this.Exchanges.Count - 1);
            }
        }
    }
}
