using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Linkwise.Core.Incoming;
using Linkwise.Desktop.Services;
using Linkwise.Desktop.ViewModels;

namespace Linkwise.Desktop;

public partial class App : Application
{
    private readonly AppServices _services = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var shell = new DesktopApplicationShell(desktop, _services);
            DataContext = new AppViewModel(shell);

            var incomingUrl = IncomingUrlParser.FindHttpUrl(desktop.Args);
            if (incomingUrl is not null)
            {
                _ = RouteIncomingUrlAndShutdownAsync(incomingUrl, desktop);
            }
            else
            {
                desktop.MainWindow = shell.CreateMainWindow();
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task RouteIncomingUrlAndShutdownAsync(Uri url, IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            await _services.Router.RouteAsync(url);
            desktop.Shutdown(0);
        }
        catch
        {
            desktop.Shutdown(1);
        }
    }
}
