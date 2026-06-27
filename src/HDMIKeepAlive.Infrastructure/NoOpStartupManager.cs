using HDMIKeepAlive.Core.Services;

namespace HDMIKeepAlive.Infrastructure;

/// <summary>
/// Milestone 0 startup manager that does not modify the system.
/// </summary>
public sealed class NoOpStartupManager : IStartupManager
{
    /// <inheritdoc />
    public Task<bool> IsEnabledAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }

    /// <inheritdoc />
    public Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
