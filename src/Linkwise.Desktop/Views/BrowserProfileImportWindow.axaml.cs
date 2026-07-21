using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Linkwise.Desktop.ViewModels;

namespace Linkwise.Desktop.Views;

public partial class BrowserProfileImportWindow : Window
{
    public BrowserProfileImportWindow()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    private void ConfirmImport(object? sender, RoutedEventArgs args)
    {
        if (DataContext is BrowserProfileImportViewModel viewModel)
            Close(viewModel.GetSelectedProfiles());
    }

    private void CancelImport(object? sender, RoutedEventArgs args)
    {
        Close(null);
    }

    private void OnKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.Escape)
            Close(null);
    }
}
