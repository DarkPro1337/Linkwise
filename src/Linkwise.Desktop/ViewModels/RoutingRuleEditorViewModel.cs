using CommunityToolkit.Mvvm.ComponentModel;
using Linkwise.Core.Models;

namespace Linkwise.Desktop.ViewModels;

public partial class RoutingRuleEditorViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _enabled = true;

    [ObservableProperty]
    private int _priority;

    [ObservableProperty]
    private RuleMatchKind _matchKind = RuleMatchKind.ExactHost;

    [ObservableProperty]
    private string _pattern = string.Empty;

    [ObservableProperty]
    private string _targetId = string.Empty;

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
            MatchKind = MatchKind,
            Pattern = Pattern.Trim(),
            TargetId = TargetId.Trim()
        };
    }
}
