using CommunityToolkit.Mvvm.ComponentModel;
using Linkwise.Core.Models;

namespace Linkwise.Desktop.ViewModels;

public partial class BrowserTargetEditorViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _executablePath = string.Empty;

    [ObservableProperty]
    private string _argumentsText = string.Empty;

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
