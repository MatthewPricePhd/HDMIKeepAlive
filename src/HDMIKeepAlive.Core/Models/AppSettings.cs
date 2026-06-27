namespace HDMIKeepAlive.Core.Models;

/// <summary>
/// User-local application settings persisted by an <see cref="HDMIKeepAlive.Core.Services.ISettingsStore"/>.
/// </summary>
public sealed record AppSettings(
    int SchemaVersion,
    bool StartWithWindows,
    bool StartMinimized,
    bool MinimizeToTray,
    AudioTargetMode TargetMode,
    string? TargetDeviceId,
    string? TargetFriendlyNameFallback,
    KeepAliveMode KeepAliveMode,
    bool LoggingEnabled,
    int ReconnectIntervalSeconds,
    bool ShowNotifications)
{
    /// <summary>
    /// Gets the default settings used when no persisted settings exist.
    /// </summary>
    public static AppSettings Default { get; } = new(
        SchemaVersion: 1,
        StartWithWindows: false,
        StartMinimized: true,
        MinimizeToTray: true,
        TargetMode: AudioTargetMode.DefaultDevice,
        TargetDeviceId: null,
        TargetFriendlyNameFallback: null,
        KeepAliveMode: KeepAliveMode.HoldOnly,
        LoggingEnabled: false,
        ReconnectIntervalSeconds: 5,
        ShowNotifications: true);
}
