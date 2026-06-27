namespace HDMIKeepAlive.Core.Services;

/// <summary>
/// Reports playback endpoint changes to reconnect-capable components.
/// </summary>
public interface IDeviceChangeMonitor
{
    /// <summary>Raised when Windows reports a meaningful playback endpoint change.</summary>
    event EventHandler<DeviceChangedEventArgs>? DeviceChanged;

    /// <summary>Starts monitoring device changes.</summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>Stops monitoring device changes.</summary>
    Task StopAsync(CancellationToken cancellationToken);
}
