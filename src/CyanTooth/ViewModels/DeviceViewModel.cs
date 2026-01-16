using System;
using CommunityToolkit.Mvvm.ComponentModel;
using BluetoothManager.Core.Models;

namespace BluetoothManager.ViewModels;

/// <summary>
/// ViewModel for a single Bluetooth device
/// </summary>
public partial class DeviceViewModel : ObservableObject
{
    public string Id { get; }
    public string MacAddress { get; }
    public BluetoothDeviceType DeviceType { get; }
    
    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private DeviceCategory _category;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isPaired;

    [ObservableProperty]
    private byte? _batteryLevel;

    [ObservableProperty]
    private bool _isAudioDevice;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isOperating;

    [ObservableProperty]
    private DateTime _lastSeen;

    public string BatteryText => BatteryLevel.HasValue ? $"{BatteryLevel}%" : "—";

    public string StatusText => IsConnected ? "已连接" : "未连接";

    public string DeviceIcon => Category switch
    {
        DeviceCategory.Headphones => "\uE7F6",   // Headphone icon
        DeviceCategory.Earbuds => "\uE7F6",
        DeviceCategory.Speaker => "\uE7F5",      // Speaker icon
        DeviceCategory.Keyboard => "\uE765",     // Keyboard icon
        DeviceCategory.Mouse => "\uE962",        // Mouse icon
        DeviceCategory.Gamepad => "\uE7FC",      // Gamepad icon
        DeviceCategory.Phone => "\uE8EA",        // Phone icon
        DeviceCategory.Computer => "\uE7F8",     // Computer icon
        DeviceCategory.Watch => "\uE916",        // Watch icon
        _ => "\uE702"                            // Bluetooth icon
    };

    public string BatteryIcon => BatteryLevel switch
    {
        null => "\uEBA0",      // Unknown battery
        <= 10 => "\uEBA0",     // Battery 0
        <= 30 => "\uEBA1",     // Battery 1
        <= 50 => "\uEBA2",     // Battery 2
        <= 70 => "\uEBA4",     // Battery 3
        <= 90 => "\uEBA6",     // Battery 4
        _ => "\uEBAA"          // Battery full
    };

    public bool IsLowBattery => BatteryLevel.HasValue && BatteryLevel.Value <= 20;

    public bool HasBattery => BatteryLevel.HasValue;

    public DeviceViewModel(BluetoothDevice device)
    {
        Id = device.Id;
        MacAddress = device.MacAddress;
        DeviceType = device.DeviceType;
        _name = device.Name;
        _category = device.Category;
        _isConnected = device.IsConnected;
        _isPaired = device.IsPaired;
        _batteryLevel = device.BatteryLevel;
        _isAudioDevice = device.IsAudioDevice;
        _lastSeen = device.LastSeen;
    }

    partial void OnBatteryLevelChanged(byte? value)
    {
        OnPropertyChanged(nameof(BatteryText));
        OnPropertyChanged(nameof(BatteryIcon));
        OnPropertyChanged(nameof(IsLowBattery));
        OnPropertyChanged(nameof(HasBattery));
    }

    partial void OnIsConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(StatusText));
    }

    partial void OnCategoryChanged(DeviceCategory value)
    {
        OnPropertyChanged(nameof(DeviceIcon));
    }
}
