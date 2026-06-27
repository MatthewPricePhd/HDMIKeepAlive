namespace HDMIKeepAlive.Core.Models;

/// <summary>
/// Playback endpoint state reported by Windows audio enumeration.
/// </summary>
public enum AudioDeviceState
{
    /// <summary>The device is active and available.</summary>
    Active,

    /// <summary>The device is disabled.</summary>
    Disabled,

    /// <summary>The device is not currently present.</summary>
    NotPresent,

    /// <summary>The device is unplugged.</summary>
    Unplugged,

    /// <summary>The device state could not be determined.</summary>
    Unknown
}
