using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BluetoothManager.Core.Models;
using BluetoothManager.Core.Services;

namespace BluetoothManager.ViewModels;

/// <summary>
/// ViewModel for the settings window
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ConfigService _configService;

    [ObservableProperty]
    private bool _startMinimized;

    [ObservableProperty]
    private bool _runAtStartup;

    [ObservableProperty]
    private int _batteryPollInterval;

    [ObservableProperty]
    private int _lowBatteryThreshold;

    [ObservableProperty]
    private bool _showBatteryInTray;

    [ObservableProperty]
    private bool _showConnectionNotifications;

    [ObservableProperty]
    private bool _showLowBatteryNotifications;

    [ObservableProperty]
    private AppTheme _selectedTheme;

    public AppTheme[] AvailableThemes => [AppTheme.System, AppTheme.Light, AppTheme.Dark];

    public int[] PollIntervalOptions => [30, 60, 120, 300, 600];

    public int[] BatteryThresholdOptions => [10, 15, 20, 25, 30];

    public SettingsViewModel(ConfigService configService)
    {
        _configService = configService;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = _configService.Settings;
        StartMinimized = settings.StartMinimized;
        RunAtStartup = settings.RunAtStartup;
        BatteryPollInterval = settings.BatteryPollIntervalSeconds;
        LowBatteryThreshold = settings.LowBatteryThreshold;
        ShowBatteryInTray = settings.ShowBatteryInTray;
        ShowConnectionNotifications = settings.ShowConnectionNotifications;
        ShowLowBatteryNotifications = settings.ShowLowBatteryNotifications;
        SelectedTheme = settings.Theme;
    }

    [RelayCommand]
    private void Save()
    {
        _configService.UpdateSettings(settings =>
        {
            settings.StartMinimized = StartMinimized;
            settings.RunAtStartup = RunAtStartup;
            settings.BatteryPollIntervalSeconds = BatteryPollInterval;
            settings.LowBatteryThreshold = LowBatteryThreshold;
            settings.ShowBatteryInTray = ShowBatteryInTray;
            settings.ShowConnectionNotifications = ShowConnectionNotifications;
            settings.ShowLowBatteryNotifications = ShowLowBatteryNotifications;
            settings.Theme = SelectedTheme;
        });

        // Update startup registration
        _configService.IsRunAtStartupEnabled = RunAtStartup;
    }

    [RelayCommand]
    private void Reset()
    {
        LoadSettings();
    }

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        // Apply theme immediately
        ApplyTheme(value);
    }

    private static void ApplyTheme(AppTheme theme)
    {
        var targetTheme = theme switch
        {
            AppTheme.Light => Wpf.Ui.Appearance.ApplicationTheme.Light,
            AppTheme.Dark => Wpf.Ui.Appearance.ApplicationTheme.Dark,
            _ => Wpf.Ui.Appearance.ApplicationTheme.Unknown
        };

        if (targetTheme != Wpf.Ui.Appearance.ApplicationTheme.Unknown)
        {
            Wpf.Ui.Appearance.ApplicationThemeManager.Apply(targetTheme);
        }
    }
}
