using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RatioMaster.App.ViewModels;

namespace RatioMaster.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);

        TorrentGrid.SelectionChanged += OnGridSelectionChanged;
        PropertyChanged += OnWindowPropertyChanged;
    }

    private MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

    /// <summary>Brings the window back from the tray or the taskbar.</summary>
    public void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    private void OnGridSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.SetSelection(TorrentGrid.SelectedItems?.OfType<TorrentItemViewModel>() ?? []);
        }
    }

    private void OnWindowPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != WindowStateProperty || ViewModel is not { } viewModel)
        {
            return;
        }

        if (WindowState == WindowState.Minimized && viewModel.Settings.MinimizeToTray)
        {
            Hide();
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Close to tray keeps the torrents announcing; quitting happens through File > Exit or the tray menu.
        if (!e.IsProgrammatic && ViewModel is { } viewModel && viewModel.Settings.CloseToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

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
