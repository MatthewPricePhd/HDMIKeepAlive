namespace HDMIKeepAlive.Audio;

/// <summary>
/// Supplies playback endpoint snapshots from the operating system or a test double.
/// </summary>
public interface IAudioEndpointSource
{
    /// <summary>Gets playback endpoints.</summary>
    Task<IReadOnlyList<AudioEndpointSnapshot>> GetPlaybackEndpointsAsync(CancellationToken cancellationToken);

    /// <summary>Gets the default playback endpoint ID, if available.</summary>
    Task<string?> GetDefaultPlaybackEndpointIdAsync(CancellationToken cancellationToken);
}
