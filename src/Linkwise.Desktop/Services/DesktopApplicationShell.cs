using Avalonia.Controls.ApplicationLifetimes;
using Linkwise.Desktop.ViewModels;
using Linkwise.Desktop.Views;

namespace Linkwise.Desktop.Services;

public sealed class DesktopApplicationShell(
    IClassicDesktopStyleApplicationLifetime desktopLifetime,
    AppServices services) : IApplicationShell
{
    public MainWindow CreateMainWindow()
    {
        return new MainWindow
        {
            DataContext = new MainWindowViewModel(
                services.ConfigStore,
                services.RouteEngine,
                services.Launcher,
                services.BrowserProfileDiscovery)
        };
    }

    public void ShowMainWindow()
    {
        desktopLifetime.MainWindow ??= CreateMainWindow();
        desktopLifetime.MainWindow.Show();
        desktopLifetime.MainWindow.Activate();
    }

    public void Quit()
    {
        desktopLifetime.Shutdown();
    }
}
