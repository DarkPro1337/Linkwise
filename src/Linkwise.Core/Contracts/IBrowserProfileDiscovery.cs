using Linkwise.Core.Models;

namespace Linkwise.Core.Contracts;

public interface IBrowserProfileDiscovery
{
    Task<IReadOnlyList<DiscoveredBrowserProfile>> DiscoverProfilesAsync(CancellationToken cancellationToken = default);
}
