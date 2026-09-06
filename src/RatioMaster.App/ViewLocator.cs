using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using RatioMaster.App.ViewModels;

namespace RatioMaster.App;

/// <summary>Maps a XxxViewModel to its XxxView by naming convention.</summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? param)
    {
        if (param is null)
        {
            return new TextBlock { Text = "No view model" };
        }

        var name = param.GetType().FullName!
            .Replace("ViewModels.Dialogs.", "Views.Dialogs.", StringComparison.Ordinal)
            .Replace("ViewModels.", "Views.", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);
        var type = Type.GetType(name);

        return type is not null
            ? (Control)Activator.CreateInstance(type)!
            : new TextBlock { Text = "View not found: " + name };
    }

    public bool Match(object? data) => data is ViewModelBase;
}
