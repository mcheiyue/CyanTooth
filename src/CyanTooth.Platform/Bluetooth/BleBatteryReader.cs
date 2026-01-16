using CyanTooth.Platform.Helpers;
using System.Runtime.InteropServices;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;

using CyanTooth.Platform.Helpers;
namespace CyanTooth.Platform.Bluetooth;

/// <summary>
/// Reads battery levels from BLE devices using GATT Battery Service
/// </summary>
public class BleBatteryReader
{
    // Standard Bluetooth GATT UUIDs
    private static readonly Guid BatteryServiceUuid = new("0000180f-0000-1000-8000-00805f9b34fb");
    private static readonly Guid BatteryLevelCharacteristicUuid = new("00002a19-0000-1000-8000-00805f9b34fb");

    private readonly Dictionary<string, GattCharacteristic> _subscribedCharacteristics = new();

    public event EventHandler<BatteryLevelChangedEventArgs>? BatteryLevelChanged;

    /// <summary>
    /// Reads battery level from a BLE device
    /// </summary>
    /// <param name="device">The BLE device</param>
    /// <returns>Battery level (0-100) or null if unavailable</returns>
    public async Task<byte?> ReadBatteryLevelAsync(BluetoothLEDevice device)
    {
        try
        {
            // Get Battery Service
            var servicesResult = await device.GetGattServicesForUuidAsync(BatteryServiceUuid);
            if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
            {
                return null;
            }

            var batteryService = servicesResult.Services[0];

            // Get Battery Level Characteristic
            var charsResult = await batteryService.GetCharacteristicsForUuidAsync(BatteryLevelCharacteristicUuid);
            if (charsResult.Status != GattCommunicationStatus.Success || charsResult.Characteristics.Count == 0)
            {
                return null;
            }

            var batteryChar = charsResult.Characteristics[0];

            // Read the battery level value
            var readResult = await batteryChar.ReadValueAsync();
            if (readResult.Status != GattCommunicationStatus.Success)
            {
                return null;
            }

            var reader = DataReader.FromBuffer(readResult.Value);
            return reader.ReadByte();
        }
        catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
        {
            // Device may be disconnected or access denied
            return null;
        }
    }

    /// <summary>
    /// Subscribes to battery level notifications from a BLE device
    /// </summary>
    public async Task<bool> SubscribeToBatteryNotificationsAsync(BluetoothLEDevice device)
    {
        try
        {
            var servicesResult = await device.GetGattServicesForUuidAsync(BatteryServiceUuid);
            if (servicesResult.Status != GattCommunicationStatus.Success || servicesResult.Services.Count == 0)
            {
                return false;
            }

            var batteryService = servicesResult.Services[0];
            var charsResult = await batteryService.GetCharacteristicsForUuidAsync(BatteryLevelCharacteristicUuid);
            if (charsResult.Status != GattCommunicationStatus.Success || charsResult.Characteristics.Count == 0)
            {
                return false;
            }

            var batteryChar = charsResult.Characteristics[0];

            // Check if characteristic supports notifications
            if (!batteryChar.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                return false;
            }

            // Subscribe to notifications
            var status = await batteryChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            if (status != GattCommunicationStatus.Success)
            {
                return false;
            }

            // Register for value changed events
            batteryChar.ValueChanged += OnBatteryValueChanged;
            _subscribedCharacteristics[device.DeviceId] = batteryChar;

            return true;
        }
        catch (Exception ex) when (ex is COMException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    /// Unsubscribes from battery level notifications
    /// </summary>
    public async Task UnsubscribeFromBatteryNotificationsAsync(string deviceId)
    {
        if (_subscribedCharacteristics.TryGetValue(deviceId, out var batteryChar))
        {
            try
            {
                batteryChar.ValueChanged -= OnBatteryValueChanged;
                await batteryChar.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.None);
            }
            catch
            {
                // Ignore errors during unsubscribe
            }
            finally
            {
                _subscribedCharacteristics.Remove(deviceId);
            }
        }
    }

    private void OnBatteryValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
    {
        try
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            var batteryLevel = reader.ReadByte();

            // Find the device ID for this characteristic
            var deviceId = _subscribedCharacteristics
                .FirstOrDefault(x => x.Value == sender).Key;

            if (deviceId != null)
            {
                BatteryLevelChanged?.Invoke(this, new BatteryLevelChangedEventArgs(deviceId, batteryLevel));
            }
        }
        catch
        {
            // Ignore parsing errors
        }
    }

    public void Dispose()
    {
        foreach (var kvp in _subscribedCharacteristics)
        {
            kvp.Value.ValueChanged -= OnBatteryValueChanged;
        }
        _subscribedCharacteristics.Clear();
    }
}

public class BatteryLevelChangedEventArgs : EventArgs
{
    public string DeviceId { get; }
    public byte BatteryLevel { get; }

    public BatteryLevelChangedEventArgs(string deviceId, byte batteryLevel)
    {
        DeviceId = deviceId;
        BatteryLevel = batteryLevel;
    }
}
