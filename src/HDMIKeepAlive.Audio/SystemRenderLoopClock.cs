namespace HDMIKeepAlive.Audio;

/// <summary>
/// Uses asynchronous delays for production render loop pacing.
/// </summary>
public sealed class SystemRenderLoopClock : IRenderLoopClock
{
    /// <inheritdoc />
    public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        return Task.Delay(delay, cancellationToken);
    }
}
