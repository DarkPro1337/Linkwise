using Avalonia.Controls;

namespace Linkwise.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, args) =>
        {
            if (args.IsProgrammatic)
                return;

            args.Cancel = true;
            Hide();
        };
    }
}
