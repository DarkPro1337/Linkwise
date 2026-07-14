namespace Linkwise.Core.Contracts;

public sealed class UnsupportedDefaultHandlerRegistrar : IDefaultHandlerRegistrar
{
    public bool IsSupported => false;

    public Task<DefaultHandlerRequestResult> RequestDefaultAsync(CancellationToken cancellationToken = default)
    {
        throw new PlatformNotSupportedException("Default-handler registration is not supported on this platform.");
    }
}
