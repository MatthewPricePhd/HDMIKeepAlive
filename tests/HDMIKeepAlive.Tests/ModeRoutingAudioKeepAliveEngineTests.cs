using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class ModeRoutingAudioKeepAliveEngineTests
{
    [Fact]
    public async Task StartAsync_WithHoldOnlyStartsHoldEngine()
    {
        var holdEngine = new FakeEngine();
        var silentEngine = new FakeEngine();
        await using var router = new ModeRoutingAudioKeepAliveEngine(holdEngine, silentEngine);
        var selection = new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null);

        await router.StartAsync(selection, KeepAliveMode.HoldOnly, CancellationToken.None);

        Assert.Equal(1, holdEngine.StartCount);
        Assert.Equal(0, silentEngine.StartCount);
        Assert.Equal(KeepAliveMode.HoldOnly, holdEngine.LastMode);
        Assert.Equal(KeepAliveEngineState.Running, router.State);
    }

    [Fact]
    public async Task StartAsync_WithSilentPcmStartsSilentEngine()
    {
        var holdEngine = new FakeEngine();
        var silentEngine = new FakeEngine();
        await using var router = new ModeRoutingAudioKeepAliveEngine(holdEngine, silentEngine);

        await router.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.SilentPcm,
            CancellationToken.None);

        Assert.Equal(0, holdEngine.StartCount);
        Assert.Equal(1, silentEngine.StartCount);
        Assert.Equal(KeepAliveMode.SilentPcm, silentEngine.LastMode);
        Assert.Equal(KeepAliveEngineState.Running, router.State);
    }

    [Fact]
    public async Task StopAsync_StopsActiveEngine()
    {
        var holdEngine = new FakeEngine();
        var silentEngine = new FakeEngine();
        await using var router = new ModeRoutingAudioKeepAliveEngine(holdEngine, silentEngine);

        await router.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.SilentPcm,
            CancellationToken.None);
        await router.StopAsync(CancellationToken.None);

        Assert.Equal(0, holdEngine.StopCount);
        Assert.Equal(1, silentEngine.StopCount);
        Assert.Equal(KeepAliveEngineState.Stopped, router.State);
    }

    [Fact]
    public async Task StartAsync_WithAutoThrowsUntilAutoMilestone()
    {
        await using var router = new ModeRoutingAudioKeepAliveEngine(new FakeEngine(), new FakeEngine());

        await Assert.ThrowsAsync<NotSupportedException>(() => router.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.Auto,
            CancellationToken.None));
    }

    private sealed class FakeEngine : IAudioKeepAliveEngine
    {
        public KeepAliveEngineState State { get; private set; } = KeepAliveEngineState.Stopped;

        public event EventHandler<KeepAliveStateChangedEventArgs>? StateChanged;

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public KeepAliveMode? LastMode { get; private set; }

        public Task StartAsync(
            AudioDeviceSelection selection,
            KeepAliveMode mode,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartCount++;
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
            KeepAliveEngineState previous = State;
            State = state;
            StateChanged?.Invoke(this, new KeepAliveStateChangedEventArgs(previous, state));
        }
    }
}
