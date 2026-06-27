using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Core.Services;

/// <summary>
/// Event data for audio engine state transitions.
/// </summary>
public sealed class KeepAliveStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KeepAliveStateChangedEventArgs"/> class.
    /// </summary>
    public KeepAliveStateChangedEventArgs(KeepAliveEngineState previousState, KeepAliveEngineState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
        ChangedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Gets the previous engine state.</summary>
    public KeepAliveEngineState PreviousState { get; }

    /// <summary>Gets the current engine state.</summary>
    public KeepAliveEngineState CurrentState { get; }

    /// <summary>Gets the UTC timestamp when the transition was observed.</summary>
    public DateTimeOffset ChangedAt { get; }
}
