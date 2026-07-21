using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Linkwise.Core.Models;
using Linkwise.Desktop.ViewModels;

namespace Linkwise.Desktop.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += (_, args) =>
        {
            if (args.IsProgrammatic)
                return;

            args.Cancel = true;
            Hide();
        };
    }

    private void ToggleTheme(object? sender, RoutedEventArgs args)
    {
        if (Application.Current is not { } application)
            return;

        application.RequestedThemeVariant = application.ActualThemeVariant == ThemeVariant.Dark
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
    }

    private async void OpenBrowserImport(object? sender, RoutedEventArgs args)
    {
        if (DataContext is not MainWindowViewModel viewModel)
            return;

        var profiles = await viewModel.DiscoverBrowserProfilesAsync();
        var importViewModel = new BrowserProfileImportViewModel(profiles, viewModel.IsBrowserProfileImported);
        var dialog = new BrowserProfileImportWindow { DataContext = importViewModel };
        var selectedProfiles = await dialog.ShowDialog<IReadOnlyList<DiscoveredBrowserProfile>?>(this);

        if (selectedProfiles is { Count: > 0 })
            viewModel.ImportBrowserProfiles(selectedProfiles);
    }
}
