namespace HDMIKeepAlive.Audio;

/// <summary>
/// Shared-mode render session that writes zero-valued PCM frames.
/// </summary>
public sealed class SilentPcmRenderSession : ISilentPcmRenderSession
{
    private readonly IWasapiRenderClient renderClient;
    private readonly SilentPcmRenderOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="SilentPcmRenderSession"/> class.
    /// </summary>
    public SilentPcmRenderSession(
        string endpointId,
        string? endpointName,
        IWasapiRenderClient renderClient)
        : this(endpointId, endpointName, renderClient, new SilentPcmRenderOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SilentPcmRenderSession"/> class.
    /// </summary>
    public SilentPcmRenderSession(
        string endpointId,
        string? endpointName,
        IWasapiRenderClient renderClient,
        SilentPcmRenderOptions options)
    {
        EndpointId = endpointId;
        EndpointName = endpointName;
        this.renderClient = renderClient ?? throw new ArgumentNullException(nameof(renderClient));
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public string? EndpointId { get; }

    /// <inheritdoc />
    public string? EndpointName { get; }

    /// <inheritdoc />
    public int? SampleRate => renderClient.Format.SampleRate;

    /// <inheritdoc />
    public int? BitDepth => renderClient.Format.BitDepth;

    /// <inheritdoc />
    public int? Channels => renderClient.Format.Channels;

    /// <inheritdoc />
    public TimeSpan? BufferDuration => renderClient.BufferDuration;

    /// <inheritdoc />
    public TimeSpan? Latency => renderClient.Latency;

    /// <inheritdoc />
    public long FramesRendered { get; private set; }

    /// <inheritdoc />
    public Task RenderSilenceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        uint bufferFrames = renderClient.GetBufferFrameCount();
        uint queuedFrames = renderClient.GetCurrentPadding();
        uint availableFrames = bufferFrames > queuedFrames ? bufferFrames - queuedFrames : 0;

        if (availableFrames == 0)
        {
            return Task.CompletedTask;
        }

        renderClient.Render(availableFrames, options.Signal);
        FramesRendered += availableFrames;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        renderClient.Stop();
        await renderClient.DisposeAsync().ConfigureAwait(false);
    }
}
