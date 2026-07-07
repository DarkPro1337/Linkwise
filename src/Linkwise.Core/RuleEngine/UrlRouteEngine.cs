using System.Text.RegularExpressions;
using Linkwise.Core.Models;

namespace Linkwise.Core.RuleEngine;

public sealed class UrlRouteEngine
{
    public RouteMatchResult Match(LinkwiseConfig config, Uri url)
    {
        var targetById = config.BrowserTargets
            .Where(target => !string.IsNullOrWhiteSpace(target.Id))
            .GroupBy(target => target.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var matchedRule = config.Rules
            .Where(rule => rule.Enabled)
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(rule => IsMatch(rule, url));

        if (matchedRule is not null)
        {
            if (targetById.TryGetValue(matchedRule.TargetId, out var target))
            {
                return new RouteMatchResult(url, target, matchedRule, null);
            }

            return new RouteMatchResult(url, null, matchedRule, $"Rule '{matchedRule.Name}' points to missing target '{matchedRule.TargetId}'.");
        }

        if (!string.IsNullOrWhiteSpace(config.FallbackTargetId) &&
            targetById.TryGetValue(config.FallbackTargetId, out var fallbackTarget))
        {
            return new RouteMatchResult(url, fallbackTarget, null, null);
        }

        return new RouteMatchResult(url, null, null, "No matching rule and no valid fallback target configured.");
    }

    private static bool IsMatch(RoutingRule rule, Uri url)
    {
        if (string.IsNullOrWhiteSpace(rule.Pattern))
        {
            return false;
        }

        var host = url.Host.TrimEnd('.').ToLowerInvariant();
        var pattern = rule.Pattern.Trim().TrimEnd('.').ToLowerInvariant();

        return rule.MatchKind switch
        {
            RuleMatchKind.ExactHost => string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchKind.DomainSuffix => IsDomainSuffixMatch(host, pattern),
            RuleMatchKind.Wildcard => IsWildcardMatch(host, pattern),
            RuleMatchKind.Regex => IsRegexMatch(url.AbsoluteUri, rule.Pattern),
            _ => false
        };
    }

    private static bool IsDomainSuffixMatch(string host, string suffix)
    {
        return string.Equals(host, suffix, StringComparison.OrdinalIgnoreCase) ||
               host.EndsWith($".{suffix}", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsWildcardMatch(string host, string pattern)
    {
        var escapedPattern = Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal);
        return Regex.IsMatch(host, $"^{escapedPattern}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
    }

    private static bool IsRegexMatch(string url, string pattern)
    {
        try
        {
            return Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
