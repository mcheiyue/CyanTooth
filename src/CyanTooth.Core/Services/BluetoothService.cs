using CyanTooth.Platform.Helpers;


using System.Collections.Concurrent;
using CyanTooth.Core.Events;
using CyanTooth.Core.Models;
using CyanTooth.Platform.Audio;
using CyanTooth.Platform.Bluetooth;


namespace CyanTooth.Core.Services;

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
        return await Task.Run(() =>
        {
            DebugLogger.Log($" ConnectAsync: deviceId={deviceId}");
            
            if (!_devices.TryGetValue(deviceId, out var device))
            {
                DebugLogger.Log($" ConnectAsync: device not found in _devices");
                return false;
            }

            DebugLogger.Log($" ConnectAsync: device={device.Name}, Address={device.Address:X12}");

            // Method 1: Try KsControl (primary method, fast and reliable)
            var endpoints = _audioEnumerator.FindEndpointsByMacAddress(device.Address, device.Name);
            DebugLogger.Log($" ConnectAsync: trying KsControl, found {endpoints.Count()} endpoints by MAC/Name");
            
            foreach (var endpoint in endpoints)
            {
                DebugLogger.Log($" ConnectAsync: trying endpoint DeviceId={endpoint.DeviceId}, HasKsControl={endpoint.KsControl != null}");
                if (_audioConnector.Connect(endpoint))
                {
                    DebugLogger.Log($" ConnectAsync: SUCCESS via KsControl");
                    return true;
                }
            }

            // Method 2: Try using the container ID
            if (device.ContainerId.HasValue)
            {
                DebugLogger.Log($" ConnectAsync: trying ContainerId={device.ContainerId.Value}");
                var endpoint = _audioEnumerator.FindEndpointByContainerId(device.ContainerId.Value);
                if (endpoint != null)
                {
                    DebugLogger.Log($" ConnectAsync: found endpoint by ContainerId, HasKsControl={endpoint.KsControl != null}");
                    return _audioConnector.Connect(endpoint);
                }
            }

            DebugLogger.Log($" ConnectAsync: FAILED - no valid endpoint found");
            return false;
        });
    }

    /// <summary>
    /// Disconnects a Bluetooth audio device
    /// </summary>
    public async Task<bool> DisconnectAsync(string deviceId)
    {
        return await Task.Run(() =>
        {
            DebugLogger.Log($" DisconnectAsync: deviceId={deviceId}");
            
            if (!_devices.TryGetValue(deviceId, out var device))
            {
                DebugLogger.Log($" DisconnectAsync: device not found");
                return false;
            }

            DebugLogger.Log($" DisconnectAsync: device={device.Name}, Address={device.Address:X12}");

            // Method 1: Try KsControl on ALL endpoints (like ToothTray does)
            var endpoints = _audioEnumerator.FindEndpointsByMacAddress(device.Address, device.Name).ToList();
            DebugLogger.Log($" DisconnectAsync: trying KsControl, found {endpoints.Count} endpoints");
            
            bool anySuccess = false;
            foreach (var endpoint in endpoints)
            {
                DebugLogger.Log($" DisconnectAsync: trying endpoint DeviceId={endpoint.DeviceId}, HasKsControl={endpoint.KsControl != null}");
                if (_audioConnector.Disconnect(endpoint))
                {
                    DebugLogger.Log($" DisconnectAsync: endpoint disconnected successfully");
                    anySuccess = true;
                }
            }

            // Method 2: Also try using the container ID if we haven't succeeded yet
            if (!anySuccess && device.ContainerId.HasValue)
            {
                DebugLogger.Log($" DisconnectAsync: trying ContainerId={device.ContainerId.Value}");
                var endpoint = _audioEnumerator.FindEndpointByContainerId(device.ContainerId.Value);
                if (endpoint != null)
                {
                    if (_audioConnector.Disconnect(endpoint))
                    {
                        anySuccess = true;
                    }
                }
            }

            DebugLogger.Log($" DisconnectAsync: {(anySuccess ? "SUCCESS" : "FAILED")}");
            return anySuccess;
        });
    }

    /// <summary>
    /// Reads the battery level for a device
    /// </summary>
    public async Task<byte?> ReadBatteryLevelAsync(string deviceId)
    {
        DebugLogger.Log($" ReadBatteryLevelAsync: deviceId={deviceId}");
        
        if (!_devices.TryGetValue(deviceId, out var device))
        {
            DebugLogger.Log($" ReadBatteryLevelAsync: device not found");
            return null;
        }

        DebugLogger.Log($" ReadBatteryLevelAsync: device={device.Name}, Type={device.DeviceType}, InstanceId={device.InstanceId ?? "null"}, Address={device.Address:X12}");

        byte? batteryLevel = null;

        // Try BLE GATT first
        if (device.DeviceType is BluetoothDeviceType.LowEnergy or BluetoothDeviceType.DualMode)
        {
            DebugLogger.Log($" ReadBatteryLevelAsync: trying BLE GATT");
            var bleDevice = await DeviceDiscoverer.GetBluetoothLeDeviceAsync(deviceId);
            if (bleDevice != null)
            {
                batteryLevel = await _bleBatteryReader.ReadBatteryLevelAsync(bleDevice);
                DebugLogger.Log($" ReadBatteryLevelAsync: BLE result={batteryLevel?.ToString() ?? "null"}");
            }
            else
            {
                DebugLogger.Log($" ReadBatteryLevelAsync: BLE device is null");
            }
        }

        // Try Classic Bluetooth PnP
        if (!batteryLevel.HasValue && device.InstanceId != null)
        {
            DebugLogger.Log($" ReadBatteryLevelAsync: trying BTC with InstanceId={device.InstanceId}");
            batteryLevel = _btcBatteryReader.ReadBatteryLevel(device.InstanceId);
            DebugLogger.Log($" ReadBatteryLevelAsync: BTC InstanceId result={batteryLevel?.ToString() ?? "null"}");
        }

        // Try using MAC address
        if (!batteryLevel.HasValue && device.Address != 0)
        {
            DebugLogger.Log($" ReadBatteryLevelAsync: trying BTC with MAC={device.Address:X12}");
            batteryLevel = _btcBatteryReader.ReadBatteryLevelByMac(device.Address);
            DebugLogger.Log($" ReadBatteryLevelAsync: BTC MAC result={batteryLevel?.ToString() ?? "null"}");
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

        DebugLogger.Log($" ReadBatteryLevelAsync: final result={batteryLevel?.ToString() ?? "null"}");
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
        var audioEndpoints = _audioEnumerator.FindEndpointsByMacAddress(e.Address, device.Name).ToList();
        device.IsAudioDevice = audioEndpoints.Any();

        // Try to get Codec from endpoints (Win11 22H2+)
        // Prefer the one that has a codec value (usually the A2DP Stereo endpoint)
        var codecEndpoint = audioEndpoints.FirstOrDefault(ep => !string.IsNullOrEmpty(ep.Codec));
        if (codecEndpoint != null)
        {
            device.AudioCodec = codecEndpoint.Codec;
            DebugLogger.Log($"OnDeviceAdded: Device {device.Name} has Codec={device.AudioCodec}");
        }

        device.ClassOfDevice = e.ClassOfDevice;

        // Determine category using Name, CoD, and Audio status
        device.Category = DetermineCategory(device.Name, device.ClassOfDevice, device.IsAudioDevice);

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

    private static DeviceCategory DetermineCategory(string name, uint cod, bool isAudioDevice)
    {
        // Debug Log: Output raw CoD for analysis
        DebugLogger.Log($"[DeviceCategory] Determine for '{name}': CoD=0x{cod:X}, IsAudio={isAudioDevice}");

        // 1. Try CoD (Class of Device) first if available (non-zero)
        if (cod != 0)
        {
            // Major Device Class (bits 8-12)
            var majorClass = (cod >> 8) & 0x1F;
            // Minor Device Class (bits 2-7)
            var minorClass = (cod >> 2) & 0x3F;

            // Audio/Video
            if (majorClass == 0x04)
            {
                // Check Minor Class
                if (minorClass == 0x01) return DeviceCategory.Headphones; // Wearable Headset
                if (minorClass == 0x02) return DeviceCategory.Headphones; // Hands-free
                if (minorClass == 0x04) return DeviceCategory.Other;      // Microphone (treat as Other or specific Mic category if we had one)
                if (minorClass == 0x05) return DeviceCategory.Speaker;    // Loudspeaker
                if (minorClass == 0x06) return DeviceCategory.Headphones; // Headphones
                if (minorClass == 0x07) return DeviceCategory.Speaker;    // Portable Audio (often speakers)
                if (minorClass == 0x08) return DeviceCategory.Speaker;    // Car Audio
                
                return DeviceCategory.Speaker; // Default A/V to Speaker if unsure
            }
            
            // Computer
            if (majorClass == 0x01) return DeviceCategory.Computer;
            
            // Phone
            if (majorClass == 0x02) return DeviceCategory.Phone;
            
            // Peripheral (Keyboard/Mouse/Gamepad)
            if (majorClass == 0x05)
            {
                // Bits 6,7 of Minor Class indicate type in Peripheral Major Class?
                // Actually Peripheral uses bits 6,7 for Mouse/Keyboard interaction
                // Bit 6: Keyboard, Bit 7: Pointing device
                var isKeyboard = (minorClass & 0x10) != 0; // Bit 4 in partial map, check full spec
                // Let's stick to simpler logic or name fallback for peripherals as CoD is tricky there
                
                // Keyboard (0x10)
                if ((cod & 0x0040) != 0) return DeviceCategory.Keyboard;
                // Mouse (0x20)
                if ((cod & 0x0080) != 0) return DeviceCategory.Mouse;
                
                // Gamepad (Joystick/Gamepad) - specific minor classes
                if ((minorClass & 0x01) != 0) return DeviceCategory.Gamepad; // Joystick
                if ((minorClass & 0x02) != 0) return DeviceCategory.Gamepad; // Gamepad
            }

            // Wearable
            if (majorClass == 0x07) return DeviceCategory.Watch;
            
            // Health
            if (majorClass == 0x09) return DeviceCategory.HealthDevice;
        }

        // 2. Fallback to name matching
        var lowerName = name.ToLowerInvariant();

        if (lowerName.Contains("phone") || lowerName.Contains("iphone") || lowerName.Contains("android")) return DeviceCategory.Phone;
        if (lowerName.Contains("pc") || lowerName.Contains("laptop") || lowerName.Contains("computer") || lowerName.Contains("desktop")) return DeviceCategory.Computer;
        
        // Gamepad/Controller keywords - Check BEFORE Speaker to avoid "Xbox" matching "box" in Speaker
        if (lowerName.Contains("controller") || lowerName.Contains("gamepad") || lowerName.Contains("xbox") || lowerName.Contains("dualsense"))
            return DeviceCategory.Gamepad;

        // Headphones keywords
        if (lowerName.Contains("headphone") || lowerName.Contains("headset") || 
            lowerName.Contains("bud") || lowerName.Contains("pod") || 
            lowerName.Contains("air") || lowerName.Contains("enco") || 
            lowerName.Contains("free") || lowerName.Contains("sono") || 
            lowerName.Contains("music") || lowerName.Contains("audio")) return DeviceCategory.Headphones;
        
        // Speaker keywords
        if (lowerName.Contains("speaker") || lowerName.Contains("sound") || lowerName.Contains("box")) return DeviceCategory.Speaker;

        // Watch/Band keywords
        if (lowerName.Contains("watch") || lowerName.Contains("band") || lowerName.Contains("strap") || lowerName.Contains("wear")) return DeviceCategory.Watch;
        
        // Input devices
        if (lowerName.Contains("key") || lowerName.Contains("board")) return DeviceCategory.Keyboard;
        if (lowerName.Contains("mouse") || lowerName.Contains("mice") || lowerName.Contains("track")) return DeviceCategory.Mouse;

        // 3. Fallback: If it's identified as an audio endpoint but we couldn't determine type, default to Headphones
        if (isAudioDevice)
        {
            return DeviceCategory.Headphones;
        }

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
