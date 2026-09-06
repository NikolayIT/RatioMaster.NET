using Avalonia.Controls;

namespace RatioMaster.App.Views.Dialogs;

public partial class DialogWindow : Window
{
    public DialogWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Host.Content = DataContext;
    }
}
