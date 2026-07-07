namespace Linkwise.Core.Models;

public sealed record DiscoveredBrowserProfile(
    string BrowserName,
    string DisplayName,
    string ExecutablePath,
    string UserDataDirectory,
    string ProfileDirectory);
