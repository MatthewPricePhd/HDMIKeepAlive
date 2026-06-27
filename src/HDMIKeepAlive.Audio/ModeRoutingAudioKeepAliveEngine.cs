using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Routes keep-alive requests to the engine that implements the selected mode.
/// </summary>
public sealed class ModeRoutingAudioKeepAliveEngine : IAudioKeepAliveEngine
{
    private readonly IAudioKeepAliveEngine holdOnlyEngine;
    private readonly IAudioKeepAliveEngine silentPcmEngine;
    private IAudioKeepAliveEngine? activeEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModeRoutingAudioKeepAliveEngine"/> class.
    /// </summary>
    public ModeRoutingAudioKeepAliveEngine(
        IAudioKeepAliveEngine holdOnlyEngine,
        IAudioKeepAliveEngine silentPcmEngine)
    {
        this.holdOnlyEngine = holdOnlyEngine ?? throw new ArgumentNullException(nameof(holdOnlyEngine));
        this.silentPcmEngine = silentPcmEngine ?? throw new ArgumentNullException(nameof(silentPcmEngine));
        this.holdOnlyEngine.StateChanged += OnInnerStateChanged;
        this.silentPcmEngine.StateChanged += OnInnerStateChanged;
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

        IAudioKeepAliveEngine nextEngine = mode switch
        {
            KeepAliveMode.HoldOnly => holdOnlyEngine,
            KeepAliveMode.SilentPcm => silentPcmEngine,
            KeepAliveMode.Auto => throw new NotSupportedException("Auto mode is not implemented before adaptive mode."),
            _ => throw new NotSupportedException($"Unsupported keep-alive mode: {mode}.")
        };

        if (activeEngine is not null && !ReferenceEquals(activeEngine, nextEngine))
        {
            await activeEngine.StopAsync(cancellationToken).ConfigureAwait(false);
        }

        activeEngine = nextEngine;
        await activeEngine.StartAsync(selection, mode, cancellationToken).ConfigureAwait(false);
        SetState(activeEngine.State);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (activeEngine is null)
        {
            SetState(KeepAliveEngineState.Stopped);
            return;
        }

        await activeEngine.StopAsync(cancellationToken).ConfigureAwait(false);
        SetState(activeEngine.State);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        holdOnlyEngine.StateChanged -= OnInnerStateChanged;
        silentPcmEngine.StateChanged -= OnInnerStateChanged;
        await holdOnlyEngine.DisposeAsync().ConfigureAwait(false);
        await silentPcmEngine.DisposeAsync().ConfigureAwait(false);
        SetState(KeepAliveEngineState.Stopped);
    }

    private void OnInnerStateChanged(object? sender, KeepAliveStateChangedEventArgs args)
    {
        if (sender is IAudioKeepAliveEngine engine && ReferenceEquals(engine, activeEngine))
        {
            SetState(args.CurrentState);
        }
    }

    private void SetState(KeepAliveEngineState state)
    {
        if (State == state)
        {
            return;
        }

        KeepAliveEngineState previous = State;
        State = state;
        StateChanged?.Invoke(this, new KeepAliveStateChangedEventArgs(previous, state));
    }
}
