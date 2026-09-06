using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RatioMaster.App.ViewModels.Dialogs;

/// <summary>Base for dialogs: carries a title and completes with a result when closed.</summary>
public abstract class DialogViewModel<TResult> : ViewModelBase
{
    private readonly TaskCompletionSource<TResult?> _completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public string Title { get; init; } = "RatioMaster.NET";

    /// <summary>Completes when the dialog closes.</summary>
    public Task<TResult?> Result => _completion.Task;

    /// <summary>Raised when the view model wants its window closed.</summary>
    public event EventHandler? CloseRequested;

    /// <summary>Closes the dialog with a result.</summary>
    public void Close(TResult? result)
    {
        _completion.TrySetResult(result);
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Called when the window was closed without a choice (the title-bar X).</summary>
    public void CompleteWithDefault() => _completion.TrySetResult(default);
}

/// <summary>One button in a message dialog.</summary>
public sealed record DialogButton(string Label, string Result, bool IsDefault = false, bool IsCancel = false);

/// <summary>A message with one or more choices; also used for confirmations and three-way prompts.</summary>
public sealed partial class MessageDialogViewModel : DialogViewModel<string>
{
    public MessageDialogViewModel(string title, string message, IReadOnlyList<DialogButton> buttons)
    {
        Title = title;
        Message = message;
        Buttons = buttons;
    }

    public string Message { get; }

    public IReadOnlyList<DialogButton> Buttons { get; }

    [RelayCommand]
    private void Choose(string? result) => Close(result);
}

/// <summary>A single-line text prompt (rename, set speed for all).</summary>
public sealed partial class InputDialogViewModel : DialogViewModel<string>
{
    [ObservableProperty]
    private string _value;

    public InputDialogViewModel(string title, string label, string initialValue)
    {
        Title = title;
        Label = label;
        _value = initialValue;
    }

    public string Label { get; }

    [RelayCommand]
    private void Accept() => Close(Value);

    [RelayCommand]
    private void Cancel() => Close(null);
}
