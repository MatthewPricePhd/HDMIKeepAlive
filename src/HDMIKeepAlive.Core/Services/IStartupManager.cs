namespace HDMIKeepAlive.Core.Services;

/// <summary>
/// Controls non-elevated startup registration for the current user.
/// </summary>
public interface IStartupManager
{
    /// <summary>Returns whether start-with-Windows is enabled for the current user.</summary>
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken);

    /// <summary>Enables or disables start-with-Windows for the current user.</summary>
    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken);
}
