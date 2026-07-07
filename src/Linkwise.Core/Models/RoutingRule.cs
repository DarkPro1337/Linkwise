namespace Linkwise.Core.Models;

public sealed class RoutingRule
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool Enabled { get; init; } = true;

    public int Priority { get; init; }

    public RuleMatchKind MatchKind { get; init; } = RuleMatchKind.ExactHost;

    public string Pattern { get; init; } = string.Empty;

    public string TargetId { get; init; } = string.Empty;
}
