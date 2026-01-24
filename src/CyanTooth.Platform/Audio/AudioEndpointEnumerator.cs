using CyanTooth.Platform.Helpers;
using Microsoft.Win32;
using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
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
    // Standard Windows Property Keys (using Ole32.PROPERTYKEY - Vanara compatible type)
    private static readonly Ole32.PROPERTYKEY PKEY_Device_FriendlyName = 
        new(new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14);
    private static readonly Ole32.PROPERTYKEY PKEY_AudioEndpoint_Codec = 
        new(new Guid("7811094D-3721-4993-94EC-23A9E963E090"), 2);

    private IMMDeviceEnumerator? _deviceEnumerator;

    public AudioEndpointEnumerator()
    {
        _deviceEnumerator = new IMMDeviceEnumerator();
    }

    /// <summary>
    /// Gets all Bluetooth audio render endpoints (following BluetoothDevicePairing reference implementation)
    /// </summary>
    public IReadOnlyList<AudioEndpointInfo> GetBluetoothAudioEndpoints()
    {
        var endpoints = new List<AudioEndpointInfo>();

        if (_deviceEnumerator == null) return endpoints;

        try
        {
            // Enumerate ALL audio endpoints (not just active) - matching reference implementation
            var collection = _deviceEnumerator.EnumAudioEndpoints(EDataFlow.eAll, DEVICE_STATE.DEVICE_STATEMASK_ALL);
            if (collection == null) return endpoints;
            
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
                            string? deviceId = device.GetId();
                            var propertyStore = device.OpenPropertyStore(STGM.STGM_READ);
                            
                            string friendlyName = "Unknown";
                            if (propertyStore != null)
                            {
                                try
                                {
                                    var friendlyNameProp = propertyStore.GetValue(PKEY_Device_FriendlyName);
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
                                    var containerIdVal = propertyStore.GetValue(Ole32.PROPERTYKEY.System.Devices.ContainerId);
                                    if (containerIdVal is Guid g) containerId = g;
                                    
                                    // Try to read A2DP Codec (Win11 22H2+)
                                    var codecVal = propertyStore.GetValue(PKEY_AudioEndpoint_Codec);
                                    if (codecVal != null)
                                    {
                                        // 0=SBC, 1=AAC, 2=aptX, 3=aptX HD, 4=LDAC, 5=LC3
                                        if (codecVal is uint codecIndex || codecVal is int)
                                        {
                                            uint idx = codecVal is uint u ? u : (uint)(int)codecVal;
                                            codec = GetCodecName(idx);
                                            DebugLogger.Log($"GetBluetoothAudioEndpoints: Found Codec={codec} for {friendlyName}");
                                        }
                                    }
                                    
                                    // Fallback: Check for Alternative A2DP Driver (Win10/11)
                                    // Note: DriverProvider check might fail on some systems (returns empty),
                                    // so we directly check the registry. If the key exists, AltA2DP is active.
                                    if (codec == null && connectedDeviceId != null)
                                    {
                                        try
                                        {
                                            // Direct registry check - more robust than PropertyStore
                                            codec = GetAltA2DPCodec(connectedDeviceId);
                                            if (codec != null)
                                            {
                                                DebugLogger.Log($"GetBluetoothAudioEndpoints: Found AltA2DP Codec={codec} for {friendlyName}");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            DebugLogger.Log($"GetBluetoothAudioEndpoints: AltA2DP check failed: {ex.Message}");
                                        }
                                    }
                                }
                                catch { }
                            }
                            
                            var endpointInfo = new AudioEndpointInfo
                            {
                                DeviceId = deviceId ?? string.Empty,
                                FriendlyName = friendlyName,
                                ContainerId = containerId,
                                Codec = codec,
                                ConnectedDeviceId = connectedDeviceId,
                                IsBluetooth = true,
                                KsControl = ksControl,
                                MMDevice = device,
                                IsConnected = true // If we found it via topology, it's connected
                            };
                            
                            endpoints.Add(endpointInfo);
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

        DebugLogger.Log($"GetBluetoothAudioEndpoints: returning {endpoints.Count} endpoints");
        return endpoints;
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

    private string GetCodecName(uint idx)
    {
        return idx switch
        {
            0 => "SBC",
            1 => "AAC",
            2 => "aptX",
            3 => "aptX HD",
            4 => "LDAC",
            5 => "LC3",
            _ => $"Codec {idx}"
        };
    }

    private string? GetAltA2DPCodec(string connectedDeviceId)
    {
        try
        {
            // Regex extraction: Look for 12 hex chars surrounded by delimiters
            var match = Regex.Match(connectedDeviceId, @"&([0-9a-fA-F]{12})_");
            if (!match.Success) match = Regex.Match(connectedDeviceId, @"dev_([0-9a-fA-F]{12})");

            if (!match.Success) 
            {
                // Silence logs for known non-A2DP devices (like HFP)
                if (!connectedDeviceId.Contains("bthhfenum", StringComparison.OrdinalIgnoreCase))
                {
                    DebugLogger.Log($"GetAltA2DPCodec: Could not extract MAC from ID: {connectedDeviceId}");
                }
                return null;
            }

            string mac = match.Groups[1].Value; 
            
            // Structure: Devices\Current\0000<MAC_LOWER>
            string currentPath = @"SYSTEM\CurrentControlSet\Services\AltA2dp\Parameters\Devices\Current";
            using var currentKey = Registry.LocalMachine.OpenSubKey(currentPath);
            if (currentKey != null)
            {
                string targetSubKey = $"0000{mac.ToLower()}";
                using var deviceKey = currentKey.OpenSubKey(targetSubKey);
                if (deviceKey != null)
                {
                    return ReadCodecFromKey(deviceKey);
                }
            }
            
            // Fallback (Old structure): Devices\<MAC>
            string keyPath = $@"SYSTEM\CurrentControlSet\Services\AltA2dp\Parameters\Devices\{mac.ToUpper()}";
            using var key = Registry.LocalMachine.OpenSubKey(keyPath);
            if (key != null) return ReadCodecFromKey(key);

            return null;
        }
        catch (Exception ex)
        {
            DebugLogger.Log($"GetAltA2DPCodec: Failed to read registry: {ex.Message}");
        }
        return null;
    }

    private string? ReadCodecFromKey(RegistryKey key)
    {
        // Check 'Opened' status first to ensure we only show codec for active devices
        // 1 = Active/Streaming/Connected, 0 = Disconnected/Inactive
        var openedObj = key.GetValue("Opened");
        if (openedObj is int opened && opened != 1)
        {
            return null;
        }

        // Priority: "Codec" seems to be the dynamic status field updated by the driver.
        // Putting it first ensures we get the real-time value.
        var fields = new[] { "Codec", "A2dpCodec", "CodecType", "CodecId" };
        
        foreach (var field in fields)
        {
            var val = key.GetValue(field);
            if (val is int intVal)
            {
                // Standard mapping: 0=SBC, 1=AAC, 2=aptX, 3=aptX HD, 4=LDAC, 5=LC3
                return GetCodecName((uint)intVal);
            }
        }

        // Special handling for VendorId/CodecId pair if present
        var vendorId = key.GetValue("VendorId");
        var vendorCodecId = key.GetValue("VendorCodecId");
        if (vendorId is int vid && vendorCodecId is int vcid)
        {
            return GetVendorCodecName((uint)vid, (uint)vcid);
        }

        return null;
    }

    private string GetVendorCodecName(uint vendorId, uint vendorCodecId)
    {
        // aptX: Vendor 0x004F, Codec 0x0001
        // aptX HD: Vendor 0x00D7, Codec 0x0024
        // LDAC: Vendor 0x012D, Codec 0x00AA
        
        if (vendorId == 0x004F && vendorCodecId == 0x0001) return "aptX";
        if (vendorId == 0x00D7 && vendorCodecId == 0x0024) return "aptX HD";
        if (vendorId == 0x012D && vendorCodecId == 0x00AA) return "LDAC";
        
        return $"Vendor {vendorId:X4}:{vendorCodecId:X4}";
    }

    public void Dispose()
    {
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
    public bool IsConnected { get; init; }
}
