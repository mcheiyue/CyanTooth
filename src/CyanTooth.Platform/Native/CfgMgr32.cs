using System.Runtime.InteropServices;

namespace CyanTooth.Platform.Native;

/// <summary>
/// CfgMgr32 P/Invoke definitions for reading Bluetooth battery levels
/// </summary>
public static class CfgMgr32
{
    private const string DllName = "CfgMgr32.dll";

    // Device Property Keys for Bluetooth Battery
    // DEVPKEY: {104EA319-6EE2-4701-BD47-8DDBF425BBE5}, pid=2
    public static readonly Guid PKEY_Bluetooth_BatteryLevel_Fmtid = new("104EA319-6EE2-4701-BD47-8DDBF425BBE5");
    public const uint PKEY_Bluetooth_BatteryLevel_Pid = 2;

    // DEVPKEY_Bluetooth_DeviceAddress - from Windows SDK
    // {2BD67D8B-8BEB-48D5-87E0-6CDA3428040A}, pid=1
    public static readonly Guid PKEY_Bluetooth_DeviceAddress_Fmtid = new("2BD67D8B-8BEB-48D5-87E0-6CDA3428040A");
    public const uint PKEY_Bluetooth_DeviceAddress_Pid = 1;

    // GUID_DEVCLASS_SYSTEM - for enumerating system devices
    public static readonly Guid GUID_DEVCLASS_SYSTEM = new("4D36E97D-E325-11CE-BFC1-08002BE10318");

    // CM_LOCATE_DEVNODE flags
    public const uint CM_LOCATE_DEVNODE_NORMAL = 0x00000000;
    public const uint CM_LOCATE_DEVNODE_PHANTOM = 0x00000001;
    public const uint CM_LOCATE_DEVNODE_CANCELREMOVE = 0x00000002;
    public const uint CM_LOCATE_DEVNODE_NOVALIDATION = 0x00000004;

    // Return codes
    public const uint CR_SUCCESS = 0x00000000;
    public const uint CR_NO_SUCH_DEVNODE = 0x0000000D;
    public const uint CR_NO_SUCH_VALUE = 0x00000025;

    // Property types
    public const uint DEVPROP_TYPE_BYTE = 0x00000003;
    public const uint DEVPROP_TYPE_STRING = 0x00000012;

    [StructLayout(LayoutKind.Sequential)]
    public struct DEVPROPKEY
    {
        public Guid fmtid;
        public uint pid;

        public DEVPROPKEY(Guid fmtid, uint pid)
        {
            this.fmtid = fmtid;
            this.pid = pid;
        }
    }

    [DllImport(DllName, CharSet = CharSet.Unicode)]
    public static extern uint CM_Locate_DevNodeW(
        out uint pdnDevInst,
        string pDeviceID,
        uint ulFlags);

    [DllImport(DllName, CharSet = CharSet.Unicode)]
    public static extern uint CM_Get_DevNode_PropertyW(
        uint dnDevInst,
        ref DEVPROPKEY PropertyKey,
        out uint PropertyType,
        IntPtr PropertyBuffer,
        ref uint PropertyBufferSize,
        uint ulFlags);

    [DllImport(DllName, CharSet = CharSet.Unicode)]
    public static extern uint CM_Get_Device_ID_Size(
        out uint pulLen,
        uint dnDevInst,
        uint ulFlags);

    [DllImport(DllName, CharSet = CharSet.Unicode)]
    public static extern uint CM_Get_Device_IDW(
        uint dnDevInst,
        [Out] char[] Buffer,
        uint BufferLen,
        uint ulFlags);

    [DllImport(DllName, CharSet = CharSet.Unicode)]
    public static extern uint CM_Get_Child(
        out uint pdnDevInst,
        uint dnDevInst,
        uint ulFlags);

    [DllImport(DllName, CharSet = CharSet.Unicode)]
    public static extern uint CM_Get_Sibling(
        out uint pdnDevInst,
        uint dnDevInst,
        uint ulFlags);

    [DllImport(DllName, CharSet = CharSet.Unicode)]
    public static extern uint CM_Get_Parent(
        out uint pdnDevInst,
        uint dnDevInst,
        uint ulFlags);
}
