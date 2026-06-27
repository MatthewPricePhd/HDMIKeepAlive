namespace HDMIKeepAlive.Audio;

/// <summary>
/// Minimal WASAPI render-client abstraction used by Silent PCM rendering.
/// </summary>
public interface IWasapiRenderClient : IAsyncDisposable
{
    /// <summary>Gets the render format.</summary>
    AudioRenderFormat Format { get; }

    /// <summary>Gets the full render buffer duration.</summary>
    TimeSpan BufferDuration { get; }

    /// <summary>Gets the stream latency.</summary>
    TimeSpan Latency { get; }

    /// <summary>Gets the endpoint buffer frame count.</summary>
    uint GetBufferFrameCount();

    /// <summary>Gets the number of frames already queued in the endpoint buffer.</summary>
    uint GetCurrentPadding();

    /// <summary>Renders zero-valued PCM frames.</summary>
    void RenderSilence(uint frameCount);

    /// <summary>Stops the render stream.</summary>
    void Stop();
}
