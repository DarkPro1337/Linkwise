using Linkwise.Core.Models;

namespace Linkwise.Core.Contracts;

public interface ILinkwiseConfigStore
{
    string FilePath { get; }

    Task<LinkwiseConfig> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(LinkwiseConfig config, CancellationToken cancellationToken = default);
}
