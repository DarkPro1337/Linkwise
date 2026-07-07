namespace Linkwise.Core.Models;

public sealed class BrowserTarget
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string ExecutablePath { get; init; } = string.Empty;

    public List<string> Arguments { get; init; } = [];
}
