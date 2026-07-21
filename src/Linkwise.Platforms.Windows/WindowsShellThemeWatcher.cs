using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;

namespace Linkwise.Platforms.Windows;

[SupportedOSPlatform("windows")]
public sealed partial class WindowsShellThemeWatcher : IDisposable
{
    private const string PersonalizeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string SystemUsesLightThemeValueName = "SystemUsesLightTheme";
    private const int ErrorSuccess = 0;

    private readonly CancellationTokenSource _cancellation = new();
    private readonly Task _watchTask;

    private bool _isLightTheme;
    private bool _disposed;

    public WindowsShellThemeWatcher()
    {
        _isLightTheme = ReadIsLightTheme();
        _watchTask = Task.Run(() => WatchRegistry(_cancellation.Token));
    }

    public bool IsLightTheme => _isLightTheme;

    public event EventHandler<WindowsShellThemeChangedEventArgs>? ThemeChanged;

    private void WatchRegistry(CancellationToken cancellationToken)
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
        if (key is null)
            return;

        using var registryChanged = new AutoResetEvent(false);
        var waitHandles = new[] { registryChanged, cancellationToken.WaitHandle };

        while (!cancellationToken.IsCancellationRequested)
        {
            var result = RegNotifyChangeKeyValue(
                key.Handle,
                watchSubtree: false,
                RegNotifyFilter.LastSet,
                registryChanged.SafeWaitHandle,
                asynchronous: true);

            if (result != ErrorSuccess)
                return;

            if (WaitHandle.WaitAny(waitHandles) != 0)
                return;

            var isLightTheme = ReadIsLightTheme();
            if (_isLightTheme == isLightTheme)
                continue;

            _isLightTheme = isLightTheme;
            ThemeChanged?.Invoke(this, new WindowsShellThemeChangedEventArgs(isLightTheme));
        }
    }

    private static bool ReadIsLightTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryPath);
        return key?.GetValue(SystemUsesLightThemeValueName) is int value && value != 0;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _cancellation.Cancel();
        _watchTask.GetAwaiter().GetResult();
        _cancellation.Dispose();
    }

    [Flags]
    private enum RegNotifyFilter : uint
    {
        LastSet = 0x00000004
    }

    [LibraryImport("advapi32.dll")]
    private static partial int RegNotifyChangeKeyValue(
        SafeRegistryHandle hKey,
        [MarshalAs(UnmanagedType.Bool)] bool watchSubtree,
        RegNotifyFilter notifyFilter,
        SafeWaitHandle eventHandle,
        [MarshalAs(UnmanagedType.Bool)] bool asynchronous);
}

public sealed class WindowsShellThemeChangedEventArgs(bool isLightTheme) : EventArgs
{
    public bool IsLightTheme { get; } = isLightTheme;
}
