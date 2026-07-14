using System.Diagnostics;
using Linkwise.Core.Contracts;

namespace Linkwise.Platforms.Mac;

public sealed class MacDefaultHandlerRegistrar : IDefaultHandlerRegistrar
{
    private const string HelperFileName = "Linkwise.DefaultHandler";

    public bool IsSupported => OperatingSystem.IsMacOSVersionAtLeast(12);

    public async Task<DefaultHandlerRequestResult> RequestDefaultAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSupported)
            throw new PlatformNotSupportedException("Default-handler registration requires macOS 12 or later.");

        var executablePath = Environment.ProcessPath
            ?? throw new InvalidOperationException("The application executable path is unavailable.");
        var bundlePath = FindApplicationBundle(executablePath);
        var helperPath = Path.Combine(Path.GetDirectoryName(executablePath)!, HelperFileName);

        if (!File.Exists(helperPath))
        {
            throw new InvalidOperationException(
                $"The native helper '{HelperFileName}' was not found. Run Linkwise from the packaged .app bundle.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = helperPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add(bundlePath);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start the macOS default-handler helper.");
        var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;
        if (process.ExitCode == 0)
            return DefaultHandlerRequestResult.Changed;

        var error = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
        throw new InvalidOperationException(
            string.IsNullOrWhiteSpace(error)
                ? $"The macOS default-handler helper exited with code {process.ExitCode}."
                : error.Trim());
    }

    private static string FindApplicationBundle(string executablePath)
    {
        var macOsDirectory = Directory.GetParent(executablePath);
        var contentsDirectory = macOsDirectory?.Parent;
        var bundleDirectory = contentsDirectory?.Parent;

        if (!string.Equals(macOsDirectory?.Name, "MacOS", StringComparison.Ordinal) ||
            !string.Equals(contentsDirectory?.Name, "Contents", StringComparison.Ordinal) ||
            bundleDirectory is null ||
            !string.Equals(bundleDirectory.Extension, ".app", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Linkwise must be running from a packaged .app bundle to become the default URL handler.");
        }

        return bundleDirectory.FullName;
    }
}
