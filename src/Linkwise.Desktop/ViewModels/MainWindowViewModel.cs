using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty]
    private BrowserTargetEditorViewModel? _selectedTarget;

    [ObservableProperty]
    private RoutingRuleEditorViewModel? _selectedRule;

    [ObservableProperty]
    private string _fallbackTargetId = string.Empty;

    [ObservableProperty]
    private string _testUrl = "https://gitlab.company.local/project";

    [ObservableProperty]
    private string _status = "Loading configuration...";

    [ObservableProperty]
    private string _routePreview = string.Empty;

    public MainWindowViewModel()
        : this(
            new JsonFileLinkwiseConfigStore(LinkwiseConfigPaths.GetDefaultConfigPath()),
            new UrlRouteEngine(),
            new ProcessUrlLauncher(),
            new Linkwise.Core.BrowserProfiles.ChromeBrowserProfileDiscovery())
    {
    }

    public MainWindowViewModel(
        ILinkwiseConfigStore configStore,
        UrlRouteEngine routeEngine,
        IUrlLauncher launcher,
        IBrowserProfileDiscovery browserProfileDiscovery)
    {
        _configStore = configStore;
        _routeEngine = routeEngine;
        _launcher = launcher;
        _browserProfileDiscovery = browserProfileDiscovery;
        _ = LoadAsync();
    }

    public ObservableCollection<BrowserTargetEditorViewModel> BrowserTargets { get; } = [];

    public ObservableCollection<RoutingRuleEditorViewModel> Rules { get; } = [];

    public string ConfigPath => _configStore.FilePath;

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
        if (string.IsNullOrWhiteSpace(FallbackTargetId))
            FallbackTargetId = target.Id;

        SelectedTarget = target;
        Status = "Added browser target.";
    }

    [RelayCommand]
    private async Task ImportChromeProfilesAsync()
    {
        var profiles = await _browserProfileDiscovery.DiscoverProfilesAsync();
        if (profiles.Count == 0)
        {
            Status = "No Chrome profiles were found.";
            return;
        }

        var addedCount = 0;
        var updatedCount = 0;
        foreach (var profile in profiles)
        {
            var profileArgument = $"--profile-directory={profile.ProfileDirectory}";
            var existingTarget = BrowserTargets.FirstOrDefault(target =>
                target.ArgumentsText
                    .Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .Any(argument => string.Equals(argument, profileArgument, StringComparison.OrdinalIgnoreCase)));

            if (existingTarget is not null)
            {
                existingTarget.Name = $"Chrome {profile.DisplayName}";
                existingTarget.ExecutablePath = profile.ExecutablePath;
                existingTarget.ArgumentsText = profileArgument;
                updatedCount++;
                continue;
            }

            var target = new BrowserTargetEditorViewModel
            {
                Id = CreateUniqueId("chrome", BrowserTargets.Select(existingTarget => existingTarget.Id)),
                Name = $"Chrome {profile.DisplayName}",
                ExecutablePath = profile.ExecutablePath,
                ArgumentsText = profileArgument
            };

            BrowserTargets.Add(target);
            SelectedTarget = target;
            addedCount++;
        }

        if (string.IsNullOrWhiteSpace(FallbackTargetId))
            FallbackTargetId = BrowserTargets.FirstOrDefault()?.Id ?? string.Empty;

        EnsureTargetReferences();
        UpdateRoutePreview();
        Status = $"Imported Chrome profiles: {addedCount} added, {updatedCount} updated. Save to persist changes.";
    }

    [RelayCommand]
    private void RemoveSelectedTarget()
    {
        if (SelectedTarget is null)
            return;

        BrowserTargets.Remove(SelectedTarget);
        SelectedTarget = BrowserTargets.FirstOrDefault();
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
        Status = "Removed routing rule.";
        UpdateRoutePreview();
    }

    [RelayCommand]
    private void PreviewRoute()
    {
        UpdateRoutePreview();
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
