using Linkwise.Core.BrowserProfiles;
using Linkwise.Core.Configuration;
using Linkwise.Core.Contracts;
using Linkwise.Core.Launching;
using Linkwise.Core.RuleEngine;

namespace Linkwise.Desktop;

public sealed class AppServices
{
    public AppServices()
    {
        ConfigStore = new JsonFileLinkwiseConfigStore(LinkwiseConfigPaths.GetDefaultConfigPath());
        RouteEngine = new UrlRouteEngine();
        Launcher = new ProcessUrlLauncher();
        BrowserProfileDiscovery = new ChromeBrowserProfileDiscovery();
        Router = new UrlRouter(ConfigStore, RouteEngine, Launcher);
    }

    public ILinkwiseConfigStore ConfigStore { get; }

    public UrlRouteEngine RouteEngine { get; }

    public IUrlLauncher Launcher { get; }

    public IBrowserProfileDiscovery BrowserProfileDiscovery { get; }

    public UrlRouter Router { get; }
}
