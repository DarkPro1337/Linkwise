namespace Linkwise.Core.Configuration;

public static class LinkwiseConfigPaths
{
    public static string GetDefaultConfigPath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrWhiteSpace(appDataPath))
            appDataPath = AppContext.BaseDirectory;

        return Path.Combine(appDataPath, "Linkwise", "config.json");
    }
}
