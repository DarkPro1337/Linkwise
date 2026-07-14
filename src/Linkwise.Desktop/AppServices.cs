using Linkwise.Core.BrowserProfiles;
using Linkwise.Core.Configuration;
using Linkwise.Core.Contracts;
using Linkwise.Core.Launching;
using Linkwise.Core.RuleEngine;
using Linkwise.Platforms.Mac;
using Linkwise.Platforms.Windows;

namespace Linkwise.Desktop;

public sealed class AppServices
{
    public AppServices()
    {
        ConfigStore = new JsonFileLinkwiseConfigStore(LinkwiseConfigPaths.GetDefaultConfigPath());
        RouteEngine = new UrlRouteEngine();
        Launcher = new ProcessUrlLauncher();
        BrowserProfileDiscovery = new ChromeBrowserProfileDiscovery();
        DefaultHandlerRegistrar = CreateDefaultHandlerRegistrar();
        Router = new UrlRouter(ConfigStore, RouteEngine, Launcher);
    }

    public ILinkwiseConfigStore ConfigStore { get; }

    public UrlRouteEngine RouteEngine { get; }

    public IUrlLauncher Launcher { get; }

    public IBrowserProfileDiscovery BrowserProfileDiscovery { get; }

    public IDefaultHandlerRegistrar DefaultHandlerRegistrar { get; }

    public UrlRouter Router { get; }

    private static IDefaultHandlerRegistrar CreateDefaultHandlerRegistrar()
    {
        if (OperatingSystem.IsMacOS())
            return new MacDefaultHandlerRegistrar();

        if (OperatingSystem.IsWindows())
            return new WindowsDefaultHandlerRegistrar();

        return new UnsupportedDefaultHandlerRegistrar();
    }
}
