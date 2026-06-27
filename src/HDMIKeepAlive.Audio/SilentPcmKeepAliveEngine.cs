using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Keep-alive engine that renders zero-valued PCM buffers in WASAPI shared mode.
/// </summary>
public sealed class SilentPcmKeepAliveEngine : IAudioKeepAliveEngine
{
    private static readonly TimeSpan DefaultLoopDelay = TimeSpan.FromMilliseconds(10);

    private readonly ISilentPcmRenderSessionFactory sessionFactory;
    private readonly IRenderLoopClock renderLoopClock;
    private readonly object stateLock = new();
    private CancellationTokenSource? renderLoopCancellation;
    private Task? renderLoopTask;
    private ISilentPcmRenderSession? currentSession;
    private DateTimeOffset? lastStateChange;
    private string? lastError;

    /// <summary>
    /// Initializes a new instance of the <see cref="SilentPcmKeepAliveEngine"/> class.
    /// </summary>
    public SilentPcmKeepAliveEngine()
        : this(new WasapiSilentPcmRenderSessionFactory(), new SystemRenderLoopClock())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SilentPcmKeepAliveEngine"/> class.
    /// </summary>
    public SilentPcmKeepAliveEngine(
        ISilentPcmRenderSessionFactory sessionFactory,
        IRenderLoopClock renderLoopClock)
    {
        this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        this.renderLoopClock = renderLoopClock ?? throw new ArgumentNullException(nameof(renderLoopClock));
    }

    /// <inheritdoc />
    public KeepAliveEngineState State { get; private set; } = KeepAliveEngineState.Stopped;

    /// <inheritdoc />
    public event EventHandler<KeepAliveStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public async Task StartAsync(AudioDeviceSelection selection, KeepAliveMode mode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selection);
        cancellationToken.ThrowIfCancellationRequested();

        if (mode != KeepAliveMode.SilentPcm)
        {
            throw new NotSupportedException("Silent PCM engine only supports SilentPcm mode.");
        }

        if (State == KeepAliveEngineState.Running)
        {
            return;
        }

        SetState(KeepAliveEngineState.Starting);

        try
        {
            currentSession = await sessionFactory.OpenAsync(selection, cancellationToken).ConfigureAwait(false);
            lastError = null;
            renderLoopCancellation = new CancellationTokenSource();
            renderLoopTask = Task.Run(
                () => RenderLoopAsync(currentSession, renderLoopCancellation.Token),
                CancellationToken.None);
            SetState(KeepAliveEngineState.Running);
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            SetState(KeepAliveEngineState.Faulted);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (State == KeepAliveEngineState.Stopped)
        {
            return;
        }

        SetState(KeepAliveEngineState.Stopping);
        await StopRenderLoopAsync().ConfigureAwait(false);
        SetState(KeepAliveEngineState.Stopped);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopRenderLoopAsync().ConfigureAwait(false);
        SetState(KeepAliveEngineState.Stopped);
    }

    /// <summary>
    /// Gets a diagnostics snapshot for the current Silent PCM engine state.
    /// </summary>
    public Task<EngineDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ISilentPcmRenderSession? session = currentSession;

        var diagnostics = new EngineDiagnostics(
            EndpointName: session?.EndpointName,
            EndpointId: session?.EndpointId,
            Mode: KeepAliveMode.SilentPcm,
            State: State,
            SampleRate: session?.SampleRate,
            BitDepth: session?.BitDepth,
            Channels: session?.Channels,
            BufferDuration: session?.BufferDuration,
            Latency: session?.Latency,
            FramesRendered: session?.FramesRendered ?? 0,
            ReconnectCount: 0,
            LastError: lastError,
            LastStateChange: lastStateChange);

        return Task.FromResult(diagnostics);
    }

    private async Task RenderLoopAsync(ISilentPcmRenderSession session, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await session.RenderSilenceAsync(cancellationToken).ConfigureAwait(false);
                await renderLoopClock.DelayAsync(DefaultLoopDelay, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            SetState(KeepAliveEngineState.Faulted);
        }
    }

    private async Task StopRenderLoopAsync()
    {
        CancellationTokenSource? cancellation = renderLoopCancellation;
        Task? loopTask = renderLoopTask;
        ISilentPcmRenderSession? session = currentSession;

        renderLoopCancellation = null;
        renderLoopTask = null;
        currentSession = null;

        if (cancellation is not null)
        {
            await cancellation.CancelAsync().ConfigureAwait(false);
        }

        if (loopTask is not null)
        {
            await loopTask.ConfigureAwait(false);
        }

        cancellation?.Dispose();

        if (session is not null)
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void SetState(KeepAliveEngineState state)
    {
        EventHandler<KeepAliveStateChangedEventArgs>? handler;
        KeepAliveStateChangedEventArgs? args;

        lock (stateLock)
        {
            if (State == state)
            {
                return;
            }

            KeepAliveEngineState previousState = State;
            State = state;
            lastStateChange = DateTimeOffset.UtcNow;
            handler = StateChanged;
            args = new KeepAliveStateChangedEventArgs(previousState, state);
        }

        handler?.Invoke(this, args);
    }
}
