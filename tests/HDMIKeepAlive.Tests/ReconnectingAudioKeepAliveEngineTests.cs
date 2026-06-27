using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class ReconnectingAudioKeepAliveEngineTests
{
    [Fact]
    public async Task StartAsync_StartsInnerEngineAndDeviceMonitor()
    {
        var innerEngine = new FakeAudioKeepAliveEngine();
        var monitor = new FakeDeviceChangeMonitor();
        await using var engine = new ReconnectingAudioKeepAliveEngine(
            innerEngine,
            monitor,
            new ImmediateReconnectDelay(),
            TimeSpan.FromMilliseconds(1));

        var selection = new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null);
        await engine.StartAsync(selection, KeepAliveMode.HoldOnly, CancellationToken.None);

        Assert.Equal(1, innerEngine.StartCount);
        Assert.Equal(selection, innerEngine.LastSelection);
        Assert.Equal(KeepAliveMode.HoldOnly, innerEngine.LastMode);
        Assert.True(monitor.Started);
        Assert.Equal(KeepAliveEngineState.Running, engine.State);
    }

    [Fact]
    public async Task DeviceChanged_WhenRunningRestartsInnerEngineAndIncrementsReconnectCount()
    {
        var innerEngine = new FakeAudioKeepAliveEngine();
        var monitor = new FakeDeviceChangeMonitor();
        await using var engine = new ReconnectingAudioKeepAliveEngine(
            innerEngine,
            monitor,
            new ImmediateReconnectDelay(),
            TimeSpan.FromMilliseconds(1));

        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None);

        monitor.RaiseDeviceChanged("device removed");
        await WaitForAsync(() => innerEngine.StartCount == 2);

        Assert.Equal(1, innerEngine.StopCount);
        Assert.Equal(1, engine.ReconnectCount);
        Assert.Equal(KeepAliveEngineState.Running, engine.State);
    }

    [Fact]
    public async Task DeviceChanged_WhenStoppedDoesNotRestartInnerEngine()
    {
        var innerEngine = new FakeAudioKeepAliveEngine();
        var monitor = new FakeDeviceChangeMonitor();
        await using var engine = new ReconnectingAudioKeepAliveEngine(
            innerEngine,
            monitor,
            new ImmediateReconnectDelay(),
            TimeSpan.FromMilliseconds(1));

        monitor.RaiseDeviceChanged("device removed");
        await Task.Delay(25);

        Assert.Equal(0, innerEngine.StartCount);
        Assert.Equal(0, innerEngine.StopCount);
    }

    [Fact]
    public async Task StopAsync_StopsMonitorAndInnerEngine()
    {
        var innerEngine = new FakeAudioKeepAliveEngine();
        var monitor = new FakeDeviceChangeMonitor();
        await using var engine = new ReconnectingAudioKeepAliveEngine(
            innerEngine,
            monitor,
            new ImmediateReconnectDelay(),
            TimeSpan.FromMilliseconds(1));

        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None);
        await engine.StopAsync(CancellationToken.None);

        Assert.Equal(1, innerEngine.StopCount);
        Assert.True(monitor.Stopped);
        Assert.Equal(KeepAliveEngineState.Stopped, engine.State);
    }

    [Fact]
    public async Task DeviceChanged_WhenRestartFailsTransitionsToFaulted()
    {
        var innerEngine = new FakeAudioKeepAliveEngine { FailSecondStart = true };
        var monitor = new FakeDeviceChangeMonitor();
        await using var engine = new ReconnectingAudioKeepAliveEngine(
            innerEngine,
            monitor,
            new ImmediateReconnectDelay(),
            TimeSpan.FromMilliseconds(1));

        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None);

        monitor.RaiseDeviceChanged("device removed");
        await WaitForAsync(() => engine.State == KeepAliveEngineState.Faulted);

        EngineDiagnostics diagnostics = await engine.GetDiagnosticsAsync(CancellationToken.None);
        Assert.Equal(KeepAliveEngineState.Faulted, diagnostics.State);
        Assert.Contains("restart failed", diagnostics.LastError, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetDiagnosticsAsync_IncludesReconnectCount()
    {
        var innerEngine = new FakeAudioKeepAliveEngine();
        var monitor = new FakeDeviceChangeMonitor();
        await using var engine = new ReconnectingAudioKeepAliveEngine(
            innerEngine,
            monitor,
            new ImmediateReconnectDelay(),
            TimeSpan.FromMilliseconds(1));

        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.SilentPcm,
            CancellationToken.None);
        monitor.RaiseDeviceChanged("device removed");
        await WaitForAsync(() => engine.ReconnectCount == 1);

        EngineDiagnostics diagnostics = await engine.GetDiagnosticsAsync(CancellationToken.None);

        Assert.Equal(1, diagnostics.ReconnectCount);
        Assert.Equal(KeepAliveMode.SilentPcm, diagnostics.Mode);
        Assert.Equal(KeepAliveEngineState.Running, diagnostics.State);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }
    }

    private sealed class FakeAudioKeepAliveEngine : IAudioKeepAliveEngine
    {
        public KeepAliveEngineState State { get; private set; } = KeepAliveEngineState.Stopped;

        public event EventHandler<KeepAliveStateChangedEventArgs>? StateChanged;

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public AudioDeviceSelection? LastSelection { get; private set; }

        public KeepAliveMode? LastMode { get; private set; }

        public bool FailSecondStart { get; init; }

        public Task StartAsync(
            AudioDeviceSelection selection,
            KeepAliveMode mode,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartCount++;
            if (FailSecondStart && StartCount > 1)
            {
                throw new InvalidOperationException("restart failed");
            }

            LastSelection = selection;
            LastMode = mode;
            SetState(KeepAliveEngineState.Running);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StopCount++;
            SetState(KeepAliveEngineState.Stopped);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            SetState(KeepAliveEngineState.Stopped);
            return ValueTask.CompletedTask;
        }

        private void SetState(KeepAliveEngineState state)
        {
            if (State == state)
            {
                return;
            }

            KeepAliveEngineState previousState = State;
            State = state;
            StateChanged?.Invoke(this, new KeepAliveStateChangedEventArgs(previousState, state));
        }
    }

    private sealed class FakeDeviceChangeMonitor : IDeviceChangeMonitor
    {
        public event EventHandler<DeviceChangedEventArgs>? DeviceChanged;

        public bool Started { get; private set; }

        public bool Stopped { get; private set; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Started = true;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Stopped = true;
            return Task.CompletedTask;
        }

        public void RaiseDeviceChanged(string reason)
        {
            DeviceChanged?.Invoke(this, new DeviceChangedEventArgs(null, reason));
        }
    }

    private sealed class ImmediateReconnectDelay : IReconnectDelay
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
