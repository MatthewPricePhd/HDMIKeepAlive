namespace HDMIKeepAlive.Audio;

/// <summary>
/// Provides delay behavior for audio render loops.
/// </summary>
public interface IRenderLoopClock
{
    /// <summary>Delays the render loop without blocking a thread.</summary>
    Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken);
}
