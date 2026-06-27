using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;

namespace HDMIKeepAlive.Diagnostics;

/// <summary>
/// Provides a minimal diagnostics snapshot until the real audio engine exists.
/// </summary>
public sealed class DiagnosticsProvider : IDiagnosticsProvider
{
    private readonly IAudioKeepAliveEngine? audioKeepAliveEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagnosticsProvider"/> class.
    /// </summary>
    public DiagnosticsProvider(IAudioKeepAliveEngine? audioKeepAliveEngine = null)
    {
        this.audioKeepAliveEngine = audioKeepAliveEngine;
    }

    /// <inheritdoc />
    public Task<EngineDiagnostics> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var diagnostics = new EngineDiagnostics(
            EndpointName: null,
            EndpointId: null,
            Mode: KeepAliveMode.HoldOnly,
            State: audioKeepAliveEngine?.State ?? KeepAliveEngineState.Stopped,
            SampleRate: null,
            BitDepth: null,
            Channels: null,
            BufferDuration: null,
            Latency: null,
            FramesRendered: 0,
            ReconnectCount: 0,
            LastError: null,
            LastStateChange: null);

        return Task.FromResult(diagnostics);
    }
}
