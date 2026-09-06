using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using RatioMaster.App.ViewModels;
using RatioMaster.Core.Settings;

namespace RatioMaster.App.Views;

public partial class MainWindow : Window
{
    /// <summary>The details pane can never be dragged below this, so its tab strip stays usable.</summary>
    private const double DetailsPaneMinHeight = 140;

    /// <summary>The torrent list keeps at least this much room whatever the pane height.</summary>
    private const double ListMinHeight = 120;

    private MainWindowViewModel? _viewModel;
    private double _detailsPaneHeight = 300;
    private PixelPoint? _normalPosition;
    private Size? _normalSize;

    public MainWindow()
    {
        InitializeComponent();

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        TorrentGrid.SelectionChanged += OnGridSelectionChanged;
        DetailsSplitter.DragCompleted += OnDetailsSplitterDragCompleted;
        ContentGrid.SizeChanged += (_, _) => ApplyDetailsPaneHeight();
        SearchBox.KeyDown += OnSearchBoxKeyDown;
        DataContextChanged += OnDataContextChanged;
        PropertyChanged += OnWindowPropertyChanged;
        PositionChanged += OnPositionChanged;

        BuildColumnsMenu();
    }

    private MainWindowViewModel? ViewModel => _viewModel;

    private RowDefinition DetailsRow => ContentGrid.RowDefinitions[2];

    private DataGridColumn NameColumn => TorrentGrid.Columns[0];

