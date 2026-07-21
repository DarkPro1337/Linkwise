namespace Linkwise.Core.Models;

public sealed record DiscoveredBrowserProfile(
    string BrowserId,
    string BrowserName,
    string DisplayName,
    string ExecutablePath,
    string UserDataDirectory,
    string ProfileDirectory,
    IReadOnlyList<string> LaunchArguments);
