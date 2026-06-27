using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Core.Services;

/// <summary>
/// Aggregates audio engine and process diagnostics for display and logging.
/// </summary>
public interface IDiagnosticsProvider
{
    /// <summary>Gets the latest diagnostics snapshot.</summary>
    Task<EngineDiagnostics> GetSnapshotAsync(CancellationToken cancellationToken);
}
