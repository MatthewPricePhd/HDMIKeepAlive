using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Models;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class SilentPcmKeepAliveEngineTests
{
    [Fact]
    public async Task StartAsync_OpensRenderSessionAndRendersSilence()
    {
        var factory = new FakeSilentPcmRenderSessionFactory();
        var clock = new FakeRenderLoopClock();
        await using var engine = new SilentPcmKeepAliveEngine(factory, clock);

        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.SilentPcm,
            CancellationToken.None);

        await factory.LastSession!.WaitForRenderAsync();

        Assert.Equal(KeepAliveEngineState.Running, engine.State);
        Assert.Equal(1, factory.OpenCount);
        Assert.True(factory.LastSession.RenderedOnlySilence);
        Assert.True(factory.LastSession.FramesRendered > 0);
    }

    [Fact]
    public async Task StopAsync_CancelsRenderLoopAndDisposesSession()
    {
        var factory = new FakeSilentPcmRenderSessionFactory();
        var clock = new FakeRenderLoopClock();
        await using var engine = new SilentPcmKeepAliveEngine(factory, clock);

        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.SilentPcm,
            CancellationToken.None);
        await factory.LastSession!.WaitForRenderAsync();

        await engine.StopAsync(CancellationToken.None);

        Assert.Equal(KeepAliveEngineState.Stopped, engine.State);
        Assert.True(factory.LastSession.Disposed);
    }

    [Fact]
    public async Task StartAsync_RejectsHoldOnlyMode()
    {
        var factory = new FakeSilentPcmRenderSessionFactory();
        await using var engine = new SilentPcmKeepAliveEngine(factory, new FakeRenderLoopClock());

        await Assert.ThrowsAsync<NotSupportedException>(() => engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None));

        Assert.Equal(0, factory.OpenCount);
        Assert.Equal(KeepAliveEngineState.Stopped, engine.State);
    }

    [Fact]
    public async Task GetDiagnosticsAsync_WhenRunningReportsRenderState()
    {
        var factory = new FakeSilentPcmRenderSessionFactory
        {
            EndpointId = "device-a",
            EndpointName = "Device A"
        };
        await using var engine = new SilentPcmKeepAliveEngine(factory, new FakeRenderLoopClock());

        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.SilentPcm,
            CancellationToken.None);
        await factory.LastSession!.WaitForRenderAsync();

        EngineDiagnostics diagnostics = await engine.GetDiagnosticsAsync(CancellationToken.None);

        Assert.Equal("device-a", diagnostics.EndpointId);
        Assert.Equal("Device A", diagnostics.EndpointName);
        Assert.Equal(KeepAliveMode.SilentPcm, diagnostics.Mode);
        Assert.Equal(KeepAliveEngineState.Running, diagnostics.State);
        Assert.Equal(48000, diagnostics.SampleRate);
        Assert.Equal(16, diagnostics.BitDepth);
        Assert.Equal(2, diagnostics.Channels);
        Assert.True(diagnostics.FramesRendered > 0);
        Assert.NotNull(diagnostics.BufferDuration);
        Assert.NotNull(diagnostics.Latency);
    }

    [Fact]
    public async Task StartAsync_WhenRenderFailsTransitionsToFaulted()
    {
        var factory = new FakeSilentPcmRenderSessionFactory
        {
            RenderFailure = new InvalidOperationException("render failed")
        };
        await using var engine = new SilentPcmKeepAliveEngine(factory, new FakeRenderLoopClock());

        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.SilentPcm,
            CancellationToken.None);

        await factory.LastSession!.WaitForRenderAttemptAsync();
        await WaitForStateAsync(engine, KeepAliveEngineState.Faulted);

        Assert.Equal(KeepAliveEngineState.Faulted, engine.State);
    }

    private static async Task WaitForStateAsync(
        SilentPcmKeepAliveEngine engine,
        KeepAliveEngineState expectedState)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (engine.State != expectedState)
        {
            timeout.Token.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromMilliseconds(10), timeout.Token);
        }
    }

    private sealed class FakeSilentPcmRenderSessionFactory : ISilentPcmRenderSessionFactory
    {
        public int OpenCount { get; private set; }

        public FakeSilentPcmRenderSession? LastSession { get; private set; }

        public string? EndpointId { get; init; }

        public string? EndpointName { get; init; }

        public Exception? RenderFailure { get; init; }

        public Task<ISilentPcmRenderSession> OpenAsync(
            AudioDeviceSelection selection,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenCount++;
            LastSession = new FakeSilentPcmRenderSession(EndpointId, EndpointName, RenderFailure);
            return Task.FromResult<ISilentPcmRenderSession>(LastSession);
        }
    }

    private sealed class FakeSilentPcmRenderSession : ISilentPcmRenderSession
    {
        private readonly TaskCompletionSource renderCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource renderAttemptCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Exception? renderFailure;

        public FakeSilentPcmRenderSession(string? endpointId, string? endpointName, Exception? renderFailure)
        {
            EndpointId = endpointId;
            EndpointName = endpointName;
            this.renderFailure = renderFailure;
        }

        public string? EndpointId { get; }

        public string? EndpointName { get; }

        public int? SampleRate => 48000;

        public int? BitDepth => 16;

        public int? Channels => 2;

        public TimeSpan? BufferDuration => TimeSpan.FromMilliseconds(20);

        public TimeSpan? Latency => TimeSpan.FromMilliseconds(10);

        public long FramesRendered { get; private set; }

        public bool RenderedOnlySilence { get; private set; }

        public bool Disposed { get; private set; }

        public Task WaitForRenderAsync()
        {
            return renderCompletion.Task;
        }

        public Task WaitForRenderAttemptAsync()
        {
            return renderAttemptCompletion.Task;
        }

        public Task RenderSilenceAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            renderAttemptCompletion.TrySetResult();
            if (renderFailure is not null)
            {
                throw renderFailure;
            }

            RenderedOnlySilence = true;
            FramesRendered += 480;
            renderCompletion.TrySetResult();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeRenderLoopClock : IRenderLoopClock
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        }
    }
}
