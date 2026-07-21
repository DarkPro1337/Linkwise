using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Linkwise.Core.Incoming;
using Linkwise.Desktop.Platforms.Windows;
using Linkwise.Desktop.Services;
using Linkwise.Desktop.ViewModels;

namespace Linkwise.Desktop;

public class App : Application
{
    private readonly AppServices _services = new();
    private IDisposable? _trayIconThemeController;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (OperatingSystem.IsWindows())
            {
                _trayIconThemeController = new WindowsTrayIconThemeController(this);
                desktop.Exit += HandleDesktopExit;
            }

            if (this.TryGetFeature<IActivatableLifetime>() is { } activatableLifetime)
                activatableLifetime.Activated += HandleApplicationActivated;

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

    private void HandleDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs args)
    {
        _trayIconThemeController?.Dispose();
        _trayIconThemeController = null;
    }

    private async void HandleApplicationActivated(object? sender, ActivatedEventArgs args)
    {
        try
        {
            if (args is not ProtocolActivatedEventArgs { Kind: ActivationKind.OpenUri, Uri: var url } || !IsHttpUrl(url))
                return;

            await _services.Router.RouteAsync(url);
        }
        catch
        {
            // The application remains available from the tray after a failed activation.
        }
    }

    private async Task RouteIncomingUrlAndShutdownAsync(Uri url, IClassicDesktopStyleApplicationLifetime desktop)
    {
        try
        {
            await _services.Router.RouteAsync(url);
            desktop.Shutdown();
        }
        catch
        {
            desktop.Shutdown(1);
        }
    }

    private static bool IsHttpUrl(Uri url) => url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps;
}
