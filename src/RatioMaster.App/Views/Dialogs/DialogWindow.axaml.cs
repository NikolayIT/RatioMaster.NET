using Avalonia.Controls;

namespace RatioMaster.App.Views.Dialogs;

public partial class DialogWindow : Window
{
    public DialogWindow()
    {
        this.InitializeComponent();
        this.DataContextChanged += (_, _) => this.Host.Content = this.DataContext;
    }
}
