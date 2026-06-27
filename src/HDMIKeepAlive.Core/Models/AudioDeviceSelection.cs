namespace HDMIKeepAlive.Core.Models;

/// <summary>
/// Captures the user's target playback endpoint preference.
/// </summary>
public sealed record AudioDeviceSelection(
    AudioTargetMode TargetMode,
    string? DeviceId,
    string? FriendlyNameFallback);
