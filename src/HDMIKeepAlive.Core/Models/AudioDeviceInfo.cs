namespace HDMIKeepAlive.Core.Models;

/// <summary>
/// Describes a Windows playback endpoint without exposing implementation-specific audio APIs.
/// </summary>
public sealed record AudioDeviceInfo(
    string Id,
    string FriendlyName,
    bool IsDefault,
    AudioDeviceState State,
    string? InterfaceName,
    int? SampleRate,
    int? BitDepth,
    int? Channels);
