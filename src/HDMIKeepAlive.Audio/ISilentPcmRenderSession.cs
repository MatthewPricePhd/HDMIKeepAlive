namespace HDMIKeepAlive.Audio;

/// <summary>
/// Represents an open shared-mode render session that can write digital silence.
/// </summary>
public interface ISilentPcmRenderSession : IAsyncDisposable
{
    /// <summary>Gets the endpoint ID.</summary>
    string? EndpointId { get; }

    /// <summary>Gets the endpoint friendly name, if available.</summary>
    string? EndpointName { get; }

    /// <summary>Gets the sample rate of the render format.</summary>
    int? SampleRate { get; }

    /// <summary>Gets the bit depth of the render format.</summary>
    int? BitDepth { get; }

    /// <summary>Gets the channel count of the render format.</summary>
    int? Channels { get; }

    /// <summary>Gets the render buffer duration.</summary>
    TimeSpan? BufferDuration { get; }

    /// <summary>Gets the stream latency.</summary>
    TimeSpan? Latency { get; }

    /// <summary>Gets the total frames rendered by this session.</summary>
    long FramesRendered { get; }

    /// <summary>Writes one period of digital silence.</summary>
    Task RenderSilenceAsync(CancellationToken cancellationToken);
}
