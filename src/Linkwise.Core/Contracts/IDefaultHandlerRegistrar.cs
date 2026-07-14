namespace Linkwise.Core.Contracts;

public interface IDefaultHandlerRegistrar
{
    bool IsSupported { get; }

    Task<DefaultHandlerRequestResult> RequestDefaultAsync(CancellationToken cancellationToken = default);
}
