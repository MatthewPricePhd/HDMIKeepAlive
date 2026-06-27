using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Core.Services;

/// <summary>
/// Owns the selected playback endpoint lifecycle.
/// </summary>
public interface IAudioKeepAliveEngine : IAsyncDisposable
{
    /// <summary>Gets the current engine state.</summary>
    KeepAliveEngineState State { get; }

    /// <summary>Raised when the engine state changes.</summary>
    event EventHandler<KeepAliveStateChangedEventArgs>? StateChanged;

    /// <summary>Starts the keep-alive engine.</summary>
    Task StartAsync(AudioDeviceSelection selection, KeepAliveMode mode, CancellationToken cancellationToken);

    /// <summary>Stops the keep-alive engine.</summary>
    Task StopAsync(CancellationToken cancellationToken);
}
