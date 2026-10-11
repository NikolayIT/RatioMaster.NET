namespace RatioMaster.App.Views.Controls
{
    using Avalonia.Controls;
    using Avalonia.Threading;

    public partial class TrackerHistoryView : UserControl
    {
        public TrackerHistoryView()
        {
            this.InitializeComponent();

            // The columns of this grid are not saved, so any of them may grow to fit a translated header.
            this.Loaded += (_, _) => Dispatcher.UIThread.Post(() => DataGridHeaders.Fit(this.ExchangesGrid, _ => true), DispatcherPriority.Loaded);
        }
    }
}
