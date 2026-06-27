using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;
using HDMIKeepAlive.UI.ViewModels;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public async Task InitializeAsync_LoadsSettingsAndPlaybackDevices()
    {
        var settingsStore = new FakeSettingsStore
        {
            Settings = AppSettings.Default with
            {
                TargetMode = AudioTargetMode.SpecificDevice,
                TargetDeviceId = "device-b",
                KeepAliveMode = KeepAliveMode.SilentPcm,
                LoggingEnabled = true
            }
        };
        var deviceEnumerator = new FakeAudioDeviceEnumerator(
            [
                CreateDevice("device-a", "Device A"),
                CreateDevice("device-b", "Device B")
            ]);
        var viewModel = CreateViewModel(settingsStore, deviceEnumerator);

        await viewModel.InitializeAsync(CancellationToken.None);

        Assert.Equal(2, viewModel.Devices.Count);
        Assert.Equal("device-b", viewModel.SelectedDevice?.Id);
        Assert.Equal(KeepAliveMode.SilentPcm, viewModel.SelectedMode);
        Assert.True(viewModel.LoggingEnabled);
    }

    [Fact]
    public async Task StartAsync_StartsEngineWithSelectedDeviceAndMode()
    {
        var engine = new FakeAudioKeepAliveEngine();
        var viewModel = CreateViewModel(engine: engine);
        await viewModel.InitializeAsync(CancellationToken.None);
        viewModel.SelectedDevice = CreateDevice("device-a", "Device A");
        viewModel.SelectedMode = KeepAliveMode.HoldOnly;

        await viewModel.StartAsync(CancellationToken.None);

        Assert.Equal(AudioTargetMode.SpecificDevice, engine.LastSelection?.TargetMode);
        Assert.Equal("device-a", engine.LastSelection?.DeviceId);
        Assert.Equal("Device A", engine.LastSelection?.FriendlyNameFallback);
        Assert.Equal(KeepAliveMode.HoldOnly, engine.LastMode);
        Assert.Equal(KeepAliveEngineState.Running, viewModel.EngineState);
    }

    [Fact]
    public async Task StopAsync_StopsEngineAndUpdatesState()
    {
        var engine = new FakeAudioKeepAliveEngine();
        var viewModel = CreateViewModel(engine: engine);
        await viewModel.InitializeAsync(CancellationToken.None);
        await viewModel.StartAsync(CancellationToken.None);

        await viewModel.StopAsync(CancellationToken.None);

        Assert.Equal(1, engine.StopCount);
        Assert.Equal(KeepAliveEngineState.Stopped, viewModel.EngineState);
    }

    [Fact]
    public async Task SaveSettingsAsync_PersistsUiSelections()
    {
        var settingsStore = new FakeSettingsStore();
        var viewModel = CreateViewModel(settingsStore: settingsStore);
        await viewModel.InitializeAsync(CancellationToken.None);
        viewModel.SelectedDevice = CreateDevice("device-a", "Device A");
        viewModel.SelectedMode = KeepAliveMode.SilentPcm;
        viewModel.StartWithWindows = true;
        viewModel.MinimizeToTray = false;
        viewModel.LoggingEnabled = true;

        await viewModel.SaveSettingsAsync(CancellationToken.None);

        Assert.True(settingsStore.SavedSettings?.StartWithWindows);
        Assert.False(settingsStore.SavedSettings?.MinimizeToTray);
        Assert.True(settingsStore.SavedSettings?.LoggingEnabled);
        Assert.Equal(AudioTargetMode.SpecificDevice, settingsStore.SavedSettings?.TargetMode);
        Assert.Equal("device-a", settingsStore.SavedSettings?.TargetDeviceId);
        Assert.Equal("Device A", settingsStore.SavedSettings?.TargetFriendlyNameFallback);
        Assert.Equal(KeepAliveMode.SilentPcm, settingsStore.SavedSettings?.KeepAliveMode);
    }

    [Fact]
    public async Task RefreshDiagnosticsAsync_FormatsStatusFields()
    {
        var diagnosticsProvider = new FakeDiagnosticsProvider
        {
            Diagnostics = new EngineDiagnostics(
                EndpointName: "Device A",
                EndpointId: "device-a",
                Mode: KeepAliveMode.SilentPcm,
                State: KeepAliveEngineState.Running,
                SampleRate: 48000,
                BitDepth: 16,
                Channels: 2,
                BufferDuration: TimeSpan.FromMilliseconds(20),
                Latency: TimeSpan.FromMilliseconds(10),
                FramesRendered: 1234,
                ReconnectCount: 2,
                LastError: null,
                LastStateChange: DateTimeOffset.UtcNow)
        };
        var viewModel = CreateViewModel(diagnosticsProvider: diagnosticsProvider);

        await viewModel.RefreshDiagnosticsAsync(CancellationToken.None);

        Assert.Equal("Running", viewModel.StateText);
        Assert.Equal("48 kHz / 16-bit / 2 ch", viewModel.FormatText);
        Assert.Equal("2", viewModel.ReconnectCountText);
        Assert.Equal("None", viewModel.LastErrorText);
    }

    [Fact]
    public async Task StartCommand_ExecutesStartAsync()
    {
        var engine = new FakeAudioKeepAliveEngine();
        var viewModel = CreateViewModel(engine: engine);
        await viewModel.InitializeAsync(CancellationToken.None);

        await viewModel.StartCommand.ExecuteAsync(null);

        Assert.Equal(1, engine.StartCount);
        Assert.Equal(KeepAliveEngineState.Running, viewModel.EngineState);
    }

    [Fact]
    public async Task StartCommand_DisablesWhileExecuting()
    {
        var engine = new BlockingAudioKeepAliveEngine();
        var viewModel = CreateViewModel(engine: engine);
        await viewModel.InitializeAsync(CancellationToken.None);

        Task commandTask = viewModel.StartCommand.ExecuteAsync(null);
        await engine.WaitForStartAttemptAsync();

        Assert.False(viewModel.StartCommand.CanExecute(null));

        engine.CompleteStart();
        await commandTask;
        Assert.True(viewModel.StartCommand.CanExecute(null));
    }

    private static MainViewModel CreateViewModel(
        FakeSettingsStore? settingsStore = null,
        FakeAudioDeviceEnumerator? deviceEnumerator = null,
        IAudioKeepAliveEngine? engine = null,
        FakeDiagnosticsProvider? diagnosticsProvider = null,
        FakeStartupManager? startupManager = null)
    {
        return new MainViewModel(
            settingsStore ?? new FakeSettingsStore(),
            deviceEnumerator ?? new FakeAudioDeviceEnumerator([CreateDevice("device-a", "Device A")]),
            engine ?? new FakeAudioKeepAliveEngine(),
            diagnosticsProvider ?? new FakeDiagnosticsProvider(),
            startupManager ?? new FakeStartupManager());
    }

    private static AudioDeviceInfo CreateDevice(string id, string friendlyName)
    {
        return new AudioDeviceInfo(
            id,
            friendlyName,
            IsDefault: false,
            AudioDeviceState.Active,
            InterfaceName: "Display Audio",
            SampleRate: 48000,
            BitDepth: 16,
            Channels: 2);
    }

    private sealed class FakeSettingsStore : ISettingsStore
    {
        public AppSettings Settings { get; init; } = AppSettings.Default;

        public AppSettings? SavedSettings { get; private set; }

        public Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Settings);
        }

        public Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SavedSettings = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAudioDeviceEnumerator : IAudioDeviceEnumerator
    {
        private readonly IReadOnlyList<AudioDeviceInfo> devices;

        public FakeAudioDeviceEnumerator(IReadOnlyList<AudioDeviceInfo> devices)
        {
            this.devices = devices;
        }

        public Task<IReadOnlyList<AudioDeviceInfo>> GetPlaybackDevicesAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(devices);
        }

        public Task<AudioDeviceInfo?> GetDefaultPlaybackDeviceAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(devices.FirstOrDefault());
        }
    }

    private sealed class FakeAudioKeepAliveEngine : IAudioKeepAliveEngine
    {
        public KeepAliveEngineState State { get; private set; } = KeepAliveEngineState.Stopped;

        public event EventHandler<KeepAliveStateChangedEventArgs>? StateChanged;

        public AudioDeviceSelection? LastSelection { get; private set; }

        public KeepAliveMode? LastMode { get; private set; }

        public int StopCount { get; private set; }

        public int StartCount { get; private set; }

        public Task StartAsync(
            AudioDeviceSelection selection,
            KeepAliveMode mode,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            StartCount++;
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
            return ValueTask.CompletedTask;
        }

        private void SetState(KeepAliveEngineState state)
        {
            KeepAliveEngineState previousState = State;
            State = state;
            StateChanged?.Invoke(this, new KeepAliveStateChangedEventArgs(previousState, state));
        }
    }

    private sealed class BlockingAudioKeepAliveEngine : IAudioKeepAliveEngine
    {
        private readonly TaskCompletionSource startAttempt = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource completeStart = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public KeepAliveEngineState State { get; private set; } = KeepAliveEngineState.Stopped;

        public event EventHandler<KeepAliveStateChangedEventArgs>? StateChanged;

        public async Task StartAsync(
            AudioDeviceSelection selection,
            KeepAliveMode mode,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            startAttempt.SetResult();
            await completeStart.Task.WaitAsync(cancellationToken);
            KeepAliveEngineState previous = State;
            State = KeepAliveEngineState.Running;
            StateChanged?.Invoke(this, new KeepAliveStateChangedEventArgs(previous, State));
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            KeepAliveEngineState previous = State;
            State = KeepAliveEngineState.Stopped;
            StateChanged?.Invoke(this, new KeepAliveStateChangedEventArgs(previous, State));
            return Task.CompletedTask;
        }

        public Task WaitForStartAttemptAsync()
        {
            return startAttempt.Task;
        }

        public void CompleteStart()
        {
            completeStart.SetResult();
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FakeDiagnosticsProvider : IDiagnosticsProvider
    {
        public EngineDiagnostics Diagnostics { get; init; } = new(
            EndpointName: null,
            EndpointId: null,
            Mode: KeepAliveMode.HoldOnly,
            State: KeepAliveEngineState.Stopped,
            SampleRate: null,
            BitDepth: null,
            Channels: null,
            BufferDuration: null,
            Latency: null,
            FramesRendered: 0,
            ReconnectCount: 0,
            LastError: null,
            LastStateChange: null);

        public Task<EngineDiagnostics> GetSnapshotAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Diagnostics);
        }
    }

    private sealed class FakeStartupManager : IStartupManager
    {
        public Task<bool> IsEnabledAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(false);
        }

        public Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
