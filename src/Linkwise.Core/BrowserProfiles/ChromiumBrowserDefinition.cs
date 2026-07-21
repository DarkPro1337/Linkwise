namespace Linkwise.Core.BrowserProfiles;

internal sealed record ChromiumBrowserDefinition(
    string Id,
    string Name,
    IReadOnlyList<string> UserDataDirectories,
    IReadOnlyList<string> ExecutablePaths,
    IReadOnlyList<string> LinuxExecutableNames);
