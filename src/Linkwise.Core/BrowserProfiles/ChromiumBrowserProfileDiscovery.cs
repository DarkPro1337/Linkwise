using System.Text.Json;
using Linkwise.Core.Contracts;
using Linkwise.Core.Models;

namespace Linkwise.Core.BrowserProfiles;

internal sealed class ChromiumBrowserProfileDiscovery(ChromiumBrowserDefinition browser) : IBrowserProfileDiscovery
{
    public async Task<IReadOnlyList<DiscoveredBrowserProfile>> DiscoverProfilesAsync(CancellationToken cancellationToken = default)
    {
        var userDataDirectory = browser.UserDataDirectories.FirstOrDefault(Directory.Exists);
        if (userDataDirectory is null)
            return [];

        var executablePath = BrowserPathResolver.FindExecutable(browser.ExecutablePaths, browser.LinuxExecutableNames);
        if (executablePath is null)
            return [];

        List<ChromiumProfileInfo> profiles;
        try
        {
            profiles = await ReadProfilesFromLocalStateAsync(userDataDirectory, cancellationToken);
        }
        catch (IOException)
        {
            profiles = [];
        }
        catch (JsonException)
        {
            profiles = [];
        }

        if (profiles.Count == 0)
            profiles = ScanProfileDirectories(userDataDirectory);

        return profiles
            .OrderBy(profile => profile.ProfileDirectory == "Default" ? 0 : 1)
            .ThenBy(profile => profile.DisplayName, StringComparer.OrdinalIgnoreCase)
            .Select(profile => new DiscoveredBrowserProfile(
                browser.Id,
                browser.Name,
                profile.DisplayName,
                executablePath,
                userDataDirectory,
                profile.ProfileDirectory,
                [$"--profile-directory={profile.ProfileDirectory}"]))
            .ToList();
    }

    private static async Task<List<ChromiumProfileInfo>> ReadProfilesFromLocalStateAsync(
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

        var profiles = new List<ChromiumProfileInfo>();
        foreach (var profileProperty in infoCacheElement.EnumerateObject())
        {
            var profileDirectory = profileProperty.Name;
            if (!Directory.Exists(Path.Combine(userDataDirectory, profileDirectory)))
                continue;

            var displayName = ReadDisplayName(profileProperty.Value, profileDirectory);
            profiles.Add(new ChromiumProfileInfo(displayName, profileDirectory));
        }

        return profiles;
    }

    private static List<ChromiumProfileInfo> ScanProfileDirectories(string userDataDirectory)
    {
        return Directory.EnumerateDirectories(userDataDirectory)
            .Select(Path.GetFileName)
            .Where(directoryName => directoryName is not null &&
                                    (string.Equals(directoryName, "Default", StringComparison.OrdinalIgnoreCase) ||
                                     directoryName.StartsWith("Profile ", StringComparison.OrdinalIgnoreCase)))
            .Select(directoryName => new ChromiumProfileInfo(directoryName!, directoryName!))
            .ToList();
    }

    private static string ReadDisplayName(JsonElement profileElement, string profileDirectory)
    {
        foreach (var propertyName in new[] { "name", "gaia_name", "user_name" })
        {
            if (profileElement.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
                return value.GetString()!;
        }

        return profileDirectory;
    }

    private sealed record ChromiumProfileInfo(string DisplayName, string ProfileDirectory);
}
