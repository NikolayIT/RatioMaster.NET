using Avalonia.Controls;

namespace RatioMaster.App.Views.Dialogs;

public partial class InputDialogView : UserControl
{
    public InputDialogView()
    {
        this.InitializeComponent();
        this.Loaded += (_, _) =>
        {
            var box = this.FindControl<TextBox>("ValueBox");
            box?.Focus();
            box?.SelectAll();
        };
    }
}
