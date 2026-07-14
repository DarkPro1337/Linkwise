using CommunityToolkit.Mvvm.ComponentModel;
using Linkwise.Core.Models;

namespace Linkwise.Desktop.ViewModels;

public partial class BrowserTargetEditorViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string Id { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ExecutablePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ArgumentsText { get; set; } = string.Empty;

    public static BrowserTargetEditorViewModel FromModel(BrowserTarget target)
    {
        return new BrowserTargetEditorViewModel
        {
            Id = target.Id,
            Name = target.Name,
            ExecutablePath = target.ExecutablePath,
            ArgumentsText = string.Join(Environment.NewLine, target.Arguments)
        };
    }

    public BrowserTarget ToModel()
    {
        return new BrowserTarget
        {
            Id = Id.Trim(),
            Name = Name.Trim(),
            ExecutablePath = ExecutablePath.Trim(),
            Arguments = ArgumentsText
                .Split(Environment.NewLine, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .ToList()
        };
    }
}
