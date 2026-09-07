using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RatioMaster.App.ViewModels.Dialogs;

/// <summary>A message with one or more choices; also used for confirmations and three-way prompts.</summary>
public sealed partial class MessageDialogViewModel : DialogViewModel<string>
{
    public MessageDialogViewModel(string title, string message, IReadOnlyList<DialogButton> buttons)
    {
        this.Title = title;
        this.Message = message;
        this.Buttons = buttons;
    }

    public string Message { get; }

    public IReadOnlyList<DialogButton> Buttons { get; }

    [RelayCommand]
    private void Choose(string? result) => this.Close(result);
}
