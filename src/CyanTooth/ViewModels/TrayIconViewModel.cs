using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CyanTooth.Core.Services;
using CyanTooth.Platform.Helpers;

namespace CyanTooth.ViewModels;

/// <summary>
/// ViewModel for the system tray icon
/// </summary>
public partial class TrayIconViewModel : ObservableObject
{
    private readonly BluetoothService _bluetoothService;
    private readonly ConfigService _configService;
    private readonly ImageSource _defaultIcon;

    [ObservableProperty]
    private string _toolTipText = "CyanTooth";

    [ObservableProperty]
    private ImageSource _trayIconSource;

    public TrayIconViewModel(BluetoothService bluetoothService, ConfigService configService)
    {
        _bluetoothService = bluetoothService;
        _configService = configService;

        // Load default icon
        try 
        {
            _defaultIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/Icons/tray.ico"));
        }
        catch (Exception ex)
        {
            DebugLogger.LogError("无法加载默认托盘图标", ex);
            // Fallback to avoid crash, though binding might fail silently
            _defaultIcon = null!; 
        }
        _trayIconSource = _defaultIcon;

        // Subscribe to events
        _bluetoothService.DeviceConnectionChanged += OnDeviceConnectionChanged;
        _bluetoothService.DeviceBatteryChanged += OnDeviceBatteryChanged;
        _configService.SettingsChanged += OnSettingsChanged;
        
        UpdateToolTip();
        UpdateTrayIcon();
    }

    private void UpdateToolTip()
    {
        var connectedDevices = _bluetoothService.GetConnectedDevices().ToList();
        if (connectedDevices.Count == 0)
        {
            ToolTipText = "CyanTooth\n未连接设备";
            return;
        }

        bool showBattery = _configService.Settings.ShowBatteryInTray;
        var lines = new List<string> { "CyanTooth" };
        foreach (var device in connectedDevices.Take(3))
        {
            var batteryText = (showBattery && device.BatteryLevel.HasValue) ? $" ({device.BatteryLevel}%)" : "";
            lines.Add($"• {device.Name}{batteryText}");
        }

        if (connectedDevices.Count > 3)
        {
            lines.Add($"...以及另外 {connectedDevices.Count - 3} 个设备");
        }

        ToolTipText = string.Join("\n", lines);
    }

    private void UpdateTrayIcon()
    {
        // Revert to default icon (user preferred behavior)
        if (!ReferenceEquals(TrayIconSource, _defaultIcon)) 
        {
            TrayIconSource = _defaultIcon;
        }
    }

    private void OnDeviceConnectionChanged(object? sender, Core.Events.DeviceConnectionChangedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => 
        {
            UpdateToolTip();
        });
    }

    private void OnDeviceBatteryChanged(object? sender, Core.Events.DeviceBatteryChangedEventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => 
        {
            UpdateToolTip();
        });
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() => 
        {
            UpdateToolTip();
        });
    }
}