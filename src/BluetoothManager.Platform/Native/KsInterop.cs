using System.Runtime.InteropServices;

namespace BluetoothManager.Platform.Native;

/// <summary>
/// KS (Kernel Streaming) Property structures for Bluetooth audio control
/// </summary>
public static class KsInterop
{
    // KSPROPSETID_BtAudio GUID
    // Used for connect/disconnect control via IKsControl
    public static readonly Guid KSPROPSETID_BtAudio = new("7FA06C40-B8F6-4C7E-8556-E8C33A12E54D");

    // Alternative GUID found in some implementations
    public static readonly Guid KSPROPSETID_BtAudio_Alt = new("602DCEAC-D13D-4DDA-807D-37456ABC210E");

    public enum KSPROPERTY_BTAUDIO : uint
    {
        KSPROPERTY_ONESHOT_RECONNECT = 0,
        KSPROPERTY_ONESHOT_DISCONNECT = 1
    }

    [Flags]
    public enum KSPROPERTY_TYPE : uint
    {
        KSPROPERTY_TYPE_GET = 0x00000001,
        KSPROPERTY_TYPE_SET = 0x00000002,
        KSPROPERTY_TYPE_TOPOLOGY = 0x10000000
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KSPROPERTY
    {
        public Guid Set;
        public uint Id;
        public uint Flags;

        public KSPROPERTY(Guid set, KSPROPERTY_BTAUDIO id, KSPROPERTY_TYPE flags)
        {
            Set = set;
            Id = (uint)id;
            Flags = (uint)flags;
        }
    }

    // IKsControl interface GUID
    public static readonly Guid IID_IKsControl = new("28F54685-06FD-11D2-B27A-00A0C9223196");
}

/// <summary>
/// IKsControl COM interface for Bluetooth audio control
/// </summary>
[ComImport]
[Guid("28F54685-06FD-11D2-B27A-00A0C9223196")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IKsControl
{
    [PreserveSig]
    int KsProperty(
        ref KsInterop.KSPROPERTY Property,
        uint PropertyLength,
        IntPtr PropertyData,
        uint DataLength,
        out uint BytesReturned);

    [PreserveSig]
    int KsMethod(
        IntPtr Method,
        uint MethodLength,
        IntPtr MethodData,
        uint DataLength,
        out uint BytesReturned);

    [PreserveSig]
    int KsEvent(
        IntPtr Event,
        uint EventLength,
        IntPtr EventData,
        uint DataLength,
        out uint BytesReturned);
}
