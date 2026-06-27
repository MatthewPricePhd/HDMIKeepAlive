namespace HDMIKeepAlive.Audio;

/// <summary>
/// Uses asynchronous delays for production reconnect attempts.
/// </summary>
public sealed class SystemReconnectDelay : IReconnectDelay
{
    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.Delay(delay, cancellationToken);
    }
}
