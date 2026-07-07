using CommunityToolkit.Mvvm.Input;
using Linkwise.Desktop.Services;

namespace Linkwise.Desktop.ViewModels;

public partial class AppViewModel(IApplicationShell shell) : ViewModelBase
{
    [RelayCommand]
    private void ShowWindow()
    {
        shell.ShowMainWindow();
    }

    [RelayCommand]
    private void Quit()
    {
        shell.Quit();
    }
}
