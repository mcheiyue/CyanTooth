using CyanTooth.Platform.Helpers;



using System.Runtime.InteropServices;
using CyanTooth.Platform.Native;
using Vanara.PInvoke;
using static Vanara.PInvoke.CoreAudio;


namespace CyanTooth.Platform.Audio;

/// <summary>
/// Enumerates Bluetooth audio endpoints using CoreAudio API
/// </summary>
public class AudioEndpointEnumerator : IDisposable
{
    private IMMDeviceEnumerator? _deviceEnumerator;
    private readonly List<AudioEndpointInfo> _endpoints = new();

    public AudioEndpointEnumerator()
    {
        _deviceEnumerator = new IMMDeviceEnumerator();
    }

    /// <summary>
    /// Gets all Bluetooth audio render endpoints (following BluetoothDevicePairing reference implementation)
    /// </summary>
    public IReadOnlyList<AudioEndpointInfo> GetBluetoothAudioEndpoints()
    {
        _endpoints.Clear();

        if (_deviceEnumerator == null) return _endpoints;

        try
        {
            // Enumerate ALL audio endpoints (not just active) - matching reference implementation
            var collection = _deviceEnumerator.EnumAudioEndpoints(EDataFlow.eAll, DEVICE_STATE.DEVICE_STATEMASK_ALL);
            if (collection == null) return _endpoints;
            
            uint count = collection.GetCount();
            DebugLogger.Log($"GetBluetoothAudioEndpoints: total audio endpoints={count}");

            for (uint i = 0; i < count; i++)
            {
                collection.Item(i, out var device);
                if (device == null) continue;

                try
                {
                    // Get device topology
                    var topology = device.Activate<IDeviceTopology>(Ole32.CLSCTX.CLSCTX_ALL);
                    if (topology == null) continue;

                    uint connectorCount = topology.GetConnectorCount();
                    
                    for (uint j = 0; j < connectorCount; j++)
                    {
                        try
                        {
                            var connector = topology.GetConnector(j);
                            if (connector == null) continue;
                            
                            // Get the connected-to connector
                            IConnector? connectedTo = null;
                            try
                            {
                                connectedTo = connector.GetConnectedTo();
                            }
                            catch
                            {
                                continue; // Not connected
                            }
                            
                            if (connectedTo == null) continue;

                            // Get the topology object of the connected part
                            var connectedPart = (IPart)connectedTo;
                            var connectedTopology = connectedPart.GetTopologyObject();
                            if (connectedTopology == null) continue;

                            string? connectedDeviceId = connectedTopology.GetDeviceId();
                            if (string.IsNullOrEmpty(connectedDeviceId)) continue;

                            // Check if this is a Bluetooth device - key check from reference!
                            // The connected device ID should start with "{2}.\\?\bth"
                            if (!connectedDeviceId.StartsWith(@"{2}.\\?\bth", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            DebugLogger.Log($"GetBluetoothAudioEndpoints: found BT device, connectedDeviceId={connectedDeviceId}");

                            // Get the connected device and activate IKsControl on it
                            var connectedDevice = _deviceEnumerator.GetDevice(connectedDeviceId);
                            IKsControl? ksControl = null;
                            if (connectedDevice != null)
                            {
                                try
                                {
                                    ksControl = connectedDevice.Activate<IKsControl>(Ole32.CLSCTX.CLSCTX_ALL);
                                }
                                catch (Exception ex)
                                {
                                    DebugLogger.Log($"GetBluetoothAudioEndpoints: failed to activate IKsControl: {ex.Message}");
                                }
                            }

                            // Get endpoint properties
                            string deviceId = device.GetId();
                            var propertyStore = device.OpenPropertyStore(STGM.STGM_READ);
                            
                            string friendlyName = "Unknown";
                            if (propertyStore != null)
                            {
                                try
                                {
                                    var friendlyNameKey = new Ole32.PROPERTYKEY(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);
                                    var friendlyNameProp = propertyStore.GetValue(friendlyNameKey);
                                    if (friendlyNameProp != null)
                                        friendlyName = friendlyNameProp.ToString() ?? "Unknown";
                                }
                                catch { }
                            }

                            Guid containerId = Guid.Empty;
                            string? codec = null;
                            if (propertyStore != null)
                            {
                                try
                                {
                                    var val = propertyStore.GetValue(Ole32.PROPERTYKEY.System.Devices.ContainerId);
                                    if (val is Guid g) containerId = g;
                                    
                                    // Try to read A2DP Codec (Win11 22H2+)
                                    // Key: {7811094D-3721-4993-94EC-23A9E963E090}, 2
                                    var codecKey = new Ole32.PROPERTYKEY(new Guid("7811094D-3721-4993-94EC-23A9E963E090"), 2);
                                    var codecVal = propertyStore.GetValue(codecKey);
                                    if (codecVal != null)
                                    {
                                        // 0=SBC, 1=AAC, 2=aptX, 3=aptX HD, 4=LDAC
                                        if (codecVal is uint codecIndex || codecVal is int codecIndexInt)
                                        {
                                            uint idx = codecVal is uint u ? u : (uint)(int)codecVal;
                                            codec = idx switch
                                            {
                                                0 => "SBC",
                                                1 => "AAC",
                                                2 => "aptX",
                                                3 => "aptX HD",
                                                4 => "LDAC",
                                                5 => "LC3",
                                                _ => $"Codec {idx}"
                                            };
                                            DebugLogger.Log($"GetBluetoothAudioEndpoints: Found Codec={codec} for {friendlyName}");
                                        }
                                    }
                                }
                                catch { }
                            }

                            var endpointInfo = new AudioEndpointInfo
                            {
                                DeviceId = deviceId,
                                FriendlyName = friendlyName,
                                ContainerId = containerId,
                                Codec = codec,
                                IsBluetooth = true,
                                KsControl = ksControl,
                                MMDevice = device,
                                ConnectedDeviceId = connectedDeviceId  // Save for MAC matching
                            };

                            _endpoints.Add(endpointInfo);
                            DebugLogger.Log($"GetBluetoothAudioEndpoints: added endpoint Name={friendlyName}, HasKsControl={ksControl != null}");
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.Log($"GetBluetoothAudioEndpoints: connector error: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.Log($"GetBluetoothAudioEndpoints: device error: {ex.Message}");
                }
            }
        }
        catch (COMException ex)
        {
            DebugLogger.Log($"GetBluetoothAudioEndpoints: COM error: {ex.Message}");
        }

        DebugLogger.Log($"GetBluetoothAudioEndpoints: returning {_endpoints.Count} endpoints");
        return _endpoints;
    }

    /// <summary>
    /// Finds the audio endpoint associated with a Bluetooth device by container ID
    /// </summary>
    public AudioEndpointInfo? FindEndpointByContainerId(Guid containerId)
    {
        var endpoints = GetBluetoothAudioEndpoints();
        return endpoints.FirstOrDefault(e => e.ContainerId == containerId);
    }

    /// <summary>
    /// Finds audio endpoints associated with a Bluetooth MAC address
    /// </summary>
    public IEnumerable<AudioEndpointInfo> FindEndpointsByMacAddress(ulong macAddress) 
        => FindEndpointsByMacAddress(macAddress, null);

    /// <summary>
    /// Finds audio endpoints associated with a Bluetooth MAC address and/or device name.
    /// This also matches Hands-Free endpoints by device name when MAC matching fails.
    /// </summary>
    public IEnumerable<AudioEndpointInfo> FindEndpointsByMacAddress(ulong macAddress, string? deviceName)
    {
        // Format MAC as uppercase hex without separators (e.g., "9505BB2CF7F4")
        string macHex = macAddress.ToString("X12");
        
        DebugLogger.Log($"FindEndpointsByMacAddress: looking for MAC={macHex}, DeviceName={deviceName ?? "null"}");
        
        var endpoints = GetBluetoothAudioEndpoints();
        
        DebugLogger.Log($"FindEndpointsByMacAddress: found {endpoints.Count} BT endpoints total");
        
        foreach (var ep in endpoints)
        {
            DebugLogger.Log($"FindEndpointsByMacAddress: endpoint Name={ep.FriendlyName}, ConnectedDeviceId={ep.ConnectedDeviceId ?? "null"}");
        }
        
        // Match using ConnectedDeviceId which contains the MAC address
        // Format: {2}.\\?\bthenum#dev_9505bb2cf7f4...
        var matchedByMac = endpoints.Where(e => 
            e.ConnectedDeviceId != null && 
            e.ConnectedDeviceId.Contains(macHex, StringComparison.OrdinalIgnoreCase)).ToList();
            
        DebugLogger.Log($"FindEndpointsByMacAddress: {matchedByMac.Count} matched by MAC");
        
        // Also match Hands-Free endpoints by device name
        // HFP endpoints are named like "耳机 (Device Name Hands-Free AG Audio)" or "Device Name Hands-Free AG Audio"
        if (!string.IsNullOrEmpty(deviceName))
        {
            var matchedByName = endpoints.Where(e => 
                !matchedByMac.Any(m => m.DeviceId == e.DeviceId) && // Not already matched by MAC
                e.FriendlyName.Contains("Hands-Free", StringComparison.OrdinalIgnoreCase) &&
                e.FriendlyName.Contains(deviceName, StringComparison.OrdinalIgnoreCase)).ToList();
                
            DebugLogger.Log($"FindEndpointsByMacAddress: {matchedByName.Count} additional Hands-Free matched by name");
            
            matchedByMac.AddRange(matchedByName);
        }
        
        DebugLogger.Log($"FindEndpointsByMacAddress: {matchedByMac.Count} total matched");
        
        return matchedByMac;
    }

    public void Dispose()
    {
        _endpoints.Clear();
        if (_deviceEnumerator != null)
        {
            Marshal.ReleaseComObject(_deviceEnumerator);
            _deviceEnumerator = null;
        }
    }
}

public class AudioEndpointInfo
{
    public required string DeviceId { get; init; }
    public required string FriendlyName { get; init; }
    public Guid ContainerId { get; init; }
    public string? Codec { get; init; } // Win11 22H2+ Codec info
    public bool IsBluetooth { get; init; }
    public IKsControl? KsControl { get; init; }
    public IMMDevice? MMDevice { get; init; }
    /// <summary>
    /// The connected Bluetooth device ID (format: {2}.\\?\bthenum#dev_XXXXXXXXXXXX...)
    /// This contains the MAC address and is used for matching
    /// </summary>
    public string? ConnectedDeviceId { get; init; }
}
