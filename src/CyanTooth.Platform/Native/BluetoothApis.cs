using CyanTooth.Platform.Helpers;


using System.Runtime.InteropServices;


namespace CyanTooth.Platform.Native;

/// <summary>
/// Bluetooth API P/Invoke definitions
/// </summary>
public static class BluetoothApis
{
    private const string DllName = "bthprops.cpl";
    private const string BluetoothDll = "BluetoothApis.dll";

    // Bluetooth authentication callback
    public delegate bool BluetoothAuthenticationCallbackEx(
        IntPtr pvParam,
        ref BLUETOOTH_AUTHENTICATION_CALLBACK_PARAMS pAuthCallbackParams);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct BLUETOOTH_DEVICE_INFO
    {
        public uint dwSize;
        public ulong Address;
        public uint ulClassofDevice;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fConnected;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fRemembered;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fAuthenticated;
        public SYSTEMTIME stLastSeen;
        public SYSTEMTIME stLastUsed;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)]
        public string szName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEMTIME
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BLUETOOTH_AUTHENTICATION_CALLBACK_PARAMS
    {
        public BLUETOOTH_DEVICE_INFO deviceInfo;
        public uint authenticationMethod;
        public uint ioCapability;
        public uint authenticationRequirements;
        public uint numericValue;
        public uint passkey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct BLUETOOTH_DEVICE_SEARCH_PARAMS
    {
        public uint dwSize;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fReturnAuthenticated;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fReturnRemembered;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fReturnUnknown;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fReturnConnected;
        [MarshalAs(UnmanagedType.Bool)]
        public bool fIssueInquiry;
        public byte cTimeoutMultiplier;
        public IntPtr hRadio;
    }

    [DllImport(BluetoothDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr BluetoothFindFirstDevice(
        ref BLUETOOTH_DEVICE_SEARCH_PARAMS pbtsp,
        ref BLUETOOTH_DEVICE_INFO pbtdi);

    [DllImport(BluetoothDll, CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BluetoothFindNextDevice(
        IntPtr hFind,
        ref BLUETOOTH_DEVICE_INFO pbtdi);

    [DllImport(BluetoothDll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BluetoothFindDeviceClose(IntPtr hFind);

    [DllImport(BluetoothDll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint BluetoothGetDeviceInfo(
        IntPtr hRadio,
        ref BLUETOOTH_DEVICE_INFO pbtdi);

    [DllImport(BluetoothDll, SetLastError = true)]
    public static extern uint BluetoothRemoveDevice(ref ulong pAddress);

    // Format MAC address from ulong
    public static string FormatMacAddress(ulong address)
    {
        byte[] bytes = BitConverter.GetBytes(address);
        return $"{bytes[5]:X2}:{bytes[4]:X2}:{bytes[3]:X2}:{bytes[2]:X2}:{bytes[1]:X2}:{bytes[0]:X2}";
    }

    // Parse MAC address to ulong
    public static ulong ParseMacAddress(string macAddress)
    {
        string[] parts = macAddress.Replace("-", ":").Split(':');
        if (parts.Length != 6)
            throw new ArgumentException("Invalid MAC address format", nameof(macAddress));

        byte[] bytes = new byte[8];
        for (int i = 0; i < 6; i++)
        {
            bytes[5 - i] = Convert.ToByte(parts[i], 16);
        }
        return BitConverter.ToUInt64(bytes, 0);
    }
}
