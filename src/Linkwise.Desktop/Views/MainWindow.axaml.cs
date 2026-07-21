using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;

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

    private void ToggleTheme(object? sender, RoutedEventArgs args)
    {
        if (Application.Current is not { } application)
            return;

        application.RequestedThemeVariant = application.ActualThemeVariant == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }
}
