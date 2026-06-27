namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens a hold-only session for an already resolved playback endpoint ID.
/// </summary>
public interface IAudioEndpointSessionOpener
{
    /// <summary>Opens a hold-only session for the playback endpoint.</summary>
    Task<IAudioEndpointSession> OpenHoldSessionAsync(string endpointId, CancellationToken cancellationToken);
}
