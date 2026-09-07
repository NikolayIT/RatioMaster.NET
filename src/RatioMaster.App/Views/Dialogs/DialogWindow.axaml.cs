namespace RatioMaster.App.Views.Dialogs
{
    using Avalonia.Controls;

    public partial class DialogWindow : Window
    {
        public DialogWindow()
        {
            this.InitializeComponent();
            this.DataContextChanged += (_, _) => this.Host.Content = this.DataContext;
        }
    }
}
