namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens Silent PCM render sessions for resolved endpoint IDs.
/// </summary>
public interface ISilentPcmRenderSessionOpener
{
    /// <summary>Opens a shared-mode Silent PCM render session.</summary>
    Task<ISilentPcmRenderSession> OpenAsync(string endpointId, CancellationToken cancellationToken);
}
