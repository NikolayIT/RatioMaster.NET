namespace RatioMaster.App.Views.Dialogs
{
    using Avalonia.Controls;
    using Avalonia.Media;
    using RatioMaster.App.Services;

    public partial class DialogWindow : Window
    {
        public DialogWindow()
        {
            this.InitializeComponent();
            if (Localization.IsRightToLeft)
            {
                this.FlowDirection = FlowDirection.RightToLeft;
                this.Classes.Add("rtl");
            }

            this.DataContextChanged += (_, _) => this.Host.Content = this.DataContext;
        }
    }
}
