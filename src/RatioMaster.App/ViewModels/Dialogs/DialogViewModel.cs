using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RatioMaster.App.ViewModels.Dialogs;

/// <summary>Base for dialogs: carries a title and completes with a result when closed.</summary>
/// <typeparam name="TResult">What the dialog hands back when it closes; null when it was dismissed.</typeparam>
public abstract class DialogViewModel<TResult> : ViewModelBase
{
    private readonly TaskCompletionSource<TResult?> completion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Raised when the view model wants its window closed.</summary>
    public event EventHandler? CloseRequested;

    public string Title { get; init; } = "RatioMaster.NET";

    /// <summary>Gets the task that completes when the dialog closes.</summary>
    public Task<TResult?> Result => this.completion.Task;

    /// <summary>Closes the dialog with a result.</summary>
    public void Close(TResult? result)
    {
        this.completion.TrySetResult(result);
        this.CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Called when the window was closed without a choice (the title-bar X).</summary>
    public void CompleteWithDefault() => this.completion.TrySetResult(default);
}
