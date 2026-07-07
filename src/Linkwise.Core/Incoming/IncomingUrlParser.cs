namespace Linkwise.Core.Incoming;

public static class IncomingUrlParser
{
    public static Uri? FindHttpUrl(IEnumerable<string>? args)
    {
        if (args is null)
            return null;

        foreach (var arg in args)
        {
            if (Uri.TryCreate(arg, UriKind.Absolute, out var url) &&
                (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps))
                return url;
        }

        return null;
    }
}