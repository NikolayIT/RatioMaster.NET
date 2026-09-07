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

    private MainWindowViewModel? viewModel;
    private double detailsPaneHeight = 300;
    private PixelPoint? normalPosition;
    private Size? normalSize;

    public MainWindow()
    {
        this.InitializeComponent();

        DragDrop.SetAllowDrop(this, true);
        this.AddHandler(DragDrop.DragOverEvent, OnDragOver);
        this.AddHandler(DragDrop.DropEvent, this.OnDrop);

        this.TorrentGrid.SelectionChanged += this.OnGridSelectionChanged;
        this.DetailsSplitter.DragCompleted += this.OnDetailsSplitterDragCompleted;
        this.ContentGrid.SizeChanged += (_, _) => this.ApplyDetailsPaneHeight();
        this.SearchBox.KeyDown += this.OnSearchBoxKeyDown;
        this.DataContextChanged += this.OnDataContextChanged;
        this.PropertyChanged += this.OnWindowPropertyChanged;
        this.PositionChanged += this.OnPositionChanged;

        this.BuildColumnsMenu();
    }

    private MainWindowViewModel? ViewModel => this.viewModel;

    private RowDefinition DetailsRow => this.ContentGrid.RowDefinitions[2];

    private DataGridColumn NameColumn => this.TorrentGrid.Columns[0];

    /// <summary>Brings the window back from the tray or the taskbar.</summary>
    public void RestoreFromTray()
    {
        this.Show();
        this.WindowState = WindowState.Normal;
        this.Activate();
        this.Focus();
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        this.SavePlacement();

        // Close to tray keeps the torrents announcing; quitting happens through File > Exit or the tray menu.
        if (!e.IsProgrammatic && this.ViewModel is { } viewModel && viewModel.Settings.CloseToTray)
        {
            e.Cancel = true;
            this.Hide();
            return;
        }

        base.OnClosing(e);
    }

    // ---- Keyboard -----------------------------------------------------------

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control)
        {
            this.SearchBox.Focus();
            this.SearchBox.SelectAll();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    // ---- Columns ------------------------------------------------------------

    private static string ColumnName(DataGridColumn column) => column.Header?.ToString() ?? string.Empty;

    // ---- Drag and drop ------------------------------------------------------

    private static void OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    // ---- View model wiring ----------------------------------------------

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (this.viewModel is not null)
        {
            this.viewModel.PropertyChanged -= this.OnViewModelPropertyChanged;
        }

        this.viewModel = this.DataContext as MainWindowViewModel;
        if (this.viewModel is null)
        {
            return;
        }

        this.viewModel.PropertyChanged += this.OnViewModelPropertyChanged;

        var settings = this.viewModel.Settings;
        this.RestorePlacement(settings.Window);
        this.detailsPaneHeight = Math.Max(DetailsPaneMinHeight, settings.DetailsPaneHeight);
        this.ApplyDetailsPaneHeight();
        this.ApplyColumnLayout(settings.Columns);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.IsDetailsPaneVisible))
        {
            this.ApplyDetailsPaneHeight();
        }
    }

    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (this.ViewModel is { } viewModel)
        {
            viewModel.SetSelection(this.TorrentGrid.SelectedItems?.OfType<TorrentItemViewModel>() ?? []);
        }
    }

    // ---- Details pane -----------------------------------------------------

    /// <summary>
    /// Sizes the details row: the remembered height when the pane is shown (clamped so the list keeps
    /// its minimum), zero when it is hidden. The row is pixel-sized, so the splitter and the content agree.
    /// </summary>
    private void ApplyDetailsPaneHeight()
    {
        var row = this.DetailsRow;
        if (this.ViewModel is { IsDetailsPaneVisible: false })
        {
            row.MinHeight = 0;
            row.Height = new GridLength(0, GridUnitType.Pixel);
            return;
        }

        var height = this.detailsPaneHeight;
        var available = this.ContentGrid.Bounds.Height - this.DetailsSplitter.Bounds.Height - ListMinHeight;
        if (available >= DetailsPaneMinHeight)
        {
            height = Math.Min(height, available);
        }

        row.MinHeight = DetailsPaneMinHeight;
        row.Height = new GridLength(Math.Max(height, DetailsPaneMinHeight), GridUnitType.Pixel);
    }

    private void OnDetailsSplitterDragCompleted(object? sender, VectorEventArgs e)
    {
        this.detailsPaneHeight = this.DetailsRow.ActualHeight;
        if (this.ViewModel is { } viewModel)
        {
            viewModel.ApplySettings(viewModel.Settings with { DetailsPaneHeight = this.detailsPaneHeight });
        }
    }

    // ---- Window placement -------------------------------------------------

    private void RestorePlacement(WindowPlacement placement)
    {
        if (placement.Width >= this.MinWidth && placement.Height >= this.MinHeight)
        {
            this.Width = placement.Width;
            this.Height = placement.Height;
        }

        if (placement is { X: { } x, Y: { } y } && this.IsOnScreen(new PixelPoint((int)x, (int)y)))
        {
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Position = new PixelPoint((int)x, (int)y);
        }
        else
        {
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        if (placement.IsMaximized)
        {
            this.WindowState = WindowState.Maximized;
        }
    }

    private bool IsOnScreen(PixelPoint point)
    {
        try
        {
            // The title bar must land on a monitor, otherwise the window would come back off-screen.
            var probe = new PixelPoint(point.X + 80, point.Y + 30);
            return this.Screens.All.Any(screen => screen.WorkingArea.Contains(probe));
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        if (this.WindowState == WindowState.Normal && this.IsVisible)
        {
            this.normalPosition = e.Point;
        }
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ClientSizeProperty && this.WindowState == WindowState.Normal)
        {
            this.normalSize = this.ClientSize;
            return;
        }

        if (e.Property != WindowStateProperty || this.ViewModel is not { } viewModel)
        {
            return;
        }

        if (this.WindowState == WindowState.Minimized && viewModel.Settings.MinimizeToTray)
        {
            this.Hide();
        }
    }

    private void SavePlacement()
    {
        if (this.ViewModel is not { } viewModel)
        {
            return;
        }

        var size = this.normalSize ?? this.ClientSize;
        var position = this.normalPosition ?? this.Position;
        var placement = new WindowPlacement
        {
            X = position.X,
            Y = position.Y,
            Width = size.Width,
            Height = size.Height,
            IsMaximized = this.WindowState == WindowState.Maximized,
        };

        viewModel.ApplySettings(viewModel.Settings with
        {
            Window = placement,
            DetailsPaneHeight = this.detailsPaneHeight,
            Columns = this.CaptureColumnLayout(),
        });
    }

    /// <summary>Fills View > Columns with a check item per column; the Name column cannot be hidden.</summary>
    private void BuildColumnsMenu()
    {
        foreach (var column in this.TorrentGrid.Columns)
        {
            var item = new MenuItem
            {
                Header = ColumnName(column),
                ToggleType = MenuItemToggleType.CheckBox,
                IsEnabled = column != this.NameColumn,
                StaysOpenOnClick = true,
            };
            item[!MenuItem.IsCheckedProperty] = column[!!DataGridColumn.IsVisibleProperty];
            this.ColumnsMenu.Items.Add(item);
        }
    }

    private void ApplyColumnLayout(IReadOnlyList<ColumnLayout> layouts)
    {
        if (layouts.Count == 0)
        {
            return;
        }

        var columns = this.TorrentGrid.Columns;
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

            column.IsVisible = layout.IsVisible || column == this.NameColumn;
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
        this.TorrentGrid.Columns.Select(column => new ColumnLayout
        {
            Name = ColumnName(column),
            IsVisible = column.IsVisible,
            Width = column.Width.IsStar ? 0 : column.Width.IsAbsolute ? column.Width.Value : column.ActualWidth,
            DisplayIndex = column.DisplayIndex,
        }).ToList();

    private void OnSearchBoxKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            this.SearchBox.Clear();
            this.TorrentGrid.Focus();
            e.Handled = true;
        }
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        e.Handled = true;
        if (this.ViewModel is not { } viewModel)
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
