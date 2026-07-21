namespace Linkwise.Core.BrowserProfiles;

internal static class BrowserPathResolver
{
    public static string? FindExecutable(
        IReadOnlyList<string> platformPaths,
        IReadOnlyList<string> linuxExecutableNames)
    {
        var executablePath = platformPaths.FirstOrDefault(File.Exists);
        if (executablePath is not null || !OperatingSystem.IsLinux())
            return executablePath;

        return linuxExecutableNames
            .Select(FindExecutableOnPath)
            .FirstOrDefault(path => path is not null);
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
