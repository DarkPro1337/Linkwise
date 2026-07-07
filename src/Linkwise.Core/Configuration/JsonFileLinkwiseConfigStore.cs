using System.Text.Json;
using System.Text.Json.Serialization;
using Linkwise.Core.Contracts;
using Linkwise.Core.Models;

namespace Linkwise.Core.Configuration;

public sealed class JsonFileLinkwiseConfigStore(string filePath) : ILinkwiseConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public string FilePath { get; } = filePath;

    public async Task<LinkwiseConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(FilePath))
        {
            var defaultConfig = LinkwiseConfigDefaults.Create();
            await SaveAsync(defaultConfig, cancellationToken);
            return defaultConfig;
        }

        await using var stream = File.OpenRead(FilePath);
        var config = await JsonSerializer.DeserializeAsync<LinkwiseConfig>(stream, JsonOptions, cancellationToken);
        return config ?? LinkwiseConfigDefaults.Create();
    }

    public async Task SaveAsync(LinkwiseConfig config, CancellationToken cancellationToken = default)
    {
        var directoryPath = Path.GetDirectoryName(FilePath);
        if (!string.IsNullOrEmpty(directoryPath))
            Directory.CreateDirectory(directoryPath);

        await using var stream = File.Create(FilePath);
        await JsonSerializer.SerializeAsync(stream, config, JsonOptions, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }
}
