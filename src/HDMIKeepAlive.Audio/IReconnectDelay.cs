namespace HDMIKeepAlive.Audio;

/// <summary>
/// Provides retry delay behavior for reconnect loops.
/// </summary>
public interface IReconnectDelay
{
    /// <summary>Delays before a reconnect attempt.</summary>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
