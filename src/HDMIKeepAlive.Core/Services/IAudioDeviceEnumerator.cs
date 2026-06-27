using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Core.Services;

/// <summary>
/// Enumerates playback endpoints without exposing Windows audio implementation details.
/// </summary>
public interface IAudioDeviceEnumerator
{
    /// <summary>Gets available playback devices.</summary>
    Task<IReadOnlyList<AudioDeviceInfo>> GetPlaybackDevicesAsync(CancellationToken cancellationToken);

    /// <summary>Gets the current default playback device, if available.</summary>
    Task<AudioDeviceInfo?> GetDefaultPlaybackDeviceAsync(CancellationToken cancellationToken);
}
