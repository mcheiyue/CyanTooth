using System;
using System.Runtime.InteropServices;

namespace CyanTooth.Platform.Bluetooth;

/// <summary>
/// P/Invoke wrapper for BluetoothSetServiceState API.
/// This API can disconnect Bluetooth audio devices connected by Windows system.
/// </summary>
public static class BluetoothServiceStateApi
{
    [DllImport("BluetoothAPIs.dll", SetLastError = true, CallingConvention = CallingConvention.StdCall)]
    private static extern uint BluetoothSetServiceState(
        IntPtr hRadio,              // Bluetooth radio handle (pass IntPtr.Zero for default)
        ref BLUETOOTH_ADDRESS pbtdi, // Device address structure
        ref Guid pGuidService,       // Service GUID to operate on
        uint dwServiceFlags          // 0=disable/disconnect, 1=enable/connect
    );

    [StructLayout(LayoutKind.Sequential)]
    private struct BLUETOOTH_ADDRESS
    {
        public ulong ullLong; // 64-bit Bluetooth address
    }

    // A2DP Audio Sink - for music playback
    public static readonly Guid AudioSinkGuid = new Guid("0000110B-0000-1000-8000-00805F9B34FB");

    // HFP Hands-Free - for calls/microphone
    public static readonly Guid HandsFreeGuid = new Guid("0000111E-0000-1000-8000-00805F9B34FB");

    // A2DP Audio Source
    public static readonly Guid AudioSourceGuid = new Guid("0000110A-0000-1000-8000-00805F9B34FB");

    // HSP Headset
    public static readonly Guid HeadsetGuid = new Guid("00001108-0000-1000-8000-00805F9B34FB");

    private const uint BLUETOOTH_SERVICE_DISABLE = 0x00;
    private const uint BLUETOOTH_SERVICE_ENABLE = 0x01;

    /// <summary>
    /// Disconnect a Bluetooth audio device by disabling its audio services.
    /// This works for devices connected by Windows system.
    /// </summary>
    /// <param name="bluetoothAddress">Device Bluetooth address (MAC as ulong)</param>
    /// <returns>True if at least one service was successfully disabled</returns>
    public static bool DisconnectAudioDevice(ulong bluetoothAddress)
    {
        CyanTooth.Platform.Helpers.DebugLogger.Log($"BluetoothServiceStateApi.DisconnectAudioDevice: MAC={bluetoothAddress:X12}");

        BLUETOOTH_ADDRESS addr = new BLUETOOTH_ADDRESS { ullLong = bluetoothAddress };
        bool anySuccess = false;

        // 1. Disable A2DP (music)
        Guid audioGuid = AudioSinkGuid;
        uint resultA2dp = BluetoothSetServiceState(IntPtr.Zero, ref addr, ref audioGuid, BLUETOOTH_SERVICE_DISABLE);
        CyanTooth.Platform.Helpers.DebugLogger.Log($"BluetoothServiceStateApi: A2DP disable result={resultA2dp}");
        if (resultA2dp == 0) anySuccess = true;

        // 2. Disable HFP (calls/microphone)
        Guid hfpGuid = HandsFreeGuid;
        uint resultHfp = BluetoothSetServiceState(IntPtr.Zero, ref addr, ref hfpGuid, BLUETOOTH_SERVICE_DISABLE);
        CyanTooth.Platform.Helpers.DebugLogger.Log($"BluetoothServiceStateApi: HFP disable result={resultHfp}");
        if (resultHfp == 0) anySuccess = true;

        // 3. Also try HSP (some older devices)
        Guid hspGuid = HeadsetGuid;
        uint resultHsp = BluetoothSetServiceState(IntPtr.Zero, ref addr, ref hspGuid, BLUETOOTH_SERVICE_DISABLE);
        CyanTooth.Platform.Helpers.DebugLogger.Log($"BluetoothServiceStateApi: HSP disable result={resultHsp}");
        if (resultHsp == 0) anySuccess = true;

        CyanTooth.Platform.Helpers.DebugLogger.Log($"BluetoothServiceStateApi.DisconnectAudioDevice: anySuccess={anySuccess}");
        return anySuccess;
    }

    /// <summary>
    /// Connect a Bluetooth audio device by enabling its audio services.
    /// </summary>
    /// <param name="bluetoothAddress">Device Bluetooth address (MAC as ulong)</param>
    /// <returns>True if at least one service was successfully enabled</returns>
    public static bool ConnectAudioDevice(ulong bluetoothAddress)
    {
        CyanTooth.Platform.Helpers.DebugLogger.Log($"BluetoothServiceStateApi.ConnectAudioDevice: MAC={bluetoothAddress:X12}");

        BLUETOOTH_ADDRESS addr = new BLUETOOTH_ADDRESS { ullLong = bluetoothAddress };
        bool anySuccess = false;

        // 1. Enable A2DP (music)
        Guid audioGuid = AudioSinkGuid;
        uint resultA2dp = BluetoothSetServiceState(IntPtr.Zero, ref addr, ref audioGuid, BLUETOOTH_SERVICE_ENABLE);
        CyanTooth.Platform.Helpers.DebugLogger.Log($"BluetoothServiceStateApi: A2DP enable result={resultA2dp}");
        if (resultA2dp == 0) anySuccess = true;

        // 2. Enable HFP (calls/microphone)
        Guid hfpGuid = HandsFreeGuid;
        uint resultHfp = BluetoothSetServiceState(IntPtr.Zero, ref addr, ref hfpGuid, BLUETOOTH_SERVICE_ENABLE);
        CyanTooth.Platform.Helpers.DebugLogger.Log($"BluetoothServiceStateApi: HFP enable result={resultHfp}");
        if (resultHfp == 0) anySuccess = true;

        // 3. Also try HSP
        Guid hspGuid = HeadsetGuid;
        uint resultHsp = BluetoothSetServiceState(IntPtr.Zero, ref addr, ref hspGuid, BLUETOOTH_SERVICE_ENABLE);
        CyanTooth.Platform.Helpers.DebugLogger.Log($"BluetoothServiceStateApi: HSP enable result={resultHsp}");
        if (resultHsp == 0) anySuccess = true;

        CyanTooth.Platform.Helpers.DebugLogger.Log($"BluetoothServiceStateApi.ConnectAudioDevice: anySuccess={anySuccess}");
        return anySuccess;
    }
}
