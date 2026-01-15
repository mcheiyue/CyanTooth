using System.Collections.Concurrent;
using BluetoothManager.Core.Events;
using BluetoothManager.Core.Models;
using BluetoothManager.Platform.Audio;
using BluetoothManager.Platform.Bluetooth;

namespace BluetoothManager.Core.Services;

/// <summary>
/// Main service for managing Bluetooth devices
/// </summary>
public class BluetoothService : IDisposable
{
    private readonly DeviceDiscoverer _discoverer;
    private readonly BleBatteryReader _bleBatteryReader;
    private readonly BtcBatteryReader _btcBatteryReader;
    private readonly AudioEndpointEnumerator _audioEnumerator;
    private readonly BluetoothAudioConnector _audioConnector;
    private readonly ConcurrentDictionary<string, BluetoothDevice> _devices = new();
    private readonly Timer _batteryPollTimer;
    private bool _isDisposed;

    public event EventHandler<DeviceConnectionChangedEventArgs>? DeviceConnectionChanged;
    public event EventHandler<DeviceBatteryChangedEventArgs>? DeviceBatteryChanged;
    public event EventHandler<Events.DeviceDiscoveredEventArgs>? DeviceDiscovered;
    public event EventHandler<DeviceRemovedEventArgs>? DeviceRemoved;
    public event EventHandler? DevicesRefreshed;

    public IReadOnlyDictionary<string, BluetoothDevice> Devices => _devices;

