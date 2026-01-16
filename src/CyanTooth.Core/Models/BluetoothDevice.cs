using CyanTooth.Platform.Helpers;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CyanTooth.Core.Models;

/// <summary>
/// Represents a Bluetooth device
/// </summary>
public partial class BluetoothDevice : ObservableObject
{
    /// <summary>
    /// Unique device identifier (WinRT Device ID)
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Bluetooth MAC address
    /// </summary>
    public ulong Address { get; init; }

    /// <summary>
    /// MAC address formatted as string (XX:XX:XX:XX:XX:XX)
    /// </summary>
    public string MacAddress => FormatMacAddress(Address);

    /// <summary>
    /// Device friendly name
    /// </summary>
    [ObservableProperty]
    private string _name = string.Empty;

    /// <summary>
    /// Device type (Classic or BLE)
    /// </summary>
    public BluetoothDeviceType DeviceType { get; init; }

    /// <summary>
    /// Device category (Headphones, Speaker, etc.)
    /// </summary>
    [ObservableProperty]
    private DeviceCategory _category = DeviceCategory.Other;

    /// <summary>
    /// Current connection status
    /// </summary>
    [ObservableProperty]
    private bool _isConnected;

    /// <summary>
    /// Whether the device is paired
    /// </summary>
    [ObservableProperty]
    private bool _isPaired;

    /// <summary>
    /// Battery level (0-100) or null if unavailable
    /// </summary>
    [ObservableProperty]
    private byte? _batteryLevel;

    /// <summary>
    /// Whether this device supports audio
    /// </summary>
    [ObservableProperty]
    private bool _isAudioDevice;

    /// <summary>
    /// PnP Instance ID for battery reading
    /// </summary>
    public string? InstanceId { get; set; }

    /// <summary>
    /// Container ID for correlating with audio endpoints
    /// </summary>
    public Guid? ContainerId { get; set; }

    /// <summary>
    /// Last seen timestamp
    /// </summary>
    [ObservableProperty]
    private DateTime _lastSeen = DateTime.Now;

    /// <summary>
    /// Formats a MAC address from ulong to string
    /// </summary>
    private static string FormatMacAddress(ulong address)
    {
        if (address == 0) return string.Empty;
        var bytes = BitConverter.GetBytes(address);
        return $"{bytes[5]:X2}:{bytes[4]:X2}:{bytes[3]:X2}:{bytes[2]:X2}:{bytes[1]:X2}:{bytes[0]:X2}";
    }
}

/// <summary>
/// Bluetooth device type
/// </summary>
public enum BluetoothDeviceType
{
    /// <summary>
    /// Classic Bluetooth (BR/EDR)
    /// </summary>
    Classic,

    /// <summary>
    /// Bluetooth Low Energy (BLE)
    /// </summary>
    LowEnergy,

    /// <summary>
    /// Dual-mode device (supports both Classic and BLE)
    /// </summary>
    DualMode
}

/// <summary>
/// Device category based on Class of Device
/// </summary>
public enum DeviceCategory
{
    Headphones,
    Speaker,
    Earbuds,
    Keyboard,
    Mouse,
    Gamepad,
    Phone,
    Computer,
    Watch,
    HealthDevice,
    Other
}
