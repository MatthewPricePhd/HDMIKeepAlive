using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Keep-alive engine that opens and holds a playback endpoint without rendering PCM.
/// </summary>
public sealed class EndpointHoldKeepAliveEngine : IAudioKeepAliveEngine
{
    private readonly IAudioEndpointSessionFactory sessionFactory;
    private IAudioEndpointSession? currentSession;
    private KeepAliveMode currentMode = KeepAliveMode.HoldOnly;
    private DateTimeOffset? lastStateChange;

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointHoldKeepAliveEngine"/> class.
    /// </summary>
    public EndpointHoldKeepAliveEngine()
        : this(new WasapiEndpointHoldSessionFactory())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EndpointHoldKeepAliveEngine"/> class.
    /// </summary>
    public EndpointHoldKeepAliveEngine(IAudioEndpointSessionFactory sessionFactory)
    {
        this.sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
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

        if (mode != KeepAliveMode.HoldOnly)
        {
            throw new NotSupportedException("Only HoldOnly mode is implemented before the Silent PCM milestone.");
        }

        if (State == KeepAliveEngineState.Running)
        {
            return;
        }

        SetState(KeepAliveEngineState.Starting);

        try
        {
            currentSession = await sessionFactory.OpenAsync(selection, mode, cancellationToken)
                .ConfigureAwait(false);
            currentMode = mode;
            SetState(KeepAliveEngineState.Running);
        }
        catch
        {
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
        await DisposeCurrentSessionAsync().ConfigureAwait(false);
        SetState(KeepAliveEngineState.Stopped);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeCurrentSessionAsync().ConfigureAwait(false);
        SetState(KeepAliveEngineState.Stopped);
    }

    /// <summary>
    /// Gets a diagnostics snapshot for the current hold-only engine state.
    /// </summary>
    public Task<EngineDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new EngineDiagnostics(
            EndpointName: currentSession?.EndpointName,
            EndpointId: currentSession?.EndpointId,
            Mode: currentMode,
            State: State,
            SampleRate: null,
            BitDepth: null,
            Channels: null,
            BufferDuration: null,
            Latency: null,
            FramesRendered: 0,
            ReconnectCount: 0,
            LastError: null,
            LastStateChange: lastStateChange);

        return Task.FromResult(diagnostics);
    }

    private async ValueTask DisposeCurrentSessionAsync()
    {
        IAudioEndpointSession? session = currentSession;
        currentSession = null;

        if (session is not null)
        {
            await session.DisposeAsync().ConfigureAwait(false);
        }
    }

    private void SetState(KeepAliveEngineState state)
    {
        if (State == state)
        {
            return;
        }

        KeepAliveEngineState previousState = State;
        State = state;
        lastStateChange = DateTimeOffset.UtcNow;
        StateChanged?.Invoke(this, new KeepAliveStateChangedEventArgs(previousState, state));
    }
}
