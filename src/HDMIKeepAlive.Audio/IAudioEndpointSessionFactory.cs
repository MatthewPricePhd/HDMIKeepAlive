using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens playback endpoint sessions for a keep-alive strategy.
/// </summary>
public interface IAudioEndpointSessionFactory
{
    /// <summary>Opens the selected endpoint for the requested keep-alive mode.</summary>
    Task<IAudioEndpointSession> OpenAsync(
        AudioDeviceSelection selection,
        KeepAliveMode mode,
        CancellationToken cancellationToken);
}
