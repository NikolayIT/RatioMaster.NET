namespace RatioMaster.App.Views.Controls
{
    using System;
    using System.Linq;

    using Avalonia.Controls;
    using RatioMaster.App.ViewModels;

    public partial class LogView : UserControl
    {
        private LogViewModel? viewModel;

        public LogView()
        {
            this.InitializeComponent();
            this.DataContextChanged += this.OnDataContextChanged;
            this.DetachedFromVisualTree += (_, _) => this.Unsubscribe();
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            this.Unsubscribe();
            if (this.DataContext is LogViewModel viewModel)
            {
                this.viewModel = viewModel;
                this.viewModel.LinesAppended += this.OnLinesAppended;
            }
        }

        private void Unsubscribe()
        {
            if (this.viewModel is not null)
            {
                this.viewModel.LinesAppended -= this.OnLinesAppended;
                this.viewModel = null;
            }
        }

        private void OnLinesAppended(object? sender, EventArgs e)
        {
            // Keep the newest line in view while auto-scroll is on.
            if (this.viewModel?.Entries.LastOrDefault() is { } last)
            {
                this.Lines.ScrollIntoView(last);
            }
        }
    }
}
