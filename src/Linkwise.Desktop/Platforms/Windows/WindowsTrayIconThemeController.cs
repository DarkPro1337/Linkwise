using System.Runtime.Versioning;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using Linkwise.Platforms.Windows;

namespace Linkwise.Desktop.Platforms.Windows;

[SupportedOSPlatform("windows")]
internal sealed class WindowsTrayIconThemeController : IDisposable
{
    private readonly TrayIcon _trayIcon;
    private readonly WindowIcon _darkThemeIcon;
    private readonly WindowIcon _lightThemeIcon;
    private readonly WindowsShellThemeWatcher _shellThemeWatcher;
    private bool _disposed;

    public WindowsTrayIconThemeController(Application application)
    {
        var trayIcons = TrayIcon.GetIcons(application);
        if (trayIcons is not { Count: > 0 })
            throw new InvalidOperationException("The application tray icon is not configured.");

        _trayIcon = trayIcons[0];
        _darkThemeIcon = LoadIcon("/Assets/tray-windows-dark.ico");
        _lightThemeIcon = LoadIcon("/Assets/tray-windows-light.ico");

        _shellThemeWatcher = new WindowsShellThemeWatcher();
        _shellThemeWatcher.ThemeChanged += HandleShellThemeChanged;
        UpdateIcon(_shellThemeWatcher.IsLightTheme);
    }

    private void HandleShellThemeChanged(object? sender, WindowsShellThemeChangedEventArgs args)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!_disposed)
                UpdateIcon(args.IsLightTheme);
        });
    }

    private void UpdateIcon(bool shellUsesLightTheme)
    {
        _trayIcon.Icon = shellUsesLightTheme ? _lightThemeIcon : _darkThemeIcon;
    }

    private static WindowIcon LoadIcon(string path)
    {
        return new WindowIcon(AssetLoader.Open(new Uri($"avares://Linkwise.Desktop{path}")));
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _shellThemeWatcher.ThemeChanged -= HandleShellThemeChanged;
        _shellThemeWatcher.Dispose();
    }
}
