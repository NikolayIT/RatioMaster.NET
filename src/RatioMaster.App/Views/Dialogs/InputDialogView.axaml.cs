namespace RatioMaster.App.Views.Dialogs
{
    using Avalonia.Controls;

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
}
