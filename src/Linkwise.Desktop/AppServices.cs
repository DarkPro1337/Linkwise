using Linkwise.Core.BrowserProfiles;
using Linkwise.Core.Configuration;
using Linkwise.Core.Contracts;
using Linkwise.Core.Launching;
using Linkwise.Core.RuleEngine;
#if LINKWISE_MACOS
using Linkwise.Platforms.Mac;
#elif LINKWISE_WINDOWS
using Linkwise.Platforms.Windows;
#endif

namespace Linkwise.Desktop;

public sealed class AppServices
{
    public AppServices()
    {
        ConfigStore = new JsonFileLinkwiseConfigStore(LinkwiseConfigPaths.GetDefaultConfigPath());
        RouteEngine = new UrlRouteEngine();
        Launcher = new ProcessUrlLauncher();
        BrowserProfileDiscovery = new BrowserProfileDiscovery();
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
#if LINKWISE_MACOS
        if (OperatingSystem.IsMacOS())
            return new MacDefaultHandlerRegistrar();
#elif LINKWISE_WINDOWS
        if (OperatingSystem.IsWindows())
            return new WindowsDefaultHandlerRegistrar();
#endif

        return new UnsupportedDefaultHandlerRegistrar();
    }
}
