using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Linkwise.Core.Models;

namespace Linkwise.Desktop.ViewModels;

public partial class BrowserProfileImportViewModel : ViewModelBase
{
    public BrowserProfileImportViewModel(
        IReadOnlyList<DiscoveredBrowserProfile> profiles,
        Func<DiscoveredBrowserProfile, bool> isImported)
    {
        string? previousBrowserId = null;
        foreach (var profile in profiles)
        {
            var item = new BrowserProfileImportItemViewModel(
                profile,
                isImported(profile),
                !string.Equals(previousBrowserId, profile.BrowserId, StringComparison.Ordinal));

            item.PropertyChanged += OnItemPropertyChanged;
            Profiles.Add(item);
            previousBrowserId = profile.BrowserId;
        }
    }

    public ObservableCollection<BrowserProfileImportItemViewModel> Profiles { get; } = [];

    public bool HasProfiles => Profiles.Count > 0;

    public bool HasNoProfiles => !HasProfiles;

    public int SelectedCount => Profiles.Count(item => item.IsSelected);

    public bool CanImport => SelectedCount > 0;

    public string SelectionSummary => SelectedCount switch
    {
        0 => "No profiles selected",
        1 => "1 profile selected",
        _ => $"{SelectedCount} profiles selected"
    };

    public IReadOnlyList<DiscoveredBrowserProfile> GetSelectedProfiles()
    {
        return Profiles.Where(item => item.IsSelected).Select(item => item.Profile).ToList();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var item in Profiles)
            item.IsSelected = true;
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var item in Profiles)
            item.IsSelected = false;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(BrowserProfileImportItemViewModel.IsSelected))
            return;

        OnPropertyChanged(nameof(SelectedCount));
        OnPropertyChanged(nameof(CanImport));
        OnPropertyChanged(nameof(SelectionSummary));
    }
}
