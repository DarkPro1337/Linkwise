using Linkwise.Core.Contracts;
using Linkwise.Core.RuleEngine;

namespace Linkwise.Core.Launching;

public sealed class UrlRouter(ILinkwiseConfigStore configStore, UrlRouteEngine routeEngine, IUrlLauncher launcher)
{
    public async Task<RouteMatchResult> RouteAsync(Uri url, CancellationToken cancellationToken = default)
    {
        var config = await configStore.LoadAsync(cancellationToken);
        var match = routeEngine.Match(config, url);
        if (!match.IsMatch || match.Target is null)
            return match;

        await launcher.LaunchAsync(match.Target, url, cancellationToken);
        return match;
    }
}
