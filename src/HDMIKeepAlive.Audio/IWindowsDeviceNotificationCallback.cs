namespace HDMIKeepAlive.Audio;

/// <summary>
/// Receives normalized Windows audio device change notifications.
/// </summary>
public interface IWindowsDeviceNotificationCallback
{
    /// <summary>Reports a playback device change.</summary>
    void NotifyDeviceChanged(string reason);
}
