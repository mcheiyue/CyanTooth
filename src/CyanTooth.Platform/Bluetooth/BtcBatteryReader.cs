using CyanTooth.Platform.Helpers;



using System.Runtime.InteropServices;
using CyanTooth.Platform.Native;


namespace CyanTooth.Platform.Bluetooth;

/// <summary>
/// Reads battery levels from Classic Bluetooth devices using Windows PnP API
/// </summary>
public class BtcBatteryReader
{
    // DEVPKEY for Bluetooth battery level: {104EA319-6EE2-4701-BD47-8DDBF425BBE5}, pid=2
    private static readonly CfgMgr32.DEVPROPKEY DEVPKEY_Bluetooth_BatteryLevel = new(
        new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"), 2);

    /// <summary>
    /// Reads battery level from a classic Bluetooth device using its instance ID
    /// </summary>
    /// <param name="instanceId">The PnP device instance ID (e.g., "BTHENUM\\Dev_XXXXXXXXXXXX\\...")</param>
    /// <returns>Battery level (0-100) or null if unavailable</returns>
    public byte? ReadBatteryLevel(string instanceId)
    {
        DebugLogger.Log($" BtcBatteryReader.ReadBatteryLevel: instanceId={instanceId}");
        try
        {
            // Locate the device node
            var result = CfgMgr32.CM_Locate_DevNodeW(
                out uint devInst,
                instanceId,
                CfgMgr32.CM_LOCATE_DEVNODE_NORMAL);

            DebugLogger.Log($" BtcBatteryReader: CM_Locate_DevNodeW(NORMAL) result={result}");

            if (result != CfgMgr32.CR_SUCCESS)
            {
                // Try with phantom flag for disconnected devices
                result = CfgMgr32.CM_Locate_DevNodeW(
                    out devInst,
                    instanceId,
                    CfgMgr32.CM_LOCATE_DEVNODE_PHANTOM);

                DebugLogger.Log($" BtcBatteryReader: CM_Locate_DevNodeW(PHANTOM) result={result}");

                if (result != CfgMgr32.CR_SUCCESS)
                {
                    return null;
                }
            }

            var batteryLevel = ReadBatteryLevelFromDevNode(devInst);
            DebugLogger.Log($" BtcBatteryReader: ReadBatteryLevelFromDevNode result={batteryLevel?.ToString() ?? "null"}");
            return batteryLevel;
        }
        catch (Exception ex)
        {
            DebugLogger.Log($" BtcBatteryReader: Exception={ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Reads battery level from a classic Bluetooth device using its MAC address
    /// Following BlueGauge implementation: enumerate GUID_DEVCLASS_SYSTEM devices,
    /// filter by BTHENUM, then match by DEVPKEY_Bluetooth_DeviceAddress
    /// </summary>
    /// <param name="macAddress">The Bluetooth MAC address</param>
    /// <returns>Battery level (0-100) or null if unavailable</returns>
    public byte? ReadBatteryLevelByMac(ulong macAddress)
    {
        DebugLogger.Log($"BtcBatteryReader.ReadBatteryLevelByMac: MAC={macAddress:X12}");

        // Use GUID_DEVCLASS_SYSTEM to enumerate devices (like BlueGauge does)
        var classGuid = CfgMgr32.GUID_DEVCLASS_SYSTEM;
        var deviceInfoSet = SetupApi.SetupDiGetClassDevsW(
            ref classGuid,
            null,  // No enumerator filter
            IntPtr.Zero,
            SetupApi.DIGCF_PRESENT);

        if (deviceInfoSet == SetupApi.INVALID_HANDLE_VALUE)
        {
            DebugLogger.Log($"BtcBatteryReader: SetupDiGetClassDevsW failed, error={Marshal.GetLastWin32Error()}");
            return null;
        }

        try
        {
            var deviceInfoData = new SetupApi.SP_DEVINFO_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>()
            };

            uint index = 0;
            int btDeviceCount = 0;
            
            while (SetupApi.SetupDiEnumDeviceInfo(deviceInfoSet, index++, ref deviceInfoData))
            {
                // Get the device instance ID
                SetupApi.SetupDiGetDeviceInstanceIdW(deviceInfoSet, ref deviceInfoData, null, 0, out uint requiredSize);
                
                if (requiredSize == 0) continue;
                
                var instanceIdBuffer = new char[requiredSize];
                if (!SetupApi.SetupDiGetDeviceInstanceIdW(deviceInfoSet, ref deviceInfoData, instanceIdBuffer, requiredSize, out _))
                    continue;
                    
                var instanceId = new string(instanceIdBuffer).TrimEnd('\0');
                
                // Filter: only process BTHENUM devices (like BlueGauge does)
                if (!instanceId.Contains(@"BTHENUM\", StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                btDeviceCount++;

                // Try to read battery level first
                var batteryLevel = TryReadBatteryProperty(deviceInfoData.DevInst);
                if (!batteryLevel.HasValue)
                    continue;

                // Read device address to match
                var deviceAddress = TryReadDeviceAddress(deviceInfoSet, ref deviceInfoData);
                
                DebugLogger.Log($"BtcBatteryReader: found device InstanceId={instanceId}, Address={deviceAddress:X12}, Battery={batteryLevel}");

                if (deviceAddress == macAddress)
                {
                    DebugLogger.Log($"BtcBatteryReader: MATCH! Returning battery={batteryLevel}");
                    return batteryLevel;
                }

                // Reset for next iteration
                deviceInfoData.cbSize = (uint)Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>();
            }

            DebugLogger.Log($"BtcBatteryReader: enumerated {btDeviceCount} BTHENUM devices, no match for {macAddress:X12}");
            return null;
        }
        finally
        {
            SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }

    /// <summary>
    /// Reads DEVPKEY_Bluetooth_DeviceAddress from a device
    /// </summary>
    private ulong TryReadDeviceAddress(IntPtr deviceInfoSet, ref SetupApi.SP_DEVINFO_DATA deviceInfoData)
    {
        var propKey = new CfgMgr32.DEVPROPKEY(
            CfgMgr32.PKEY_Bluetooth_DeviceAddress_Fmtid,
            CfgMgr32.PKEY_Bluetooth_DeviceAddress_Pid);

        // First call to get required size
        SetupApi.SetupDiGetDevicePropertyW(
            deviceInfoSet,
            ref deviceInfoData,
            ref propKey,
            out uint propertyType,
            IntPtr.Zero,
            0,
            out uint requiredSize,
            0);

        if (requiredSize == 0)
            return 0;

        IntPtr buffer = Marshal.AllocHGlobal((int)requiredSize);
        try
        {
            if (!SetupApi.SetupDiGetDevicePropertyW(
                deviceInfoSet,
                ref deviceInfoData,
                ref propKey,
                out propertyType,
                buffer,
                requiredSize,
                out _,
                0))
            {
                return 0;
            }

            // The address is stored as a string like "XX:XX:XX:XX:XX:XX" or as raw bytes
            // Property type 0x12 = DEVPROP_TYPE_STRING
            if (propertyType == CfgMgr32.DEVPROP_TYPE_STRING)
            {
                string addressStr = Marshal.PtrToStringUni(buffer) ?? "";
                return ParseMacAddress(addressStr);
            }
            
            // If it's stored as raw 8 bytes (UINT64)
            if (requiredSize >= 8)
            {
                return (ulong)Marshal.ReadInt64(buffer);
            }

            return 0;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static ulong ParseMacAddress(string addressStr)
    {
        // Remove separators and parse as hex
        var hex = addressStr.Replace(":", "").Replace("-", "").Trim();
        if (ulong.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out ulong result))
            return result;
        return 0;
    }

    private byte? ReadBatteryLevelFromDevNode(uint devInst)
    {
        // First try to read directly from this node
        var batteryLevel = TryReadBatteryProperty(devInst);
        if (batteryLevel.HasValue)
            return batteryLevel;
        
        // If not found, try child devices
        DebugLogger.Log($" BtcBatteryReader: trying child devices");
        
        uint childDevInst = 0;
        var result = CfgMgr32.CM_Get_Child(out childDevInst, devInst, 0);
        
        while (result == CfgMgr32.CR_SUCCESS)
        {
            batteryLevel = TryReadBatteryProperty(childDevInst);
            if (batteryLevel.HasValue)
            {
                DebugLogger.Log($" BtcBatteryReader: found battery in child device: {batteryLevel}");
                return batteryLevel;
            }
            
            // Try next sibling
            result = CfgMgr32.CM_Get_Sibling(out childDevInst, childDevInst, 0);
        }
        
        return null;
    }
    
    private byte? TryReadBatteryProperty(uint devInst)
    {
        var propKey = DEVPKEY_Bluetooth_BatteryLevel;
        uint propertyType = 0;
        uint bufferSize = 1;
        IntPtr buffer = Marshal.AllocHGlobal(1);

        try
        {
            var result = CfgMgr32.CM_Get_DevNode_PropertyW(
                devInst,
                ref propKey,
                out propertyType,
                buffer,
                ref bufferSize,
                0);

            if (result != CfgMgr32.CR_SUCCESS)
            {
                return null;
            }

            if (propertyType != CfgMgr32.DEVPROP_TYPE_BYTE)
            {
                return null;
            }

            return Marshal.ReadByte(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private byte? FindAndReadBatteryLevel(string instanceIdPattern)
    {
        DebugLogger.Log($" BtcBatteryReader.FindAndReadBatteryLevel: pattern={instanceIdPattern}");
        
        // Use SetupAPI to enumerate all Bluetooth devices and find matching one
        var deviceInfoSet = SetupApi.SetupDiGetClassDevsW(
            IntPtr.Zero,
            "BTHENUM",  // Enumerate BTHENUM devices
            IntPtr.Zero,
            SetupApi.DIGCF_PRESENT | SetupApi.DIGCF_ALLCLASSES);

        if (deviceInfoSet == SetupApi.INVALID_HANDLE_VALUE)
        {
            DebugLogger.Log($" BtcBatteryReader: SetupDiGetClassDevsW returned INVALID_HANDLE, error={Marshal.GetLastWin32Error()}");
            return null;
        }

        try
        {
            var deviceInfoData = new SetupApi.SP_DEVINFO_DATA
            {
                cbSize = (uint)Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>()
            };

            uint index = 0;
            int foundCount = 0;
            while (SetupApi.SetupDiEnumDeviceInfo(deviceInfoSet, index++, ref deviceInfoData))
            {
                // Get the device instance ID
                SetupApi.SetupDiGetDeviceInstanceIdW(deviceInfoSet, ref deviceInfoData, null, 0, out uint requiredSize);
                
                if (requiredSize > 0)
                {
                    var instanceIdBuffer = new char[requiredSize];
                    if (SetupApi.SetupDiGetDeviceInstanceIdW(deviceInfoSet, ref deviceInfoData, instanceIdBuffer, requiredSize, out _))
                    {
                        var instanceId = new string(instanceIdBuffer).TrimEnd('\0');
                        foundCount++;
                        
                        // Check if this matches our pattern (case-insensitive)
                        if (instanceId.StartsWith(instanceIdPattern, StringComparison.OrdinalIgnoreCase))
                        {
                            DebugLogger.Log($" BtcBatteryReader: MATCH found: {instanceId}");
                            // Try to read battery level from this device
                            var batteryLevel = ReadBatteryLevelFromDevNode(deviceInfoData.DevInst);
                            if (batteryLevel.HasValue)
                                return batteryLevel;
                        }
                    }
                }

                // Reset for next iteration
                deviceInfoData.cbSize = (uint)Marshal.SizeOf<SetupApi.SP_DEVINFO_DATA>();
            }

            DebugLogger.Log($" BtcBatteryReader: enumerated {foundCount} BTHENUM devices, no match");
            return null;
        }
        finally
        {
            SetupApi.SetupDiDestroyDeviceInfoList(deviceInfoSet);
        }
    }

    /// <summary>
    /// Gets the PnP instance ID for a Bluetooth device from its MAC address
    /// </summary>
    public string? GetInstanceIdFromMac(ulong macAddress)
    {
        string macHex = macAddress.ToString("X12");
        // The typical format is: BTHENUM\Dev_{MAC}\{GUID}
        // This is a simplified version - real implementation would enumerate devices
        return $"BTHENUM\\Dev_{macHex}";
    }
}
