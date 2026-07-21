using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Linkwise.Core.Contracts;
using Microsoft.Win32;

namespace Linkwise.Platforms.Windows;

public sealed partial class WindowsDefaultHandlerRegistrar : IDefaultHandlerRegistrar
{
    private const string ApplicationName = "Linkwise";
    private const string ProgId = "Linkwise.Url";
    private const string CapabilitiesPath = @"Software\Linkwise\Capabilities";
    private const uint AssociationChanged = 0x08000000;
    private const uint FlushAssociationChanges = 0x00001003;

    public bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10);

    public async Task<DefaultHandlerRequestResult> RequestDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10))
            throw new PlatformNotSupportedException("Default-handler registration requires Windows 10 or later.");

        cancellationToken.ThrowIfCancellationRequested();

        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("The application executable path is unavailable.");
        if (!string.Equals(Path.GetFileName(executablePath), "Linkwise.Desktop.exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Linkwise must be running from a published Windows executable to register as a URL handler.");
        }

        RegisterApplication(executablePath);
        SHChangeNotify(AssociationChanged, FlushAssociationChanges, 0, 0);
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        OpenDefaultAppsSettings();

        return DefaultHandlerRequestResult.UserActionRequired;
    }

    [SupportedOSPlatform("windows10.0")]
    private static void RegisterApplication(string executablePath)
    {
        var iconReference = $"\"{executablePath}\",0";

        using (var progId = CreateSubKey($@"Software\Classes\{ProgId}"))
        {
            progId.SetValue(null, "Linkwise URL");
            progId.SetValue("URL Protocol", string.Empty);
        }

        using (var icon = CreateSubKey($@"Software\Classes\{ProgId}\DefaultIcon"))
            icon.SetValue(null, iconReference);

        using (var command = CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command"))
            command.SetValue(null, $"\"{executablePath}\" \"%1\"");

        using (var capabilities = CreateSubKey(CapabilitiesPath))
        {
            capabilities.SetValue("ApplicationName", ApplicationName);
            capabilities.SetValue("ApplicationDescription", "Routes web links to configured browser profiles.");
            capabilities.SetValue("ApplicationIcon", iconReference);
        }

        using (var urlAssociations = CreateSubKey($@"{CapabilitiesPath}\UrlAssociations"))
        {
            urlAssociations.SetValue("http", ProgId);
            urlAssociations.SetValue("https", ProgId);
        }

        using var registeredApplications = CreateSubKey(@"Software\RegisteredApplications");
        registeredApplications.SetValue(ApplicationName, CapabilitiesPath);
    }

    [SupportedOSPlatform("windows10.0")]
    private static RegistryKey CreateSubKey(string path)
    {
        return Registry.CurrentUser.CreateSubKey(path)
            ?? throw new InvalidOperationException($"Could not create registry key HKCU\\{path}.");
    }

    private static void OpenDefaultAppsSettings()
    {
        var settingsUri = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)
            ? $"ms-settings:defaultapps?registeredAppUser={Uri.EscapeDataString(ApplicationName)}"
            : "ms-settings:defaultapps";

        _ = Process.Start(new ProcessStartInfo
        {
            FileName = settingsUri,
            UseShellExecute = true
        }) ?? throw new InvalidOperationException("Could not open Windows Default Apps settings.");
    }

    [LibraryImport("shell32.dll")]
    private static partial void SHChangeNotify(uint eventId, uint flags, nint item1, nint item2);
}
