using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Raw playback endpoint snapshot returned by an audio endpoint source.
/// </summary>
public sealed record AudioEndpointSnapshot(
    string Id,
    string FriendlyName,
    AudioDeviceState State,
    string? InterfaceName,
    int? SampleRate,
    int? BitDepth,
    int? Channels);