    public BluetoothService()
    {
        _discoverer = new DeviceDiscoverer();
        _bleBatteryReader = new BleBatteryReader();
        _btcBatteryReader = new BtcBatteryReader();
        _audioEnumerator = new AudioEndpointEnumerator();
        _audioConnector = new BluetoothAudioConnector();

        // Subscribe to platform events
        _discoverer.DeviceAdded += OnDeviceAdded;
        _discoverer.DeviceUpdated += OnDeviceUpdated;
        _discoverer.DeviceRemoved += OnDeviceRemoved;
        _bleBatteryReader.BatteryLevelChanged += OnBleBatteryChanged;

        // Setup battery polling timer (60 seconds)
        _batteryPollTimer = new Timer(PollBatteryLevels, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Starts device discovery and monitoring
    /// </summary>
    public void Start()
    {
        _discoverer.StartDiscovery();
        _batteryPollTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(60));
    }

    /// <summary>
    /// Stops device discovery and monitoring
    /// </summary>
    public void Stop()
    {
        _discoverer.StopDiscovery();
        _batteryPollTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Refreshes the device list
    /// </summary>
    public void RefreshDevices()
    {
        _discoverer.StopDiscovery();
        _devices.Clear();
        _discoverer.StartDiscovery();
    }

    /// <summary>
    /// Gets a device by ID
    /// </summary>
    public BluetoothDevice? GetDevice(string deviceId)
    {
        return _devices.TryGetValue(deviceId, out var device) ? device : null;
    }

    /// <summary>
    /// Gets all connected devices
    /// </summary>
    public IEnumerable<BluetoothDevice> GetConnectedDevices()
    {
        return _devices.Values.Where(d => d.IsConnected);
    }

    /// <summary>
    /// Gets all audio devices
    /// </summary>
    public IEnumerable<BluetoothDevice> GetAudioDevices()
    {
        return _devices.Values.Where(d => d.IsAudioDevice);
    }

    /// <summary>
    /// Connects a Bluetooth audio device
    /// </summary>
    public async Task<bool> ConnectAsync(string deviceId)
    {
        Helpers.DebugLogger.Log($" ConnectAsync: deviceId={deviceId}");
        
        if (!_devices.TryGetValue(deviceId, out var device))
        {
            Helpers.DebugLogger.Log($" ConnectAsync: device not found in _devices");
            return false;
        }

        Helpers.DebugLogger.Log($" ConnectAsync: device={device.Name}, Address={device.Address:X12}");

        // Method 1: Try KsControl (primary method, fast and reliable)
        var endpoints = _audioEnumerator.FindEndpointsByMacAddress(device.Address, device.Name);
        Helpers.DebugLogger.Log($" ConnectAsync: trying KsControl, found {endpoints.Count()} endpoints by MAC/Name");
        
        foreach (var endpoint in endpoints)
        {
            Helpers.DebugLogger.Log($" ConnectAsync: trying endpoint DeviceId={endpoint.DeviceId}, HasKsControl={endpoint.KsControl != null}");
            if (_audioConnector.Connect(endpoint))
            {
                Helpers.DebugLogger.Log($" ConnectAsync: SUCCESS via KsControl");
                return true;
            }
        }

        // Method 2: Try using the container ID
        if (device.ContainerId.HasValue)
        {
            Helpers.DebugLogger.Log($" ConnectAsync: trying ContainerId={device.ContainerId.Value}");
            var endpoint = _audioEnumerator.FindEndpointByContainerId(device.ContainerId.Value);
            if (endpoint != null)
            {
                Helpers.DebugLogger.Log($" ConnectAsync: found endpoint by ContainerId, HasKsControl={endpoint.KsControl != null}");
                return _audioConnector.Connect(endpoint);
            }
        }

        Helpers.DebugLogger.Log($" ConnectAsync: FAILED - no valid endpoint found");
        return false;
    }

    /// <summary>
    /// Disconnects a Bluetooth audio device
    /// </summary>
    public async Task<bool> DisconnectAsync(string deviceId)
    {
        Helpers.DebugLogger.Log($" DisconnectAsync: deviceId={deviceId}");
        
        if (!_devices.TryGetValue(deviceId, out var device))
        {
            Helpers.DebugLogger.Log($" DisconnectAsync: device not found");
            return false;
        }

        Helpers.DebugLogger.Log($" DisconnectAsync: device={device.Name}, Address={device.Address:X12}");

        // Method 1: Try KsControl on ALL endpoints (like ToothTray does)
        // A Bluetooth device typically has multiple audio endpoints (A2DP, HFP, etc.)
        // We must disconnect ALL of them to fully disconnect the device.
        var endpoints = _audioEnumerator.FindEndpointsByMacAddress(device.Address, device.Name).ToList();
        Helpers.DebugLogger.Log($" DisconnectAsync: trying KsControl, found {endpoints.Count} endpoints");
        
        bool anySuccess = false;
        foreach (var endpoint in endpoints)
        {
            Helpers.DebugLogger.Log($" DisconnectAsync: trying endpoint DeviceId={endpoint.DeviceId}, HasKsControl={endpoint.KsControl != null}");
            if (_audioConnector.Disconnect(endpoint))
            {
                Helpers.DebugLogger.Log($" DisconnectAsync: endpoint disconnected successfully");
                anySuccess = true;
            }
            // Continue to disconnect ALL endpoints, don't return early
        }

        // Method 2: Also try using the container ID if we haven't succeeded yet
        if (!anySuccess && device.ContainerId.HasValue)
        {
            Helpers.DebugLogger.Log($" DisconnectAsync: trying ContainerId={device.ContainerId.Value}");
            var endpoint = _audioEnumerator.FindEndpointByContainerId(device.ContainerId.Value);
            if (endpoint != null)
            {
                if (_audioConnector.Disconnect(endpoint))
                {
                    anySuccess = true;
                }
            }
        }

        Helpers.DebugLogger.Log($" DisconnectAsync: {(anySuccess ? "SUCCESS" : "FAILED")}");
        return anySuccess;
    }

    /// <summary>
    /// Reads the battery level for a device
    /// </summary>
    public async Task<byte?> ReadBatteryLevelAsync(string deviceId)
    {
        Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: deviceId={deviceId}");
        
        if (!_devices.TryGetValue(deviceId, out var device))
        {
            Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: device not found");
            return null;
        }

        Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: device={device.Name}, Type={device.DeviceType}, InstanceId={device.InstanceId ?? "null"}, Address={device.Address:X12}");

        byte? batteryLevel = null;

        // Try BLE GATT first
        if (device.DeviceType is BluetoothDeviceType.LowEnergy or BluetoothDeviceType.DualMode)
        {
            Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: trying BLE GATT");
            var bleDevice = await DeviceDiscoverer.GetBluetoothLeDeviceAsync(deviceId);
            if (bleDevice != null)
            {
                batteryLevel = await _bleBatteryReader.ReadBatteryLevelAsync(bleDevice);
                Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: BLE result={batteryLevel?.ToString() ?? "null"}");
            }
            else
            {
                Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: BLE device is null");
            }
        }

        // Try Classic Bluetooth PnP
        if (!batteryLevel.HasValue && device.InstanceId != null)
        {
            Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: trying BTC with InstanceId={device.InstanceId}");
            batteryLevel = _btcBatteryReader.ReadBatteryLevel(device.InstanceId);
            Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: BTC InstanceId result={batteryLevel?.ToString() ?? "null"}");
        }

        // Try using MAC address
        if (!batteryLevel.HasValue && device.Address != 0)
        {
            Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: trying BTC with MAC={device.Address:X12}");
            batteryLevel = _btcBatteryReader.ReadBatteryLevelByMac(device.Address);
            Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: BTC MAC result={batteryLevel?.ToString() ?? "null"}");
        }

        if (batteryLevel.HasValue && device.BatteryLevel != batteryLevel.Value)
        {
            var oldLevel = device.BatteryLevel;
            device.BatteryLevel = batteryLevel.Value;

            DeviceBatteryChanged?.Invoke(this, new DeviceBatteryChangedEventArgs
            {
                DeviceId = deviceId,
                DeviceName = device.Name,
                OldBatteryLevel = oldLevel,
                NewBatteryLevel = batteryLevel.Value
            });
        }

        Helpers.DebugLogger.Log($" ReadBatteryLevelAsync: final result={batteryLevel?.ToString() ?? "null"}");
        return batteryLevel;
    }

    private void OnDeviceAdded(object? sender, Platform.Bluetooth.DeviceDiscoveredEventArgs e)
    {
        var device = new BluetoothDevice
        {
            Id = e.DeviceId,
            Address = e.Address,
            Name = e.Name ?? "Unknown Device",
            DeviceType = e.DeviceType == DiscoveredDeviceType.LowEnergy 
                ? BluetoothDeviceType.LowEnergy 
                : BluetoothDeviceType.Classic,
            IsConnected = e.IsConnected,
            IsPaired = e.IsPaired,
            LastSeen = DateTime.Now
        };

        // Check if this is an audio device (including Hands-Free endpoints by name)
        var audioEndpoints = _audioEnumerator.FindEndpointsByMacAddress(e.Address, device.Name);
        device.IsAudioDevice = audioEndpoints.Any();

        // Determine category from name (simplified)
        device.Category = DetermineCategory(device.Name);

        _devices[e.DeviceId] = device;

        DeviceDiscovered?.Invoke(this, new Events.DeviceDiscoveredEventArgs
        {
            DeviceId = e.DeviceId,
            DeviceName = device.Name,
            Address = e.Address,
            IsConnected = e.IsConnected,
            IsPaired = e.IsPaired
        });

        // Read battery level asynchronously
        _ = ReadBatteryLevelAsync(e.DeviceId);
    }

    private void OnDeviceUpdated(object? sender, Platform.Bluetooth.DeviceDiscoveredEventArgs e)
    {
        if (_devices.TryGetValue(e.DeviceId, out var device))
        {
            var wasConnected = device.IsConnected;
            device.IsConnected = e.IsConnected;
            device.LastSeen = DateTime.Now;

            if (wasConnected != e.IsConnected)
            {
                DeviceConnectionChanged?.Invoke(this, new DeviceConnectionChangedEventArgs
                {
                    DeviceId = e.DeviceId,
                    DeviceName = device.Name,
                    IsConnected = e.IsConnected
                });

                // Re-read battery level on connection change
                if (e.IsConnected)
                {
                    _ = ReadBatteryLevelAsync(e.DeviceId);
                }
            }
        }
    }

    private void OnDeviceRemoved(object? sender, string deviceId)
    {
        if (_devices.TryRemove(deviceId, out var device))
        {
            DeviceRemoved?.Invoke(this, new DeviceRemovedEventArgs
            {
                DeviceId = deviceId,
                DeviceName = device.Name
            });
        }
    }

    private void OnBleBatteryChanged(object? sender, BatteryLevelChangedEventArgs e)
    {
        if (_devices.TryGetValue(e.DeviceId, out var device))
        {
            var oldLevel = device.BatteryLevel;
            device.BatteryLevel = e.BatteryLevel;

            DeviceBatteryChanged?.Invoke(this, new DeviceBatteryChangedEventArgs
            {
                DeviceId = e.DeviceId,
                DeviceName = device.Name,
                OldBatteryLevel = oldLevel,
                NewBatteryLevel = e.BatteryLevel
            });
        }
    }

    private void PollBatteryLevels(object? state)
    {
        foreach (var device in _devices.Values.Where(d => d.IsConnected))
        {
            _ = ReadBatteryLevelAsync(device.Id);
        }
    }

    private static DeviceCategory DetermineCategory(string name)
    {
        var lowerName = name.ToLowerInvariant();

        if (lowerName.Contains("airpods") || lowerName.Contains("earbuds") || lowerName.Contains("buds"))
            return DeviceCategory.Earbuds;
        if (lowerName.Contains("headphone") || lowerName.Contains("headset") || lowerName.Contains("wh-") || lowerName.Contains("qc"))
            return DeviceCategory.Headphones;
        if (lowerName.Contains("speaker") || lowerName.Contains("soundlink") || lowerName.Contains("jbl"))
            return DeviceCategory.Speaker;
        if (lowerName.Contains("keyboard"))
            return DeviceCategory.Keyboard;
        if (lowerName.Contains("mouse") || lowerName.Contains("mx master"))
            return DeviceCategory.Mouse;
        if (lowerName.Contains("controller") || lowerName.Contains("gamepad") || lowerName.Contains("xbox") || lowerName.Contains("dualsense"))
            return DeviceCategory.Gamepad;
        if (lowerName.Contains("phone") || lowerName.Contains("iphone") || lowerName.Contains("galaxy"))
            return DeviceCategory.Phone;
        if (lowerName.Contains("watch"))
            return DeviceCategory.Watch;

        return DeviceCategory.Other;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Stop();
        _batteryPollTimer.Dispose();
        _discoverer.Dispose();
        _bleBatteryReader.Dispose();
        _audioEnumerator.Dispose();
    }
}
