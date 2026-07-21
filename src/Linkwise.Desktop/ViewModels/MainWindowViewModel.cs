using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linkwise.Core.BrowserProfiles;
using Linkwise.Core.Configuration;
using Linkwise.Core.Contracts;
using Linkwise.Core.Launching;
using Linkwise.Core.Models;
using Linkwise.Core.RuleEngine;

namespace Linkwise.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILinkwiseConfigStore _configStore;
    private readonly UrlRouteEngine _routeEngine;
    private readonly IUrlLauncher _launcher;
    private readonly IBrowserProfileDiscovery _browserProfileDiscovery;
    private readonly IDefaultHandlerRegistrar _defaultHandlerRegistrar;

    [ObservableProperty]
    public partial BrowserTargetEditorViewModel? SelectedTarget { get; set; }

    [ObservableProperty]
    public partial RoutingRuleEditorViewModel? SelectedRule { get; set; }

    public bool HasTargets => BrowserTargets.Count > 0;

    public bool HasNoTargets => !HasTargets;

    public bool HasRules => Rules.Count > 0;

    public bool HasNoRules => !HasRules;

    public bool HasSelectedTarget => SelectedTarget is not null;

    public bool HasNoSelectedTarget => !HasSelectedTarget;

    public bool HasSelectedRule => SelectedRule is not null;

    public bool HasNoSelectedRule => !HasSelectedRule;

    [ObservableProperty]
    public partial string FallbackTargetId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TestUrl { get; set; } = "https://gitlab.company.local/project";

    [ObservableProperty]
    public partial string Status { get; set; } = "Loading configuration...";

    [ObservableProperty]
    private string _routePreview = string.Empty;

    public MainWindowViewModel()
        : this(
            new JsonFileLinkwiseConfigStore(LinkwiseConfigPaths.GetDefaultConfigPath()),
            new UrlRouteEngine(),
            new ProcessUrlLauncher(),
            new BrowserProfileDiscovery(),
            new UnsupportedDefaultHandlerRegistrar())
    {
    }

    public MainWindowViewModel(
        ILinkwiseConfigStore configStore,
        UrlRouteEngine routeEngine,
        IUrlLauncher launcher,
        IBrowserProfileDiscovery browserProfileDiscovery,
        IDefaultHandlerRegistrar defaultHandlerRegistrar)
    {
        _configStore = configStore;
        _routeEngine = routeEngine;
        _launcher = launcher;
        _browserProfileDiscovery = browserProfileDiscovery;
        _defaultHandlerRegistrar = defaultHandlerRegistrar;
        _ = LoadAsync();
    }

    public ObservableCollection<BrowserTargetEditorViewModel> BrowserTargets { get; } = [];

    public ObservableCollection<RoutingRuleEditorViewModel> Rules { get; } = [];

    public string ConfigPath => _configStore.FilePath;

    public bool IsDefaultHandlerRegistrationSupported => _defaultHandlerRegistrar.IsSupported;

    partial void OnSelectedTargetChanged(BrowserTargetEditorViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedTarget));
        OnPropertyChanged(nameof(HasNoSelectedTarget));
    }

    partial void OnSelectedRuleChanged(RoutingRuleEditorViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedRule));
        OnPropertyChanged(nameof(HasNoSelectedRule));
    }

    private void NotifyCollectionStateChanged()
    {
        OnPropertyChanged(nameof(HasTargets));
        OnPropertyChanged(nameof(HasNoTargets));
        OnPropertyChanged(nameof(HasRules));
        OnPropertyChanged(nameof(HasNoRules));
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        var config = await _configStore.LoadAsync();

        BrowserTargets.Clear();
        var targetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var target in config.BrowserTargets)
        {
            var targetEditor = BrowserTargetEditorViewModel.FromModel(target);
            EnsureEditorId(targetEditor, "target", targetIds);
            BrowserTargets.Add(targetEditor);
        }

        Rules.Clear();
        var ruleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var rule in config.Rules.OrderByDescending(rule => rule.Priority))
        {
            var ruleEditor = RoutingRuleEditorViewModel.FromModel(rule);
            EnsureEditorId(ruleEditor, "rule", ruleIds);
            Rules.Add(ruleEditor);
        }

        FallbackTargetId = config.FallbackTargetId ?? string.Empty;
        EnsureTargetReferences();
        SelectedTarget = BrowserTargets.FirstOrDefault();
        SelectedRule = Rules.FirstOrDefault();
        NotifyCollectionStateChanged();
        Status = $"Loaded {BrowserTargets.Count} targets and {Rules.Count} rules.";
        UpdateRoutePreview();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _configStore.SaveAsync(CreateConfigFromEditors());
        Status = $"Saved configuration to {ConfigPath}.";
        UpdateRoutePreview();
    }

    [RelayCommand]
    private void AddTarget()
    {
        var nextNumber = BrowserTargets.Count + 1;
        var target = new BrowserTargetEditorViewModel
        {
            Id = CreateUniqueId("target", BrowserTargets.Select(existingTarget => existingTarget.Id)),
            Name = $"Target {nextNumber}"
        };

        BrowserTargets.Add(target);
        NotifyCollectionStateChanged();
        if (string.IsNullOrWhiteSpace(FallbackTargetId))
            FallbackTargetId = target.Id;

        SelectedTarget = target;
        Status = "Added browser target.";
    }

    public async Task<IReadOnlyList<DiscoveredBrowserProfile>> DiscoverBrowserProfilesAsync()
    {
        Status = "Looking for installed browsers and profiles...";
        try
        {
            var profiles = await _browserProfileDiscovery.DiscoverProfilesAsync();
            Status = profiles.Count == 0
                ? "No supported browser profiles were found."
                : $"Found {profiles.Count} browser profiles.";

            return profiles;
        }
        catch (Exception exception)
        {
            Status = $"Could not scan browser profiles: {exception.Message}";
            return [];
        }
    }

    public bool IsBrowserProfileImported(DiscoveredBrowserProfile profile)
    {
        return FindTargetForProfile(profile) is not null;
    }

    public void ImportBrowserProfiles(IReadOnlyList<DiscoveredBrowserProfile> profiles)
    {
        var addedCount = 0;
        var updatedCount = 0;

        foreach (var profile in profiles)
        {
            var existingTarget = FindTargetForProfile(profile);
            var argumentsText = string.Join(Environment.NewLine, profile.LaunchArguments);

            if (existingTarget is not null)
            {
                existingTarget.Name = $"{profile.BrowserName} — {profile.DisplayName}";
                existingTarget.ExecutablePath = profile.ExecutablePath;
                existingTarget.ArgumentsText = argumentsText;
                updatedCount++;
                continue;
            }

            var target = new BrowserTargetEditorViewModel
            {
                Id = CreateUniqueId(profile.BrowserId, BrowserTargets.Select(browserTarget => browserTarget.Id)),
                Name = $"{profile.BrowserName} — {profile.DisplayName}",
                ExecutablePath = profile.ExecutablePath,
                ArgumentsText = argumentsText
            };

            BrowserTargets.Add(target);
            SelectedTarget = target;
            addedCount++;
        }

        if (string.IsNullOrWhiteSpace(FallbackTargetId))
            FallbackTargetId = BrowserTargets.FirstOrDefault()?.Id ?? string.Empty;

        EnsureTargetReferences();
        NotifyCollectionStateChanged();
        UpdateRoutePreview();
        Status = $"Imported browser profiles: {addedCount} added, {updatedCount} updated. Save to persist changes.";
    }

    private BrowserTargetEditorViewModel? FindTargetForProfile(DiscoveredBrowserProfile profile)
    {
        var pathComparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return BrowserTargets.FirstOrDefault(target =>
        {
            if (!string.Equals(target.ExecutablePath.Trim(), profile.ExecutablePath, pathComparison))
                return false;

            var targetArguments = target.ArgumentsText.Split(Environment.NewLine,
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return targetArguments.SequenceEqual(profile.LaunchArguments, StringComparer.Ordinal);
        });
    }

    [RelayCommand]
    private void RemoveSelectedTarget()
    {
        if (SelectedTarget is null)
            return;

        BrowserTargets.Remove(SelectedTarget);
        SelectedTarget = BrowserTargets.FirstOrDefault();
        NotifyCollectionStateChanged();
        EnsureTargetReferences();
        Status = "Removed browser target.";
        UpdateRoutePreview();
    }

    [RelayCommand]
    private void AddRule()
    {
        var nextNumber = Rules.Count + 1;
        var rule = new RoutingRuleEditorViewModel
        {
            Id = CreateUniqueId("rule", Rules.Select(existingRule => existingRule.Id)),
            Name = $"Rule {nextNumber}",
            Priority = 10,
            MatchKind = RuleMatchKind.DomainSuffix,
            TargetId = BrowserTargets.FirstOrDefault()?.Id ?? string.Empty
        };

        Rules.Add(rule);
        SelectedRule = rule;
        NotifyCollectionStateChanged();
        Status = "Added routing rule.";
        UpdateRoutePreview();
    }

    [RelayCommand]
    private void RemoveSelectedRule()
    {
        if (SelectedRule is null)
            return;

        Rules.Remove(SelectedRule);
        SelectedRule = Rules.FirstOrDefault();
        NotifyCollectionStateChanged();
        Status = "Removed routing rule.";
        UpdateRoutePreview();
    }

    [RelayCommand]
    private void PreviewRoute()
    {
        UpdateRoutePreview();
    }

    [RelayCommand]
    private async Task RequestDefaultHandlerAsync()
    {
        try
        {
            var config = CreateConfigFromEditors();
            var validationError = ValidateDefaultHandlerConfig(config);
            if (validationError is not null)
            {
                Status = $"Cannot enable web-link handling: {validationError}";
                return;
            }

            await _configStore.SaveAsync(config);
            Status = "Requesting the default web-link handler change...";
            var result = await _defaultHandlerRegistrar.RequestDefaultAsync();
            Status = result switch
            {
                DefaultHandlerRequestResult.Changed => "Linkwise is now the default handler for HTTP and HTTPS URLs.",
                DefaultHandlerRequestResult.UserActionRequired => "Select Linkwise for HTTP and HTTPS in Windows Default Apps.",
                _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
            };
        }
        catch (Exception exception)
        {
            Status = $"Could not set Linkwise as the default handler: {exception.Message}";
        }
    }

    private static string? ValidateDefaultHandlerConfig(LinkwiseConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.FallbackTargetId))
            return "select a fallback browser target first.";

        var targetsById = config.BrowserTargets.ToDictionary(target => target.Id, StringComparer.OrdinalIgnoreCase);
        var requiredTargetIds = config.Rules
            .Where(rule => rule.Enabled)
            .Select(rule => rule.TargetId)
            .Append(config.FallbackTargetId)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var targetId in requiredTargetIds)
        {
            if (!targetsById.TryGetValue(targetId, out var target))
                return $"target '{targetId}' does not exist.";

            if (string.IsNullOrWhiteSpace(target.ExecutablePath) || !File.Exists(target.ExecutablePath))
                return $"browser executable for '{target.Name}' does not exist.";

            if (string.Equals(
                    Path.GetFullPath(target.ExecutablePath),
                    Environment.ProcessPath,
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                return $"'{target.Name}' points back to Linkwise and would create a routing loop.";
            }
        }

        return null;
    }

    [RelayCommand]
    private async Task OpenTestUrlAsync()
    {
        if (!Uri.TryCreate(TestUrl, UriKind.Absolute, out var url))
        {
            RoutePreview = "Test URL is not a valid absolute URL.";
            return;
        }

        var match = _routeEngine.Match(CreateConfigFromEditors(), url);
        if (!match.IsMatch || match.Target is null)
        {
            RoutePreview = match.Error ?? "No target selected.";
            return;
        }

        await _launcher.LaunchAsync(match.Target, url);
        RoutePreview = DescribeMatch(match);
    }

    partial void OnTestUrlChanged(string value)
    {
        UpdateRoutePreview();
    }

    private void UpdateRoutePreview()
    {
        if (!Uri.TryCreate(TestUrl, UriKind.Absolute, out var url))
        {
            RoutePreview = "Enter a valid absolute HTTP/HTTPS URL.";
            return;
        }

        var match = _routeEngine.Match(CreateConfigFromEditors(), url);
        RoutePreview = DescribeMatch(match);
    }

    private LinkwiseConfig CreateConfigFromEditors()
    {
        EnsureTargetReferences();
        return new LinkwiseConfig
        {
            BrowserTargets = BrowserTargets.Select(target => target.ToModel()).ToList(),
            Rules = Rules.Select(rule => rule.ToModel()).ToList(),
            FallbackTargetId = string.IsNullOrWhiteSpace(FallbackTargetId) ? null : FallbackTargetId.Trim()
        };
    }

    private static string DescribeMatch(RouteMatchResult match)
    {
        if (!match.IsMatch || match.Target is null)
            return match.Error ?? "No route found.";

        var ruleName = match.Rule?.Name ?? "Fallback";
        return $"{match.Url.Host} -> {match.Target.Name} ({ruleName})";
    }

    private static void EnsureEditorId(BrowserTargetEditorViewModel target, string prefix, HashSet<string> existingIds)
    {
        if (!string.IsNullOrWhiteSpace(target.Id) && existingIds.Add(target.Id))
            return;

        target.Id = CreateUniqueId(prefix, existingIds);
        existingIds.Add(target.Id);
    }

    private static void EnsureEditorId(RoutingRuleEditorViewModel rule, string prefix, HashSet<string> existingIds)
    {
        if (!string.IsNullOrWhiteSpace(rule.Id) && existingIds.Add(rule.Id))
            return;

        rule.Id = CreateUniqueId(prefix, existingIds);
        existingIds.Add(rule.Id);
    }

    private static string CreateUniqueId(string prefix, IEnumerable<string> existingIds)
    {
        var existingIdSet = existingIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        while (true)
        {
            var token = Guid.NewGuid().ToString("N")[..8];
            var id = $"{prefix}-{token}";
            if (!existingIdSet.Contains(id))
                return id;
        }
    }

    private void EnsureTargetReferences()
    {
        var firstTargetId = BrowserTargets.FirstOrDefault()?.Id ?? string.Empty;
        var targetIds = BrowserTargets
            .Select(target => target.Id)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (targetIds.Count == 0)
        {
            FallbackTargetId = string.Empty;
            foreach (var rule in Rules)
                rule.TargetId = string.Empty;

            return;
        }

        if (string.IsNullOrWhiteSpace(FallbackTargetId) || !targetIds.Contains(FallbackTargetId))
            FallbackTargetId = firstTargetId;

        foreach (var rule in Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.TargetId) || !targetIds.Contains(rule.TargetId))
                rule.TargetId = firstTargetId;
        }
    }
}
