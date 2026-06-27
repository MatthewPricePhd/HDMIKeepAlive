namespace HDMIKeepAlive.Audio;

/// <summary>
/// Represents an open playback endpoint session held by the keep-alive engine.
/// </summary>
public interface IAudioEndpointSession : IAsyncDisposable
{
    /// <summary>Gets the held endpoint ID.</summary>
    string? EndpointId { get; }

    /// <summary>Gets the held endpoint friendly name, if available.</summary>
    string? EndpointName { get; }
}
