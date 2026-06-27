namespace HDMIKeepAlive.Audio;

/// <summary>
/// Describes the PCM format used by a shared-mode render stream.
/// </summary>
public sealed record AudioRenderFormat(
    int SampleRate,
    int BitDepth,
    int Channels,
    int BlockAlign);
