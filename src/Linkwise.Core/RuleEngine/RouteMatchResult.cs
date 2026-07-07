using Linkwise.Core.Models;

namespace Linkwise.Core.RuleEngine;

public sealed record RouteMatchResult(Uri Url, BrowserTarget? Target, RoutingRule? Rule, string? Error)
{
    public bool IsMatch => Target is not null && Error is null;
}
