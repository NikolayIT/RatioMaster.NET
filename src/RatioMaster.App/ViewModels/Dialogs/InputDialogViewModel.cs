using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RatioMaster.App.ViewModels.Dialogs;

/// <summary>A single-line text prompt (rename, set speed for all).</summary>
public sealed partial class InputDialogViewModel : DialogViewModel<string>
{
    [ObservableProperty]
    private string value;

    public InputDialogViewModel(string title, string label, string initialValue)
    {
        this.Title = title;
        this.Label = label;
        this.value = initialValue;
    }

    public string Label { get; }

    [RelayCommand]
    private void Accept() => this.Close(this.Value);

    [RelayCommand]
    private void Cancel() => this.Close(null);
}
