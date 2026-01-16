using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using BluetoothManager.Core.Services;
using BluetoothManager.Views;

namespace BluetoothManager.ViewModels;

/// <summary>
/// ViewModel for the system tray icon
/// </summary>
public partial class TrayIconViewModel : ObservableObject
{
    private readonly BluetoothService _bluetoothService;
    private readonly ConfigService _configService;
    private FlyoutWindow? _flyoutWindow;
    private SettingsWindow? _settingsWindow;

    [ObservableProperty]
    private string _toolTipText = "CyanTooth";

    [ObservableProperty]
    private string _iconSource = "pack://application:,,,/Resources/Icons/tray.ico";

    public TrayIconViewModel(BluetoothService bluetoothService, ConfigService configService)
    {
        _bluetoothService = bluetoothService;
        _configService = configService;

        // Subscribe to connection changes to update tooltip
        _bluetoothService.DeviceConnectionChanged += OnDeviceConnectionChanged;
        _bluetoothService.DeviceBatteryChanged += OnDeviceBatteryChanged;
        
        UpdateToolTip();
    }

    private void UpdateToolTip()
    {
        var connectedDevices = _bluetoothService.GetConnectedDevices().ToList();
        if (connectedDevices.Count == 0)
        {
            ToolTipText = "CyanTooth\n未连接设备";
            return;
        }

        var lines = new List<string> { "CyanTooth" };
        foreach (var device in connectedDevices.Take(3))
        {
            var batteryText = device.BatteryLevel.HasValue ? $" ({device.BatteryLevel}%)" : "";
            lines.Add($"• {device.Name}{batteryText}");
        }

        if (connectedDevices.Count > 3)
        {
            lines.Add($"...以及另外 {connectedDevices.Count - 3} 个设备");
        }

        ToolTipText = string.Join("\n", lines);
    }

    private void OnDeviceConnectionChanged(object? sender, Core.Events.DeviceConnectionChangedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(UpdateToolTip);
    }

    private void OnDeviceBatteryChanged(object? sender, Core.Events.DeviceBatteryChangedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(UpdateToolTip);
    }

    [RelayCommand]
    private void ShowFlyout()
    {
        if (_flyoutWindow == null || !_flyoutWindow.IsLoaded)
        {
            _flyoutWindow = new FlyoutWindow();
        }

        if (_flyoutWindow.IsVisible)
        {
            _flyoutWindow.Hide();
        }
        else
        {
            PositionFlyoutWindow(_flyoutWindow);
            _flyoutWindow.Show();
            _flyoutWindow.Activate();
        }
    }

    [RelayCommand]
    private void ShowSettings()
    {
        if (_settingsWindow == null || !_settingsWindow.IsLoaded)
        {
            _settingsWindow = new SettingsWindow();
        }

        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    [RelayCommand]
    private void RefreshDevices()
    {
        _bluetoothService.RefreshDevices();
    }

    [RelayCommand]
    private void OpenBluetoothSettings()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "ms-settings:bluetooth",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors
        }
    }

    [RelayCommand]
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void PositionFlyoutWindow(Window window)
    {
        var workArea = SystemParameters.WorkArea;
        var taskbarPosition = GetTaskbarPosition();

        window.Width = 380;
        window.Height = 500;

        switch (taskbarPosition)
        {
            case TaskbarPosition.Bottom:
                window.Left = workArea.Right - window.Width - 10;
                window.Top = workArea.Bottom - window.Height - 10;
                break;
            case TaskbarPosition.Top:
                window.Left = workArea.Right - window.Width - 10;
                window.Top = workArea.Top + 10;
                break;
            case TaskbarPosition.Left:
                window.Left = workArea.Left + 10;
                window.Top = workArea.Bottom - window.Height - 10;
                break;
            case TaskbarPosition.Right:
                window.Left = workArea.Right - window.Width - 10;
                window.Top = workArea.Bottom - window.Height - 10;
                break;
        }
    }

    private static TaskbarPosition GetTaskbarPosition()
    {
        var workArea = SystemParameters.WorkArea;
        var screenArea = SystemParameters.PrimaryScreenHeight;

        if (workArea.Top > 0) return TaskbarPosition.Top;
        if (workArea.Left > 0) return TaskbarPosition.Left;
        if (workArea.Height < screenArea) return TaskbarPosition.Bottom;
        return TaskbarPosition.Right;
    }

    private enum TaskbarPosition
    {
        Bottom,
        Top,
        Left,
        Right
    }
}
