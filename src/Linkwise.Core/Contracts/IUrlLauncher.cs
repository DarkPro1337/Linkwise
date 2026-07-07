using Linkwise.Core.Models;

namespace Linkwise.Core.Contracts;

public interface IUrlLauncher
{
    Task LaunchAsync(BrowserTarget target, Uri url, CancellationToken cancellationToken = default);
}
