using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Wraps a keep-alive engine with device-change reconnect behavior.
/// </summary>
public sealed class ReconnectingAudioKeepAliveEngine : IAudioKeepAliveEngine
{
    private readonly IAudioKeepAliveEngine innerEngine;
    private readonly IDeviceChangeMonitor deviceChangeMonitor;
    private readonly IReconnectDelay reconnectDelay;
    private readonly TimeSpan reconnectInterval;
    private readonly SemaphoreSlim reconnectLock = new(1, 1);
    private readonly object stateLock = new();
    private CancellationTokenSource? lifecycleCancellation;
    private AudioDeviceSelection? activeSelection;
    private KeepAliveMode activeMode;
    private string? lastError;
    private DateTimeOffset? lastStateChange;
    private bool isStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReconnectingAudioKeepAliveEngine"/> class.
    /// </summary>
    public ReconnectingAudioKeepAliveEngine(
        IAudioKeepAliveEngine innerEngine,
        IDeviceChangeMonitor deviceChangeMonitor,
        IReconnectDelay reconnectDelay,
        TimeSpan reconnectInterval)
    {
        this.innerEngine = innerEngine ?? throw new ArgumentNullException(nameof(innerEngine));
        this.deviceChangeMonitor = deviceChangeMonitor ?? throw new ArgumentNullException(nameof(deviceChangeMonitor));
        this.reconnectDelay = reconnectDelay ?? throw new ArgumentNullException(nameof(reconnectDelay));
        this.reconnectInterval = reconnectInterval;
        this.deviceChangeMonitor.DeviceChanged += OnDeviceChanged;
    }

    /// <inheritdoc />
    public KeepAliveEngineState State { get; private set; } = KeepAliveEngineState.Stopped;

    /// <summary>
    /// Gets the number of successful reconnects.
    /// </summary>
    public int ReconnectCount { get; private set; }

    /// <inheritdoc />
    public event EventHandler<KeepAliveStateChangedEventArgs>? StateChanged;

    /// <inheritdoc />
    public async Task StartAsync(AudioDeviceSelection selection, KeepAliveMode mode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selection);
        cancellationToken.ThrowIfCancellationRequested();

        activeSelection = selection;
        activeMode = mode;
        lifecycleCancellation?.Dispose();
        lifecycleCancellation = new CancellationTokenSource();

        SetState(KeepAliveEngineState.Starting);

        try
        {
            await deviceChangeMonitor.StartAsync(cancellationToken).ConfigureAwait(false);
            await innerEngine.StartAsync(selection, mode, cancellationToken).ConfigureAwait(false);
            isStarted = true;
            lastError = null;
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

        if (!isStarted && State == KeepAliveEngineState.Stopped)
        {
            return;
        }

        SetState(KeepAliveEngineState.Stopping);
        isStarted = false;
        if (lifecycleCancellation is not null)
        {
            await lifecycleCancellation.CancelAsync().ConfigureAwait(false);
        }

        await deviceChangeMonitor.StopAsync(cancellationToken).ConfigureAwait(false);
        await innerEngine.StopAsync(cancellationToken).ConfigureAwait(false);
        SetState(KeepAliveEngineState.Stopped);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        deviceChangeMonitor.DeviceChanged -= OnDeviceChanged;
        if (lifecycleCancellation is not null)
        {
            await lifecycleCancellation.CancelAsync().ConfigureAwait(false);
            lifecycleCancellation.Dispose();
            lifecycleCancellation = null;
        }

        await innerEngine.DisposeAsync().ConfigureAwait(false);
        reconnectLock.Dispose();
        SetState(KeepAliveEngineState.Stopped);
    }

    /// <summary>
    /// Gets a diagnostics snapshot for reconnect state.
    /// </summary>
    public Task<EngineDiagnostics> GetDiagnosticsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new EngineDiagnostics(
            EndpointName: null,
            EndpointId: activeSelection?.DeviceId,
            Mode: activeMode,
            State: State,
            SampleRate: null,
            BitDepth: null,
            Channels: null,
            BufferDuration: null,
            Latency: null,
            FramesRendered: 0,
            ReconnectCount: ReconnectCount,
            LastError: lastError,
            LastStateChange: lastStateChange);

        return Task.FromResult(diagnostics);
    }

    private void OnDeviceChanged(object? sender, DeviceChangedEventArgs args)
    {
        if (!isStarted || activeSelection is null)
        {
            return;
        }

        _ = Task.Run(ReconnectAsync);
    }

    private async Task ReconnectAsync()
    {
        CancellationToken cancellationToken = lifecycleCancellation?.Token ?? CancellationToken.None;

        try
        {
            await reconnectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            if (!isStarted || activeSelection is null)
            {
                return;
            }

            SetState(KeepAliveEngineState.Reconnecting);
            await innerEngine.StopAsync(cancellationToken).ConfigureAwait(false);
            await reconnectDelay.DelayAsync(reconnectInterval, cancellationToken).ConfigureAwait(false);
            await innerEngine.StartAsync(activeSelection, activeMode, cancellationToken).ConfigureAwait(false);
            ReconnectCount++;
            lastError = null;
            SetState(KeepAliveEngineState.Running);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            lastError = ex.Message;
            SetState(KeepAliveEngineState.Faulted);
        }
        finally
        {
            reconnectLock.Release();
        }
    }

    private void SetState(KeepAliveEngineState state)
    {
        EventHandler<KeepAliveStateChangedEventArgs>? handler;
        KeepAliveStateChangedEventArgs args;

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
