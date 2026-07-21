using Linkwise.Core.Contracts;
using Linkwise.Core.Models;

namespace Linkwise.Core.BrowserProfiles;

internal sealed class FirefoxBrowserProfileDiscovery(
    IReadOnlyList<string> profileRoots,
    IReadOnlyList<string> executablePaths,
    IReadOnlyList<string> linuxExecutableNames) : IBrowserProfileDiscovery
{
    public async Task<IReadOnlyList<DiscoveredBrowserProfile>> DiscoverProfilesAsync(
        CancellationToken cancellationToken = default)
    {
        var profileRoot = profileRoots.FirstOrDefault(directory =>
            File.Exists(Path.Combine(directory, "profiles.ini")) ||
            Directory.Exists(Path.Combine(directory, "Profiles")));
        if (profileRoot is null)
            return [];

        var executablePath = BrowserPathResolver.FindExecutable(executablePaths, linuxExecutableNames);
        if (executablePath is null)
            return [];

        var profilesIniPath = Path.Combine(profileRoot, "profiles.ini");
        var lines = File.Exists(profilesIniPath)
            ? await File.ReadAllLinesAsync(profilesIniPath, cancellationToken)
            : [];
        var profiles = ParseProfiles(lines, profileRoot).ToList();
        AddUnlistedProfileDirectories(profiles, profileRoot);

        return profiles
            .OrderByDescending(profile => profile.IsDefault)
            .ThenBy(profile => profile.Name, StringComparer.OrdinalIgnoreCase)
            .Select(profile => new DiscoveredBrowserProfile(
                "firefox",
                "Firefox",
                profile.Name,
                executablePath,
                profileRoot,
                profile.ProfilePath,
                ["-profile", profile.ProfilePath]))
            .ToList();
    }

    private static void AddUnlistedProfileDirectories(List<FirefoxProfileInfo> profiles, string profileRoot)
    {
        var profilesDirectory = Path.Combine(profileRoot, "Profiles");
        if (!Directory.Exists(profilesDirectory))
            return;

        var knownPaths = profiles
            .Select(profile => profile.ProfilePath)
            .ToHashSet(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

        foreach (var directory in Directory.EnumerateDirectories(profilesDirectory))
        {
            var fullPath = Path.GetFullPath(directory);
            if (knownPaths.Contains(fullPath) ||
                (!File.Exists(Path.Combine(fullPath, "prefs.js")) &&
                 !File.Exists(Path.Combine(fullPath, "places.sqlite"))))
            {
                continue;
            }

            profiles.Add(new FirefoxProfileInfo(Path.GetFileName(fullPath), fullPath, false));
        }
    }

    private static IReadOnlyList<FirefoxProfileInfo> ParseProfiles(IEnumerable<string> lines, string profileRoot)
    {
        var profiles = new List<FirefoxProfileInfo>();
        string? section = null;
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void AddCurrentProfile()
        {
            if (section is null || !section.StartsWith("Profile", StringComparison.OrdinalIgnoreCase) ||
                !values.TryGetValue("Path", out var configuredPath))
            {
                return;
            }

            var isRelative = !values.TryGetValue("IsRelative", out var relativeValue) || relativeValue == "1";
            var profilePath = isRelative ? Path.Combine(profileRoot, configuredPath) : configuredPath;
            if (!Directory.Exists(profilePath))
                return;

            var name = values.GetValueOrDefault("Name");
            if (string.IsNullOrWhiteSpace(name))
                name = Path.GetFileName(profilePath);

            profiles.Add(new FirefoxProfileInfo(
                name,
                Path.GetFullPath(profilePath),
                values.GetValueOrDefault("Default") == "1"));
        }

        foreach (var rawLine in lines.Append("[End]"))
        {
            var line = rawLine.Trim();
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                AddCurrentProfile();
                section = line[1..^1];
                values.Clear();
                continue;
            }

            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                continue;

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
                continue;

            values[line[..separatorIndex].Trim()] = line[(separatorIndex + 1)..].Trim();
        }

        return profiles;
    }

    private sealed record FirefoxProfileInfo(string Name, string ProfilePath, bool IsDefault);
}
