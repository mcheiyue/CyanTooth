using CyanTooth.Platform.Helpers;
namespace CyanTooth.Core.Events;

/// <summary>
/// Event args for device connection state changes
/// </summary>
public class DeviceConnectionChangedEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public string? DeviceName { get; init; }
    public bool IsConnected { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

/// <summary>
/// Event args for battery level changes
/// </summary>
public class DeviceBatteryChangedEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public string? DeviceName { get; init; }
    public byte? OldBatteryLevel { get; init; }
    public byte? NewBatteryLevel { get; init; }
    public bool IsLowBattery => NewBatteryLevel.HasValue && NewBatteryLevel.Value <= 20;
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

/// <summary>
/// Event args for device discovery
/// </summary>
public class DeviceDiscoveredEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public string? DeviceName { get; init; }
    public ulong Address { get; init; }
    public bool IsConnected { get; init; }
    public bool IsPaired { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}

/// <summary>
/// Event args for device removal
/// </summary>
public class DeviceRemovedEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public string? DeviceName { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.Now;
}
