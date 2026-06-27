using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;
using HDMIKeepAlive.Diagnostics;
using HDMIKeepAlive.Infrastructure;
using HDMIKeepAlive.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class CoreServiceTests
{
    [Fact]
    public async Task AudioKeepAliveEngine_HoldEngineRaisesStateChange()
    {
        await using var engine = new EndpointHoldKeepAliveEngine(new TestAudioEndpointSessionFactory());
        var observedStates = new List<KeepAliveEngineState>();
        engine.StateChanged += (_, args) => observedStates.Add(args.CurrentState);

        await engine.StartAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None);
        await engine.StopAsync(CancellationToken.None);

        Assert.Equal(KeepAliveEngineState.Stopped, engine.State);
        Assert.Contains(KeepAliveEngineState.Running, observedStates);
        Assert.Contains(KeepAliveEngineState.Stopped, observedStates);
    }

    private sealed class TestAudioEndpointSessionFactory : IAudioEndpointSessionFactory
    {
        public Task<IAudioEndpointSession> OpenAsync(
            AudioDeviceSelection selection,
            KeepAliveMode mode,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<IAudioEndpointSession>(new TestAudioEndpointSession());
        }
    }

    private sealed class TestAudioEndpointSession : IAudioEndpointSession
    {
        public string? EndpointId => "test-endpoint";

        public string? EndpointName => "Test Endpoint";

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task SettingsStore_PersistsSettings()
    {
        ISettingsStore store = new JsonSettingsStore(Path.Combine(
            Path.GetTempPath(),
            "HDMIKeepAlive.Tests",
            Guid.NewGuid().ToString("N"),
            "settings.json"));
        AppSettings settings = AppSettings.Default with { LoggingEnabled = true };

        await store.SaveAsync(settings, CancellationToken.None);
        AppSettings loaded = await store.LoadAsync(CancellationToken.None);

        Assert.True(loaded.LoggingEnabled);
    }

    [Fact]
    public async Task StartupManager_PlaceholderNeverEnablesStartup()
    {
        IStartupManager startupManager = new NoOpStartupManager();

        await startupManager.SetEnabledAsync(true, CancellationToken.None);
        bool enabled = await startupManager.IsEnabledAsync(CancellationToken.None);

        Assert.False(enabled);
    }

    [Fact]
    public async Task DiagnosticsProvider_ReturnsStoppedSnapshotWithoutEngine()
    {
        IDiagnosticsProvider provider = new DiagnosticsProvider();

        EngineDiagnostics snapshot = await provider.GetSnapshotAsync(CancellationToken.None);

        Assert.Equal(KeepAliveEngineState.Stopped, snapshot.State);
        Assert.Equal(KeepAliveMode.HoldOnly, snapshot.Mode);
    }

    [Fact]
    public async Task DeviceChangeMonitor_InterfaceCanBeImplementedByTestDouble()
    {
        var monitor = new TestDeviceChangeMonitor();
        var observedReasons = new List<string>();
        monitor.DeviceChanged += (_, args) => observedReasons.Add(args.Reason);

        await monitor.StartAsync(CancellationToken.None);

        Assert.Contains("test", observedReasons);
    }

    [Fact]
    public void AppBootstrapper_RegistersApplicationServices()
    {
        using IHost host = AppBootstrapper.CreateHostBuilder(Array.Empty<string>()).Build();

        Assert.NotNull(host.Services.GetRequiredService<ISettingsStore>());
        Assert.NotNull(host.Services.GetRequiredService<IStartupManager>());
        Assert.NotNull(host.Services.GetRequiredService<IDiagnosticsProvider>());
        Assert.NotNull(host.Services.GetRequiredService<HDMIKeepAlive.UI.ViewModels.MainViewModel>());
    }

    private sealed class TestDeviceChangeMonitor : IDeviceChangeMonitor
    {
        public event EventHandler<DeviceChangedEventArgs>? DeviceChanged;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DeviceChanged?.Invoke(this, new DeviceChangedEventArgs(device: null, reason: "test"));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
