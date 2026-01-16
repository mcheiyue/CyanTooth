using CyanTooth.Platform.Helpers;



using System.Runtime.InteropServices;
using CyanTooth.Platform.Native;


namespace CyanTooth.Platform.Audio;

/// <summary>
/// Controls Bluetooth audio connection/disconnection using IKsControl
/// </summary>
public class BluetoothAudioConnector
{
    /// <summary>
    /// Connects a Bluetooth audio device
    /// </summary>
    /// <param name="ksControl">The IKsControl interface for the audio endpoint</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool Connect(IKsControl ksControl)
    {
        return SendKsProperty(ksControl, KsInterop.KSPROPERTY_BTAUDIO.KSPROPERTY_ONESHOT_RECONNECT);
    }

    /// <summary>
    /// Disconnects a Bluetooth audio device
    /// </summary>
    /// <param name="ksControl">The IKsControl interface for the audio endpoint</param>
    /// <returns>True if successful, false otherwise</returns>
    public bool Disconnect(IKsControl ksControl)
    {
        return SendKsProperty(ksControl, KsInterop.KSPROPERTY_BTAUDIO.KSPROPERTY_ONESHOT_DISCONNECT);
    }

    /// <summary>
    /// Connects a Bluetooth audio device by its endpoint info
    /// </summary>
    public bool Connect(AudioEndpointInfo endpoint)
    {
        if (endpoint.KsControl == null) return false;
        return Connect(endpoint.KsControl);
    }

    /// <summary>
    /// Disconnects a Bluetooth audio device by its endpoint info
    /// </summary>
    public bool Disconnect(AudioEndpointInfo endpoint)
    {
        if (endpoint.KsControl == null) return false;
        return Disconnect(endpoint.KsControl);
    }

    /// <summary>
    /// Sends a KS property to the Bluetooth audio driver
    /// </summary>
    private static bool SendKsProperty(IKsControl ksControl, KsInterop.KSPROPERTY_BTAUDIO btAudioProperty)
    {
        var property = new KsInterop.KSPROPERTY(
            KsInterop.KSPROPSETID_BtAudio,
            btAudioProperty,
            KsInterop.KSPROPERTY_TYPE.KSPROPERTY_TYPE_GET);

        try
        {
            int hr = ksControl.KsProperty(
                ref property,
                (uint)Marshal.SizeOf<KsInterop.KSPROPERTY>(),
                IntPtr.Zero,
                0,
                out _);

            // If the first GUID doesn't work, try the alternative
            if (hr != 0)
            {
                property = new KsInterop.KSPROPERTY(
                    KsInterop.KSPROPSETID_BtAudio_Alt,
                    btAudioProperty,
                    KsInterop.KSPROPERTY_TYPE.KSPROPERTY_TYPE_GET);

                hr = ksControl.KsProperty(
                    ref property,
                    (uint)Marshal.SizeOf<KsInterop.KSPROPERTY>(),
                    IntPtr.Zero,
                    0,
                    out _);
            }

            return hr == 0;
        }
        catch (COMException)
        {
            return false;
        }
    }
}
