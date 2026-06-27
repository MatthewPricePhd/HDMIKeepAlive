namespace HDMIKeepAlive.Core.Models;

/// <summary>
/// Snapshot of audio engine state and runtime diagnostics.
/// </summary>
public sealed record EngineDiagnostics(
    string? EndpointName,
    string? EndpointId,
    KeepAliveMode Mode,
    KeepAliveEngineState State,
    int? SampleRate,
    int? BitDepth,
    int? Channels,
    TimeSpan? BufferDuration,
    TimeSpan? Latency,
    long FramesRendered,
    int ReconnectCount,
    string? LastError,
    DateTimeOffset? LastStateChange);
