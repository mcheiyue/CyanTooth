using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace CyanTooth.ViewModels;

/// <summary>
/// ViewModel for the detailed main window
/// </summary>
public partial class DetailViewModel : ObservableObject
{
    public MainViewModel DeviceList { get; }
    public SettingsViewModel Settings { get; }

    public DetailViewModel(MainViewModel mainViewModel, SettingsViewModel settingsViewModel)
    {
        DeviceList = mainViewModel;
        Settings = settingsViewModel;
        
        // Ensure data is loaded
        DeviceList.Initialize();
    }
}
