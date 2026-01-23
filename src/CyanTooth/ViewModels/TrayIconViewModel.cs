using CyanTooth.Platform.Helpers;


using System;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using CyanTooth.Core.Services;
using CyanTooth.Views;


namespace CyanTooth.ViewModels;

/// <summary>
/// ViewModel for the system tray icon
/// </summary>
public partial class TrayIconViewModel : ObservableObject
{
    private readonly BluetoothService _bluetoothService;
    private readonly ConfigService _configService;

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
}
