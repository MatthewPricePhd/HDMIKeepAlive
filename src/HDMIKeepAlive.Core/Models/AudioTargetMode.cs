namespace HDMIKeepAlive.Core.Models;

/// <summary>
/// Defines how HDMIKeepAlive resolves the playback endpoint to target.
/// </summary>
public enum AudioTargetMode
{
    /// <summary>Use the current default playback device.</summary>
    DefaultDevice,

    /// <summary>Use a specific playback endpoint ID.</summary>
    SpecificDevice,

    /// <summary>Limit automatic selection to HDMI-like playback devices.</summary>
    HdmiDevicesOnly
}
