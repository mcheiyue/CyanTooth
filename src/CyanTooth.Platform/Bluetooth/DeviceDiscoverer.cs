using CyanTooth.Platform.Helpers;



using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;


namespace CyanTooth.Platform.Bluetooth;

/// <summary>
/// Discovers and monitors Bluetooth devices using WinRT APIs
/// </summary>
public class DeviceDiscoverer : IDisposable
{
    private DeviceWatcher? _classicWatcher;
    private DeviceWatcher? _bleWatcher;
    private bool _isRunning;

    public event EventHandler<DeviceDiscoveredEventArgs>? DeviceAdded;
    public event EventHandler<DeviceDiscoveredEventArgs>? DeviceUpdated;
    public event EventHandler<string>? DeviceRemoved;
    public event EventHandler? EnumerationCompleted;

    /// <summary>
    /// Additional properties to request during enumeration
    /// </summary>
    private static readonly string[] RequestedProperties =
    [
        "System.Devices.Aep.DeviceAddress",
        "System.Devices.Aep.IsConnected",
        "System.Devices.Aep.IsPaired",
        "System.Devices.Aep.Bluetooth.Le.IsConnectable",
        "System.Devices.Aep.Category",
        "System.Devices.Aep.ContainerId",
        "{ea900399-b1d5-4529-a35c-43f295b92209} 26" // System.Devices.Aep.Bluetooth.Cod (Canonical names not always supported for this prop)
    ];

    /// <summary>
    /// Starts discovering paired Bluetooth devices
    /// </summary>
    public void StartDiscovery()
    {
        if (_isRunning) return;

        // Create watcher for Classic Bluetooth devices
        string classicSelector = BluetoothDevice.GetDeviceSelectorFromPairingState(true);
        _classicWatcher = DeviceInformation.CreateWatcher(
            classicSelector,
            RequestedProperties,
            DeviceInformationKind.AssociationEndpoint);

        _classicWatcher.Added += OnClassicDeviceAdded;
        _classicWatcher.Updated += OnDeviceUpdated;
        _classicWatcher.Removed += OnDeviceRemoved;
        _classicWatcher.EnumerationCompleted += OnEnumerationCompleted;

        // Create watcher for BLE devices
        string bleSelector = BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
        _bleWatcher = DeviceInformation.CreateWatcher(
            bleSelector,
            RequestedProperties,
            DeviceInformationKind.AssociationEndpoint);

        _bleWatcher.Added += OnBleDeviceAdded;
        _bleWatcher.Updated += OnDeviceUpdated;
        _bleWatcher.Removed += OnDeviceRemoved;
        _bleWatcher.EnumerationCompleted += OnEnumerationCompleted;

        _classicWatcher.Start();
        _bleWatcher.Start();
        _isRunning = true;
    }

    /// <summary>
    /// Stops device discovery
    /// </summary>
    public void StopDiscovery()
    {
        if (!_isRunning) return;

        if (_classicWatcher != null)
        {
            _classicWatcher.Stop();
            _classicWatcher.Added -= OnClassicDeviceAdded;
            _classicWatcher.Updated -= OnDeviceUpdated;
            _classicWatcher.Removed -= OnDeviceRemoved;
            _classicWatcher.EnumerationCompleted -= OnEnumerationCompleted;
        }

        if (_bleWatcher != null)
        {
            _bleWatcher.Stop();
            _bleWatcher.Added -= OnBleDeviceAdded;
            _bleWatcher.Updated -= OnDeviceUpdated;
            _bleWatcher.Removed -= OnDeviceRemoved;
            _bleWatcher.EnumerationCompleted -= OnEnumerationCompleted;
        }

        _isRunning = false;
    }

