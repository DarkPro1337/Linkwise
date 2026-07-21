using Linkwise.Core.Contracts;
using Linkwise.Core.Models;

namespace Linkwise.Core.BrowserProfiles;

public sealed class BrowserProfileDiscovery : IBrowserProfileDiscovery
{
    private readonly IReadOnlyList<IBrowserProfileDiscovery> _discoveries;

    public BrowserProfileDiscovery()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        _discoveries =
        [
            CreateChromium("chrome", "Google Chrome",
                Mac(home, "Google", "Chrome"), Win(local, "Google", "Chrome", "User Data"), Linux(home, "google-chrome"),
                MacApp("Google Chrome"), WinApps(programFiles, programFilesX86, local, "chrome.exe", "Google", "Chrome"),
                "google-chrome", "google-chrome-stable"),
            CreateChromium("brave", "Brave",
                Mac(home, "BraveSoftware", "Brave-Browser"), Win(local, "BraveSoftware", "Brave-Browser", "User Data"), Linux(home, "BraveSoftware", "Brave-Browser"),
                MacApp("Brave Browser"), WinApps(programFiles, programFilesX86, local, "brave.exe", "BraveSoftware", "Brave-Browser"),
                "brave-browser", "brave-browser-stable", "brave"),
            CreateChromium("vivaldi", "Vivaldi",
                Mac(home, "Vivaldi"), Win(local, "Vivaldi", "User Data"), Linux(home, "vivaldi"),
                MacApp("Vivaldi"), WinApps(programFiles, programFilesX86, local, "vivaldi.exe", "Vivaldi"),
                "vivaldi", "vivaldi-stable"),
            CreateChromium("opera", "Opera",
                Mac(home, "com.operasoftware.Opera"), Win(roaming, "Opera Software", "Opera Stable"), Linux(home, "opera"),
                MacApp("Opera"),
                [Path.Combine(local, "Programs", "Opera", "opera.exe")],
                "opera"),
            CreateChromium("opera-gx", "Opera GX",
                Mac(home, "com.operasoftware.OperaGX"), Win(roaming, "Opera Software", "Opera GX Stable"), null,
                "/Applications/Opera GX.app/Contents/MacOS/Opera",
                [Path.Combine(local, "Programs", "Opera GX", "opera.exe")]),
            CreateChromium("chromium", "Chromium",
                Mac(home, "Chromium"), Win(local, "Chromium", "User Data"), Linux(home, "chromium"),
                MacApp("Chromium"),
                [Path.Combine(local, "Chromium", "Application", "chrome.exe")],
                "chromium", "chromium-browser"),
            CreateChromium("yandex", "Yandex Browser",
                Mac(home, "Yandex", "YandexBrowser"), Win(local, "Yandex", "YandexBrowser", "User Data"), Linux(home, "yandex-browser"),
                "/Applications/Yandex.app/Contents/MacOS/Yandex",
                [Path.Combine(local, "Yandex", "YandexBrowser", "Application", "browser.exe")],
                "yandex-browser-stable", "yandex-browser"),
            new FirefoxBrowserProfileDiscovery(
                PlatformValues(
                    Path.Combine(home, "Library", "Application Support", "Firefox"),
                    Path.Combine(roaming, "Mozilla", "Firefox"),
                    Path.Combine(home, ".mozilla", "firefox")),
                PlatformValues(
                    "/Applications/Firefox.app/Contents/MacOS/firefox",
                    Path.Combine(programFiles, "Mozilla Firefox", "firefox.exe"),
                    string.Empty)
                    .Concat(OperatingSystem.IsMacOS()
                        ? ["/Applications/Firefox Developer Edition.app/Contents/MacOS/firefox"]
                        : OperatingSystem.IsWindows()
                        ? [
                            Path.Combine(programFilesX86, "Mozilla Firefox", "firefox.exe"),
                            Path.Combine(local, "Mozilla Firefox", "firefox.exe"),
                            Path.Combine(programFiles, "Firefox Developer Edition", "firefox.exe"),
                            Path.Combine(programFilesX86, "Firefox Developer Edition", "firefox.exe")
                        ]
                        : [])
                    .ToList(),
                ["firefox", "firefox-developer-edition"])
        ];
    }

    internal BrowserProfileDiscovery(IReadOnlyList<IBrowserProfileDiscovery> discoveries)
    {
        _discoveries = discoveries;
    }

    public async Task<IReadOnlyList<DiscoveredBrowserProfile>> DiscoverProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        var tasks = _discoveries.Select(discovery => DiscoverSafelyAsync(discovery, cancellationToken));
        var profileGroups = await Task.WhenAll(tasks);

        return profileGroups
            .SelectMany(profiles => profiles)
            .OrderBy(profile => BrowserSortOrder(profile.BrowserId))
            .ThenBy(profile => string.Equals(profile.ProfileDirectory, "Default", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<IReadOnlyList<DiscoveredBrowserProfile>> DiscoverSafelyAsync(
        IBrowserProfileDiscovery discovery,
        CancellationToken cancellationToken)
    {
        try
        {
            return await discovery.DiscoverProfilesAsync(cancellationToken);
        }
        catch (IOException)
        {
            return [];
        }
        catch (UnauthorizedAccessException)
        {
            return [];
        }
    }

    private static ChromiumBrowserProfileDiscovery CreateChromium(
        string id,
        string name,
        string? macUserData,
        string? windowsUserData,
        string? linuxUserData,
        string? macExecutable,
        IReadOnlyList<string> windowsExecutables,
        params string[] linuxExecutables)
    {
        return new ChromiumBrowserProfileDiscovery(new ChromiumBrowserDefinition(
            id,
            name,
            PlatformValues(macUserData, windowsUserData, linuxUserData),
            PlatformValues(macExecutable, windowsExecutables),
            linuxExecutables));
    }

    private static IReadOnlyList<string> PlatformValues(string? macValue, string? windowsValue, string? linuxValue)
    {
        var value = OperatingSystem.IsMacOS() ? macValue : OperatingSystem.IsWindows() ? windowsValue : linuxValue;
        return string.IsNullOrWhiteSpace(value) ? [] : [value];
    }

    private static IReadOnlyList<string> PlatformValues(string? macValue, IReadOnlyList<string> windowsValues)
    {
        if (OperatingSystem.IsMacOS())
            return string.IsNullOrWhiteSpace(macValue) ? [] : [macValue];

        return OperatingSystem.IsWindows()
            ? windowsValues.Where(path => !string.IsNullOrWhiteSpace(path)).ToList()
            : [];
    }

    private static string Mac(string home, params string[] parts)
    {
        return Path.Combine([home, "Library", "Application Support", .. parts]);
    }

    private static string Win(string root, params string[] parts)
    {
        return Path.Combine([root, .. parts]);
    }

    private static string Linux(string home, params string[] parts)
    {
        return Path.Combine([home, ".config", .. parts]);
    }

    private static string MacApp(string appName) => $"/Applications/{appName}.app/Contents/MacOS/{appName}";

    private static IReadOnlyList<string> WinApps(
        string programFiles,
        string programFilesX86,
        string local,
        string executableName,
        params string[] vendorAndBrowser)
    {
        var suffix = vendorAndBrowser.Append("Application").Append(executableName).ToArray();
        return
        [
            Path.Combine([programFiles, .. suffix]),
            Path.Combine([programFilesX86, .. suffix]),
            Path.Combine([local, .. suffix])
        ];
    }

    private static int BrowserSortOrder(string browserId) => browserId switch
    {
        "chrome" => 0,
        "firefox" => 1,
        "brave" => 2,
        "vivaldi" => 3,
        "opera" => 4,
        "opera-gx" => 5,
        "chromium" => 6,
        "yandex" => 7,
        _ => 100
    };
}
