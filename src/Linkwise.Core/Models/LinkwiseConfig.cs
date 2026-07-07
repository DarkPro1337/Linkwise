namespace Linkwise.Core.Models;

public sealed class LinkwiseConfig
{
    public List<BrowserTarget> BrowserTargets { get; init; } = [];

    public List<RoutingRule> Rules { get; init; } = [];

    public string? FallbackTargetId { get; init; }
}