    /// <summary>Brings the window back from the tray or the taskbar.</summary>
    public void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    // ---- View model wiring ----------------------------------------------

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as MainWindowViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        var settings = _viewModel.Settings;
        RestorePlacement(settings.Window);
        _detailsPaneHeight = Math.Max(DetailsPaneMinHeight, settings.DetailsPaneHeight);
        ApplyDetailsPaneHeight();
        ApplyColumnLayout(settings.Columns);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsDetailsPaneVisible))
        {
            ApplyDetailsPaneHeight();
        }
    }

    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.SetSelection(TorrentGrid.SelectedItems?.OfType<TorrentItemViewModel>() ?? []);
        }
    }

    // ---- Details pane -----------------------------------------------------

    /// <summary>
    /// Sizes the details row: the remembered height when the pane is shown (clamped so the list keeps
    /// its minimum), zero when it is hidden. The row is pixel-sized, so the splitter and the content agree.
    /// </summary>
    private void ApplyDetailsPaneHeight()
    {
        var row = DetailsRow;
        if (ViewModel is { IsDetailsPaneVisible: false })
        {
            row.MinHeight = 0;
            row.Height = new GridLength(0, GridUnitType.Pixel);
            return;
        }

        var height = _detailsPaneHeight;
        var available = ContentGrid.Bounds.Height - DetailsSplitter.Bounds.Height - ListMinHeight;
        if (available >= DetailsPaneMinHeight)
        {
            height = Math.Min(height, available);
        }

        row.MinHeight = DetailsPaneMinHeight;
        row.Height = new GridLength(Math.Max(height, DetailsPaneMinHeight), GridUnitType.Pixel);
    }

    private void OnDetailsSplitterDragCompleted(object? sender, VectorEventArgs e)
    {
        _detailsPaneHeight = DetailsRow.ActualHeight;
        if (ViewModel is { } viewModel)
        {
            viewModel.ApplySettings(viewModel.Settings with { DetailsPaneHeight = _detailsPaneHeight });
        }
    }

    // ---- Window placement -------------------------------------------------

    private void RestorePlacement(WindowPlacement placement)
    {
        if (placement.Width >= MinWidth && placement.Height >= MinHeight)
        {
            Width = placement.Width;
            Height = placement.Height;
        }

        if (placement is { X: { } x, Y: { } y } && IsOnScreen(new PixelPoint((int)x, (int)y)))
        {
            WindowStartupLocation = WindowStartupLocation.Manual;
            Position = new PixelPoint((int)x, (int)y);
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (placement.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private bool IsOnScreen(PixelPoint point)
    {
        try
        {
            // The title bar must land on a monitor, otherwise the window would come back off-screen.
            var probe = new PixelPoint(point.X + 80, point.Y + 30);
            return Screens.All.Any(screen => screen.WorkingArea.Contains(probe));
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (WindowState == WindowState.Normal && IsVisible)
        {
            _normalPosition = e.Point;
        }
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ClientSizeProperty && WindowState == WindowState.Normal)
        {
            _normalSize = ClientSize;
            return;
        }

        if (e.Property != WindowStateProperty || ViewModel is not { } viewModel)
        {
            return;
        }

        if (WindowState == WindowState.Minimized && viewModel.Settings.MinimizeToTray)
        {
            Hide();
        }
    }

    private void SavePlacement()
    {
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        var size = _normalSize ?? ClientSize;
        var position = _normalPosition ?? Position;
        var placement = new WindowPlacement
        {
            X = position.X,
            Y = position.Y,
            Width = size.Width,
            Height = size.Height,
            IsMaximized = WindowState == WindowState.Maximized,
        };

        viewModel.ApplySettings(viewModel.Settings with
        {
            Window = placement,
            DetailsPaneHeight = _detailsPaneHeight,
            Columns = CaptureColumnLayout(),
        });
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        SavePlacement();

        // Close to tray keeps the torrents announcing; quitting happens through File > Exit or the tray menu.
        if (!e.IsProgrammatic && ViewModel is { } viewModel && viewModel.Settings.CloseToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    // ---- Columns ------------------------------------------------------------

    private static string ColumnName(DataGridColumn column) => column.Header?.ToString() ?? string.Empty;

    /// <summary>Fills View > Columns with a check item per column; the Name column cannot be hidden.</summary>
    private void BuildColumnsMenu()
    {
        foreach (var column in TorrentGrid.Columns)
        {
            var item = new MenuItem
            {
                Header = ColumnName(column),
                ToggleType = MenuItemToggleType.CheckBox,
                IsEnabled = column != NameColumn,
                StaysOpenOnClick = true,
            };
            item[!MenuItem.IsCheckedProperty] = column[!!DataGridColumn.IsVisibleProperty];
            ColumnsMenu.Items.Add(item);
        }
    }

    private void ApplyColumnLayout(IReadOnlyList<ColumnLayout> layouts)
    {
        if (layouts.Count == 0)
        {
            return;
        }

        var columns = TorrentGrid.Columns;
        var byName = new Dictionary<string, ColumnLayout>(StringComparer.Ordinal);
        foreach (var layout in layouts)
        {
            byName.TryAdd(layout.Name, layout);
        }

        foreach (var column in columns)
        {
            if (!byName.TryGetValue(ColumnName(column), out var layout))
            {
                continue;
            }

            column.IsVisible = layout.IsVisible || column == NameColumn;
            if (layout.Width >= 40 && !column.Width.IsStar)
            {
                column.Width = new DataGridLength(layout.Width);
            }
        }

        // Reorder only when the saved layout covers exactly the current columns.
        var names = columns.Select(ColumnName).ToHashSet(StringComparer.Ordinal);
        var ordered = layouts.Where(l => names.Contains(l.Name)).Select(l => l.Name).Distinct().OrderBy(n => byName[n].DisplayIndex).ToList();
        if (ordered.Count != columns.Count)
        {
            return;
        }

        for (var index = 0; index < ordered.Count; index++)
        {
            var column = columns.First(c => ColumnName(c) == ordered[index]);
            column.DisplayIndex = index;
        }
    }

    private List<ColumnLayout> CaptureColumnLayout() =>
        TorrentGrid.Columns.Select(column => new ColumnLayout
        {
            Name = ColumnName(column),
            IsVisible = column.IsVisible,
            Width = column.Width.IsStar ? 0 : column.Width.IsAbsolute ? column.Width.Value : column.ActualWidth,
            DisplayIndex = column.DisplayIndex,
        }).ToList();

    // ---- Keyboard -----------------------------------------------------------

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control)
        {
            SearchBox.Focus();
            SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SearchBox.Clear();
            TorrentGrid.Focus();
            e.Handled = true;
        }
    }

    // ---- Drag and drop ------------------------------------------------------

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        if (ViewModel is not { } viewModel)
        {
            return;
        }

        var files = e.DataTransfer.TryGetFiles();
        if (files is null)
        {
            return;
        }

        var paths = new List<string>();
        foreach (var file in files)
        {
            if (file.TryGetLocalPath() is { Length: > 0 } path)
            {
                paths.Add(path);
            }
        }

        if (paths.Count > 0)
        {
            await viewModel.HandleDroppedFilesAsync(paths);
        }
    }
}
