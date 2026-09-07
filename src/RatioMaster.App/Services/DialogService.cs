using System.Collections.Generic;
using System.Threading.Tasks;
using RatioMaster.App.Services.Abstractions;
using RatioMaster.App.ViewModels.Dialogs;
using RatioMaster.App.Views.Dialogs;

namespace RatioMaster.App.Services;

public sealed class DialogService(IMainWindowProvider windows) : IDialogService
{
    public async Task<TResult?> ShowAsync<TResult>(DialogViewModel<TResult> viewModel)
    {
        var owner = windows.Window;
        if (owner is null)
        {
            return default;
        }

        var window = new DialogWindow { DataContext = viewModel, Title = viewModel.Title };
        void OnCloseRequested(object? sender, System.EventArgs e) => window.Close();
        viewModel.CloseRequested += OnCloseRequested;
        window.Closed += (_, _) => viewModel.CompleteWithDefault();

        try
        {
            await window.ShowDialog(owner);
            return await viewModel.Result;
        }
        finally
        {
            viewModel.CloseRequested -= OnCloseRequested;
        }
    }

    public Task ShowMessageAsync(string title, string message) =>
        this.ShowAsync(new MessageDialogViewModel(title, message, [new DialogButton("OK", "ok", IsDefault: true, IsCancel: true)]));

    public async Task<bool> ConfirmAsync(string title, string message, string okText = "OK", string cancelText = "Cancel")
    {
        var result = await this.ShowAsync(new MessageDialogViewModel(
            title,
            message,
            [
                new DialogButton(okText, "ok", IsDefault: true),
                new DialogButton(cancelText, "cancel", IsCancel: true),
            ]));

        return result == "ok";
    }

    public Task<string?> ChooseAsync(string title, string message, IReadOnlyList<DialogButton> buttons) =>
        this.ShowAsync(new MessageDialogViewModel(title, message, buttons));

    public Task<string?> PromptAsync(string title, string label, string initialValue) =>
        this.ShowAsync(new InputDialogViewModel(title, label, initialValue));
}
