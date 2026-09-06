using System;
using System.Linq;
using Avalonia.Controls;
using RatioMaster.App.ViewModels;

namespace RatioMaster.App.Views.Controls;

public partial class LogView : UserControl
{
    private LogViewModel? _viewModel;

    public LogView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        DetachedFromVisualTree += (_, _) => Unsubscribe();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Unsubscribe();
        if (DataContext is LogViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.LinesAppended += OnLinesAppended;
        }
    }

    private void Unsubscribe()
    {
        if (_viewModel is not null)
        {
            _viewModel.LinesAppended -= OnLinesAppended;
            _viewModel = null;
        }
    }

    private void OnLinesAppended(object? sender, EventArgs e)
    {
        // Keep the newest line in view while auto-scroll is on.
        if (_viewModel?.Entries.LastOrDefault() is { } last)
        {
            Lines.ScrollIntoView(last);
        }
    }
}
