using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Core.Services;

/// <summary>
/// Event data for playback endpoint changes.
/// </summary>
public sealed class DeviceChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceChangedEventArgs"/> class.
    /// </summary>
    public DeviceChangedEventArgs(AudioDeviceInfo? device, string reason)
    {
        Device = device;
        Reason = reason;
    }

    /// <summary>Gets the device associated with the change, if available.</summary>
    public AudioDeviceInfo? Device { get; }

    /// <summary>Gets a short reason for the change.</summary>
    public string Reason { get; }
}
