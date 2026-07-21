using CommunityToolkit.Mvvm.ComponentModel;
using Linkwise.Core.Models;

namespace Linkwise.Desktop.ViewModels;

public partial class RoutingRuleEditorViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Id { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool Enabled { get; set; } = true;

    [ObservableProperty]
    public partial int Priority { get; set; }

    [ObservableProperty]
    public partial RuleMatchKind? MatchKind { get; set; } = RuleMatchKind.ExactHost;

    [ObservableProperty]
    public partial string Pattern { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string TargetId { get; set; } = string.Empty;
    public IReadOnlyList<RuleMatchKind> MatchKinds { get; } = Enum.GetValues<RuleMatchKind>();

    public static RoutingRuleEditorViewModel FromModel(RoutingRule rule)
    {
        return new RoutingRuleEditorViewModel
        {
            Id = rule.Id,
            Name = rule.Name,
            Enabled = rule.Enabled,
            Priority = rule.Priority,
            MatchKind = rule.MatchKind,
            Pattern = rule.Pattern,
            TargetId = rule.TargetId
        };
    }

    public RoutingRule ToModel()
    {
        return new RoutingRule
        {
            Id = Id.Trim(),
            Name = Name.Trim(),
            Enabled = Enabled,
            Priority = Priority,
            MatchKind = MatchKind ?? RuleMatchKind.ExactHost,
            Pattern = Pattern.Trim(),
            TargetId = TargetId.Trim()
        };
    }
}
