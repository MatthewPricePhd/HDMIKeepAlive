using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class EndpointHoldKeepAliveEngineTests
{
    [Fact]
    public async Task StartAsync_OpensHoldSessionAndTransitionsToRunning()
    {
        var sessionFactory = new FakeAudioEndpointSessionFactory();
        await using var engine = new EndpointHoldKeepAliveEngine(sessionFactory);
        var observedStates = new List<KeepAliveEngineState>();
        engine.StateChanged += (_, args) => observedStates.Add(args.CurrentState);

        var selection = new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null);
        await engine.StartAsync(selection, KeepAliveMode.HoldOnly, CancellationToken.None);

        Assert.Equal(KeepAliveEngineState.Running, engine.State);
        Assert.Equal(selection, sessionFactory.LastSelection);
        Assert.Equal(KeepAliveMode.HoldOnly, sessionFactory.LastMode);
        Assert.Equal([KeepAliveEngineState.Starting, KeepAliveEngineState.Running], observedStates);
    }

    [Fact]
    public async Task StopAsync_DisposesCurrentSessionAndTransitionsToStopped()
    {
        var sessionFactory = new FakeAudioEndpointSessionFactory();
        await using var engine = new EndpointHoldKeepAliveEngine(sessionFactory);
        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None);

        await engine.StopAsync(CancellationToken.None);

        Assert.Equal(KeepAliveEngineState.Stopped, engine.State);
        Assert.True(sessionFactory.LastSession?.Disposed);
    }

    [Fact]
    public async Task StopAsync_WhenAlreadyStoppedDoesNotOpenOrDisposeSession()
    {
        var sessionFactory = new FakeAudioEndpointSessionFactory();
        await using var engine = new EndpointHoldKeepAliveEngine(sessionFactory);

        await engine.StopAsync(CancellationToken.None);

        Assert.Equal(KeepAliveEngineState.Stopped, engine.State);
        Assert.Equal(0, sessionFactory.OpenCount);
    }

    [Fact]
    public async Task StartAsync_WhenAlreadyRunningDoesNotReopenSession()
    {
        var sessionFactory = new FakeAudioEndpointSessionFactory();
        await using var engine = new EndpointHoldKeepAliveEngine(sessionFactory);
        var selection = new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null);

        await engine.StartAsync(selection, KeepAliveMode.HoldOnly, CancellationToken.None);
        await engine.StartAsync(selection, KeepAliveMode.HoldOnly, CancellationToken.None);

        Assert.Equal(1, sessionFactory.OpenCount);
        Assert.Equal(KeepAliveEngineState.Running, engine.State);
    }

    [Fact]
    public async Task StartAsync_RejectsSilentPcmMode()
    {
        var sessionFactory = new FakeAudioEndpointSessionFactory();
        await using var engine = new EndpointHoldKeepAliveEngine(sessionFactory);

        await Assert.ThrowsAsync<NotSupportedException>(() => engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.SilentPcm,
            CancellationToken.None));

        Assert.Equal(KeepAliveEngineState.Stopped, engine.State);
        Assert.Equal(0, sessionFactory.OpenCount);
    }

    [Fact]
    public async Task StartAsync_WhenFactoryFailsTransitionsToFaulted()
    {
        var sessionFactory = new FakeAudioEndpointSessionFactory
        {
            Failure = new InvalidOperationException("endpoint missing")
        };
        await using var engine = new EndpointHoldKeepAliveEngine(sessionFactory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None));

        Assert.Equal(KeepAliveEngineState.Faulted, engine.State);
    }

    [Fact]
    public async Task GetDiagnosticsAsync_WhenRunningReportsHeldEndpoint()
    {
        var sessionFactory = new FakeAudioEndpointSessionFactory
        {
            EndpointId = "device-a",
            EndpointName = "Device A"
        };
        await using var engine = new EndpointHoldKeepAliveEngine(sessionFactory);

        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None);
        EngineDiagnostics diagnostics = await engine.GetDiagnosticsAsync(CancellationToken.None);

        Assert.Equal("device-a", diagnostics.EndpointId);
        Assert.Equal("Device A", diagnostics.EndpointName);
        Assert.Equal(KeepAliveMode.HoldOnly, diagnostics.Mode);
        Assert.Equal(KeepAliveEngineState.Running, diagnostics.State);
        Assert.NotNull(diagnostics.LastStateChange);
    }

    private sealed class FakeAudioEndpointSessionFactory : IAudioEndpointSessionFactory
    {
        public int OpenCount { get; private set; }

        public AudioDeviceSelection? LastSelection { get; private set; }

        public KeepAliveMode? LastMode { get; private set; }

        public FakeAudioEndpointSession? LastSession { get; private set; }

        public Exception? Failure { get; init; }

        public string? EndpointId { get; init; }

        public string? EndpointName { get; init; }

        public Task<IAudioEndpointSession> OpenAsync(
            AudioDeviceSelection selection,
            KeepAliveMode mode,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Failure is not null)
            {
                throw Failure;
            }

            OpenCount++;
            LastSelection = selection;
            LastMode = mode;
            LastSession = new FakeAudioEndpointSession(EndpointId, EndpointName);
            return Task.FromResult<IAudioEndpointSession>(LastSession);
        }
    }

    private sealed class FakeAudioEndpointSession : IAudioEndpointSession
    {
        public FakeAudioEndpointSession(string? endpointId = null, string? endpointName = null)
        {
            EndpointId = endpointId;
            EndpointName = endpointName;
        }

        public string? EndpointId { get; }

        public string? EndpointName { get; }

        public bool Disposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
