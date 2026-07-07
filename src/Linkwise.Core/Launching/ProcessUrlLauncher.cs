using System.Diagnostics;
using Linkwise.Core.Contracts;
using Linkwise.Core.Models;

namespace Linkwise.Core.Launching;

public sealed class ProcessUrlLauncher : IUrlLauncher
{
    public Task LaunchAsync(BrowserTarget target, Uri url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(target.ExecutablePath))
            throw new InvalidOperationException($"Target '{target.Name}' has no executable path.");

        var startInfo = new ProcessStartInfo
        {
            FileName = target.ExecutablePath,
            UseShellExecute = false
        };

        foreach (var argument in target.Arguments.Where(argument => !string.IsNullOrWhiteSpace(argument)))
        {
            startInfo.ArgumentList.Add(argument);
        }

        startInfo.ArgumentList.Add(url.AbsoluteUri);

        Process.Start(startInfo);
        return Task.CompletedTask;
    }
}
