using System.Runtime.InteropServices;

namespace Linkwise.Core.BrowserProfiles;

internal static class ChromeBrowserPaths
{
    public static string? GetUserDataDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "Google", "Chrome");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Google", "Chrome", "User Data");
        }

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(profile, ".config", "google-chrome");
    }

    public static string? GetExecutablePath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return ExistingFileOrNull("/Applications/Google Chrome.app/Contents/MacOS/Google Chrome");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google", "Chrome", "Application", "chrome.exe")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        return FindExecutableOnPath("google-chrome") ??
               FindExecutableOnPath("google-chrome-stable") ??
               FindExecutableOnPath("chromium");
    }

    private static string? ExistingFileOrNull(string path)
    {
        return File.Exists(path) ? path : null;
    }

    private static string? FindExecutableOnPath(string executableName)
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
            return null;

        return path
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(directory => Path.Combine(directory, executableName))
            .FirstOrDefault(File.Exists);
    }
}
