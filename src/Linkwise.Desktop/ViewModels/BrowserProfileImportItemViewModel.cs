using CommunityToolkit.Mvvm.ComponentModel;
using Linkwise.Core.Models;

namespace Linkwise.Desktop.ViewModels;

public partial class BrowserProfileImportItemViewModel : ViewModelBase
{
    public BrowserProfileImportItemViewModel(
        DiscoveredBrowserProfile profile,
        bool isAlreadyImported,
        bool showBrowserHeader)
    {
        Profile = profile;
        IsAlreadyImported = isAlreadyImported;
        ShowBrowserHeader = showBrowserHeader;
        IsSelected = !isAlreadyImported;
    }

    public DiscoveredBrowserProfile Profile { get; }

    public string BrowserName => Profile.BrowserName;

    public string ProfileName => Profile.DisplayName;

    public string ProfileLocation => Profile.ProfileDirectory;

    public string BrowserInitials => Profile.BrowserId switch
    {
        "chrome" => "C",
        "firefox" => "F",
        "brave" => "B",
        "vivaldi" => "V",
        "opera" => "O",
        "opera-gx" => "GX",
        "chromium" => "Cr",
        "yandex" => "Y",
        _ => Profile.BrowserName[..1].ToUpperInvariant()
    };

    public bool IsAlreadyImported { get; }

    public bool ShowBrowserHeader { get; }

    public string ImportState => IsAlreadyImported ? "Already added · select to update" : "Ready to import";

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
