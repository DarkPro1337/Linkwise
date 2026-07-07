using System.Text.Json;
using Linkwise.Core.Contracts;
using Linkwise.Core.Models;

namespace Linkwise.Core.BrowserProfiles;

public sealed class ChromeBrowserProfileDiscovery : IBrowserProfileDiscovery
{
    private const string BrowserName = "Google Chrome";

    public async Task<IReadOnlyList<DiscoveredBrowserProfile>> DiscoverProfilesAsync(CancellationToken cancellationToken = default)
    {
        var userDataDirectory = ChromeBrowserPaths.GetUserDataDirectory();
        if (string.IsNullOrWhiteSpace(userDataDirectory) || !Directory.Exists(userDataDirectory))
            return [];

        var executablePath = ChromeBrowserPaths.GetExecutablePath();
        if (string.IsNullOrWhiteSpace(executablePath))
            return [];

        var profiles = await ReadProfilesFromLocalStateAsync(userDataDirectory, cancellationToken);
        if (profiles.Count == 0)
            profiles = ScanProfileDirectories(userDataDirectory);

        return profiles
            .OrderBy(profile => profile.ProfileDirectory == "Default" ? 0 : 1)
            .ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(profile => new DiscoveredBrowserProfile(
                BrowserName,
                profile.DisplayName,
                executablePath,
                userDataDirectory,
                profile.ProfileDirectory))
            .ToList();
    }

    private static async Task<List<ChromeProfileInfo>> ReadProfilesFromLocalStateAsync(
        string userDataDirectory,
        CancellationToken cancellationToken)
    {
        var localStatePath = Path.Combine(userDataDirectory, "Local State");
        if (!File.Exists(localStatePath))
            return [];

        await using var stream = File.OpenRead(localStatePath);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        if (!document.RootElement.TryGetProperty("profile", out var profileElement) ||
            !profileElement.TryGetProperty("info_cache", out var infoCacheElement) ||
            infoCacheElement.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var profiles = new List<ChromeProfileInfo>();
        foreach (var profileProperty in infoCacheElement.EnumerateObject())
        {
            var profileDirectory = profileProperty.Name;
            if (!Directory.Exists(Path.Combine(userDataDirectory, profileDirectory)))
                continue;

            var displayName = ReadDisplayName(profileProperty.Value, profileDirectory);
            profiles.Add(new ChromeProfileInfo(displayName, profileDirectory));
        }

        return profiles;
    }

    private static List<ChromeProfileInfo> ScanProfileDirectories(string userDataDirectory)
    {
        return Directory.EnumerateDirectories(userDataDirectory)
            .Select(Path.GetFileName)
            .Where(directoryName => directoryName is not null &&
                                    (string.Equals(directoryName, "Default", StringComparison.OrdinalIgnoreCase) ||
                                     directoryName.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase)))
            .Select(directoryName => new ChromeProfileInfo(directoryName!, directoryName!))
            .ToList();
    }

    private static string ReadDisplayName(JsonElement profileElement, string profileDirectory)
    {
        foreach (var propertyName in new[] { "name", "gaia_name", "user_name" })
        {
            if (profileElement.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return value.GetString()!;
            }
        }

        return profileDirectory;
    }

    private sealed record ChromeProfileInfo(string DisplayName, string ProfileDirectory);
}
