using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;

namespace HDMIKeepAlive.UI.ViewModels;

/// <summary>
/// View model for the main HDMIKeepAlive configuration and status surface.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ISettingsStore settingsStore;
    private readonly IAudioDeviceEnumerator audioDeviceEnumerator;
    private readonly IAudioKeepAliveEngine audioKeepAliveEngine;
    private readonly IDiagnosticsProvider diagnosticsProvider;
    private readonly IStartupManager startupManager;
    private AudioDeviceInfo? selectedDevice;
    private KeepAliveMode selectedMode = KeepAliveMode.HoldOnly;
    private KeepAliveEngineState engineState = KeepAliveEngineState.Stopped;
    private bool startWithWindows;
    private bool minimizeToTray = true;
    private bool loggingEnabled;
    private string stateText = KeepAliveEngineState.Stopped.ToString();
    private string formatText = "Unavailable";
    private string reconnectCountText = "0";
    private string lastErrorText = "None";

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel(
        ISettingsStore settingsStore,
        IAudioDeviceEnumerator audioDeviceEnumerator,
        IAudioKeepAliveEngine audioKeepAliveEngine,
        IDiagnosticsProvider diagnosticsProvider,
        IStartupManager startupManager)
    {
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        this.audioDeviceEnumerator = audioDeviceEnumerator ?? throw new ArgumentNullException(nameof(audioDeviceEnumerator));
        this.audioKeepAliveEngine = audioKeepAliveEngine ?? throw new ArgumentNullException(nameof(audioKeepAliveEngine));
        this.diagnosticsProvider = diagnosticsProvider ?? throw new ArgumentNullException(nameof(diagnosticsProvider));
        this.startupManager = startupManager ?? throw new ArgumentNullException(nameof(startupManager));
        this.audioKeepAliveEngine.StateChanged += OnEngineStateChanged;

        StartCommand = new AsyncRelayCommand((_, token) => StartAsync(token));
        StopCommand = new AsyncRelayCommand((_, token) => StopAsync(token));
        RestartCommand = new AsyncRelayCommand((_, token) => RestartAsync(token));
        SaveSettingsCommand = new AsyncRelayCommand((_, token) => SaveSettingsAsync(token));
        RefreshDiagnosticsCommand = new AsyncRelayCommand((_, token) => RefreshDiagnosticsAsync(token));
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets playback devices available for selection.</summary>
    public ObservableCollection<AudioDeviceInfo> Devices { get; } = [];

    /// <summary>Gets the keep-alive modes available in the UI.</summary>
    public IReadOnlyList<KeepAliveMode> AvailableModes { get; } =
        [KeepAliveMode.HoldOnly, KeepAliveMode.SilentPcm, KeepAliveMode.Auto];

    /// <summary>Gets the command that starts the engine.</summary>
    public AsyncRelayCommand StartCommand { get; }

    /// <summary>Gets the command that stops the engine.</summary>
    public AsyncRelayCommand StopCommand { get; }

    /// <summary>Gets the command that restarts the engine.</summary>
    public AsyncRelayCommand RestartCommand { get; }

    /// <summary>Gets the command that saves settings.</summary>
    public AsyncRelayCommand SaveSettingsCommand { get; }

    /// <summary>Gets the command that refreshes diagnostics.</summary>
    public AsyncRelayCommand RefreshDiagnosticsCommand { get; }

    /// <summary>Gets or sets the selected playback device.</summary>
    public AudioDeviceInfo? SelectedDevice
    {
        get => selectedDevice;
        set => SetProperty(ref selectedDevice, value);
    }

    /// <summary>Gets or sets the selected keep-alive mode.</summary>
    public KeepAliveMode SelectedMode
    {
        get => selectedMode;
        set => SetProperty(ref selectedMode, value);
    }

    /// <summary>Gets the current engine state.</summary>
    public KeepAliveEngineState EngineState
    {
        get => engineState;
        private set
        {
            if (SetProperty(ref engineState, value))
            {
                StateText = value.ToString();
            }
        }
    }

    /// <summary>Gets or sets whether startup is enabled.</summary>
    public bool StartWithWindows
    {
        get => startWithWindows;
        set => SetProperty(ref startWithWindows, value);
    }

    /// <summary>Gets or sets whether the main window should minimize to tray.</summary>
    public bool MinimizeToTray
    {
        get => minimizeToTray;
        set => SetProperty(ref minimizeToTray, value);
    }

    /// <summary>Gets or sets whether local logging is enabled.</summary>
    public bool LoggingEnabled
    {
        get => loggingEnabled;
        set => SetProperty(ref loggingEnabled, value);
    }

    /// <summary>Gets a display string for the current state.</summary>
    public string StateText
    {
        get => stateText;
        private set => SetProperty(ref stateText, value);
    }

    /// <summary>Gets a display string for the current render format.</summary>
    public string FormatText
    {
        get => formatText;
        private set => SetProperty(ref formatText, value);
    }

    /// <summary>Gets a display string for reconnect count.</summary>
    public string ReconnectCountText
    {
        get => reconnectCountText;
        private set => SetProperty(ref reconnectCountText, value);
    }

    /// <summary>Gets a display string for the last error.</summary>
    public string LastErrorText
    {
        get => lastErrorText;
        private set => SetProperty(ref lastErrorText, value);
    }

    /// <summary>
    /// Loads settings and playback devices.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        AppSettings settings = await settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AudioDeviceInfo> devices = await audioDeviceEnumerator
            .GetPlaybackDevicesAsync(cancellationToken)
            .ConfigureAwait(false);

        Devices.Clear();
        foreach (AudioDeviceInfo device in devices)
        {
            Devices.Add(device);
        }

        SelectedMode = settings.KeepAliveMode;
        StartWithWindows = settings.StartWithWindows || await startupManager
            .IsEnabledAsync(cancellationToken)
            .ConfigureAwait(false);
        MinimizeToTray = settings.MinimizeToTray;
        LoggingEnabled = settings.LoggingEnabled;
        SelectedDevice = ResolveSelectedDevice(settings, devices);
        EngineState = audioKeepAliveEngine.State;
    }

    /// <summary>
    /// Starts the keep-alive engine with the current UI selection.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        AudioDeviceSelection selection = CreateDeviceSelection();
        await audioKeepAliveEngine.StartAsync(selection, SelectedMode, cancellationToken).ConfigureAwait(false);
        EngineState = audioKeepAliveEngine.State;
    }

    /// <summary>
    /// Stops the keep-alive engine.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await audioKeepAliveEngine.StopAsync(cancellationToken).ConfigureAwait(false);
        EngineState = audioKeepAliveEngine.State;
    }

    /// <summary>
    /// Restarts the keep-alive engine using the current UI selection.
    /// </summary>
    public async Task RestartAsync(CancellationToken cancellationToken)
    {
        await StopAsync(cancellationToken).ConfigureAwait(false);
        await StartAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves current UI settings.
    /// </summary>
    public async Task SaveSettingsAsync(CancellationToken cancellationToken)
    {
        AppSettings settings = AppSettings.Default with
        {
            StartWithWindows = StartWithWindows,
            MinimizeToTray = MinimizeToTray,
            TargetMode = SelectedDevice is null ? AudioTargetMode.DefaultDevice : AudioTargetMode.SpecificDevice,
            TargetDeviceId = SelectedDevice?.Id,
            TargetFriendlyNameFallback = SelectedDevice?.FriendlyName,
            KeepAliveMode = SelectedMode,
            LoggingEnabled = LoggingEnabled
        };

        await settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await startupManager.SetEnabledAsync(StartWithWindows, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Refreshes diagnostics display fields.
    /// </summary>
    public async Task RefreshDiagnosticsAsync(CancellationToken cancellationToken)
    {
        EngineDiagnostics diagnostics = await diagnosticsProvider
            .GetSnapshotAsync(cancellationToken)
            .ConfigureAwait(false);

        EngineState = diagnostics.State;
        StateText = diagnostics.State.ToString();
        FormatText = FormatDiagnostics(diagnostics);
        ReconnectCountText = diagnostics.ReconnectCount.ToString();
        LastErrorText = string.IsNullOrWhiteSpace(diagnostics.LastError) ? "None" : diagnostics.LastError;
    }

    private static AudioDeviceInfo? ResolveSelectedDevice(
        AppSettings settings,
        IReadOnlyList<AudioDeviceInfo> devices)
    {
        if (settings.TargetMode == AudioTargetMode.SpecificDevice && settings.TargetDeviceId is not null)
        {
            AudioDeviceInfo? exact = devices.FirstOrDefault(device =>
                string.Equals(device.Id, settings.TargetDeviceId, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact;
            }
        }

        return devices.FirstOrDefault(device => device.IsDefault) ?? devices.FirstOrDefault();
    }

    private AudioDeviceSelection CreateDeviceSelection()
    {
        if (SelectedDevice is null)
        {
            return new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null);
        }

        return new AudioDeviceSelection(
            AudioTargetMode.SpecificDevice,
            SelectedDevice.Id,
            SelectedDevice.FriendlyName);
    }

    private static string FormatDiagnostics(EngineDiagnostics diagnostics)
    {
        if (diagnostics.SampleRate is null || diagnostics.BitDepth is null || diagnostics.Channels is null)
        {
            return "Unavailable";
        }

        return $"{diagnostics.SampleRate / 1000} kHz / {diagnostics.BitDepth}-bit / {diagnostics.Channels} ch";
    }

    private void OnEngineStateChanged(object? sender, KeepAliveStateChangedEventArgs args)
    {
        EngineState = args.CurrentState;
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
