using HDMIKeepAlive.Core.Services;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Monitors Windows playback device changes.
/// </summary>
public sealed class WindowsDeviceChangeMonitor : IDeviceChangeMonitor, IWindowsDeviceNotificationCallback
{
    private readonly IWindowsDeviceNotificationRegistration registration;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsDeviceChangeMonitor"/> class.
    /// </summary>
    public WindowsDeviceChangeMonitor()
        : this(new WindowsDeviceNotificationRegistration())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsDeviceChangeMonitor"/> class.
    /// </summary>
    public WindowsDeviceChangeMonitor(IWindowsDeviceNotificationRegistration registration)
    {
        this.registration = registration ?? throw new ArgumentNullException(nameof(registration));
    }

    /// <inheritdoc />
    public event EventHandler<DeviceChangedEventArgs>? DeviceChanged;

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return registration.RegisterAsync(this, cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return registration.UnregisterAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void NotifyDeviceChanged(string reason)
    {
        DeviceChanged?.Invoke(this, new DeviceChangedEventArgs(device: null, reason));
    }
}
