namespace HDMIKeepAlive.Audio;

/// <summary>
/// Registers and unregisters Windows audio device notifications.
/// </summary>
public interface IWindowsDeviceNotificationRegistration
{
    /// <summary>Registers device notifications.</summary>
    Task RegisterAsync(IWindowsDeviceNotificationCallback callback, CancellationToken cancellationToken);

    /// <summary>Unregisters device notifications.</summary>
    Task UnregisterAsync(CancellationToken cancellationToken);
}
