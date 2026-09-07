using System.Collections.Generic;
using System.Threading.Tasks;
using RatioMaster.App.Services.Abstractions;
using RatioMaster.App.ViewModels.Dialogs;
using RatioMaster.App.Views.Dialogs;

namespace RatioMaster.App.Services;

/// <summary>Shows dialogs hosted in a shared window; the content view is resolved by the view locator.</summary>
public interface IDialogService
{
    /// <summary>Shows any dialog view model and returns its result.</summary>
    Task<TResult?> ShowAsync<TResult>(DialogViewModel<TResult> viewModel);

    Task ShowMessageAsync(string title, string message);

    Task<bool> ConfirmAsync(string title, string message, string okText = "OK", string cancelText = "Cancel");

    Task<string?> ChooseAsync(string title, string message, IReadOnlyList<DialogButton> buttons);

    Task<string?> PromptAsync(string title, string label, string initialValue);
}