    /// <summary>
    /// Gets a Bluetooth device by its ID
    /// </summary>
    public static async Task<BluetoothDevice?> GetBluetoothDeviceAsync(string deviceId)
    {
        try
        {
            return await BluetoothDevice.FromIdAsync(deviceId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a Bluetooth LE device by its ID
    /// </summary>
    public static async Task<BluetoothLEDevice?> GetBluetoothLeDeviceAsync(string deviceId)
    {
        try
        {
            return await BluetoothLEDevice.FromIdAsync(deviceId);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a Bluetooth device by its MAC address
    /// </summary>
    public static async Task<BluetoothDevice?> GetBluetoothDeviceByAddressAsync(ulong address)
    {
        try
        {
            return await BluetoothDevice.FromBluetoothAddressAsync(address);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a Bluetooth LE device by its MAC address
    /// </summary>
    public static async Task<BluetoothLEDevice?> GetBluetoothLeDeviceByAddressAsync(ulong address)
    {
        try
        {
            return await BluetoothLEDevice.FromBluetoothAddressAsync(address);
        }
        catch
        {
            return null;
        }
    }

    private async void OnClassicDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
    {
        var device = await GetBluetoothDeviceAsync(deviceInfo.Id);
        if (device == null) return;

        uint cod = 0;
        // Use the GUID format key for CoD retrieval
        if (deviceInfo.Properties.TryGetValue("{ea900399-b1d5-4529-a35c-43f295b92209} 26", out var codVal) && codVal is uint codUint)
        {
            cod = codUint;
        }

        var args = new DeviceDiscoveredEventArgs
        {
            DeviceId = deviceInfo.Id,
            Name = device.Name,
            Address = device.BluetoothAddress,
            ClassOfDevice = cod,
            DeviceType = DiscoveredDeviceType.Classic,
            IsConnected = device.ConnectionStatus == BluetoothConnectionStatus.Connected,
            IsPaired = deviceInfo.Pairing.IsPaired
        };

        DeviceAdded?.Invoke(this, args);
    }

    private async void OnBleDeviceAdded(DeviceWatcher sender, DeviceInformation deviceInfo)
    {
        var device = await GetBluetoothLeDeviceAsync(deviceInfo.Id);
        if (device == null) return;

        // BLE devices usually don't have standard CoD, but we check anyway
        uint cod = 0;
        // Use the GUID format key for CoD retrieval
        if (deviceInfo.Properties.TryGetValue("{ea900399-b1d5-4529-a35c-43f295b92209} 26", out var codVal) && codVal is uint codUint)
        {
            cod = codUint;
        }

        var args = new DeviceDiscoveredEventArgs
        {
            DeviceId = deviceInfo.Id,
            Name = device.Name,
            Address = device.BluetoothAddress,
            ClassOfDevice = cod,
            DeviceType = DiscoveredDeviceType.LowEnergy,
            IsConnected = device.ConnectionStatus == BluetoothConnectionStatus.Connected,
            IsPaired = deviceInfo.Pairing.IsPaired
        };

        DeviceAdded?.Invoke(this, args);
    }

    private void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
    {
        var isConnected = false;
        if (deviceInfoUpdate.Properties.TryGetValue("System.Devices.Aep.IsConnected", out var connected))
        {
            isConnected = connected as bool? ?? false;
        }

        var args = new DeviceDiscoveredEventArgs
        {
            DeviceId = deviceInfoUpdate.Id,
            IsConnected = isConnected
        };

        DeviceUpdated?.Invoke(this, args);
    }

    private void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
    {
        DeviceRemoved?.Invoke(this, deviceInfoUpdate.Id);
    }

    private void OnEnumerationCompleted(DeviceWatcher sender, object args)
    {
        EnumerationCompleted?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        StopDiscovery();
        _classicWatcher = null;
        _bleWatcher = null;
    }
}

public enum DiscoveredDeviceType
{
    Classic,
    LowEnergy
}

public class DeviceDiscoveredEventArgs : EventArgs
{
    public required string DeviceId { get; init; }
    public string? Name { get; init; }
    public ulong Address { get; init; }
    public uint ClassOfDevice { get; init; }
    public DiscoveredDeviceType DeviceType { get; init; }
    public bool IsConnected { get; init; }
    public bool IsPaired { get; init; }

    public string MacAddress => FormatMacAddress(Address);

    private static string FormatMacAddress(ulong address)
    {
        if (address == 0) return string.Empty;
        var bytes = BitConverter.GetBytes(address);
        return $"{bytes[5]:X2}:{bytes[4]:X2}:{bytes[3]:X2}:{bytes[2]:X2}:{bytes[1]:X2}:{bytes[0]:X2}";
    }
}
