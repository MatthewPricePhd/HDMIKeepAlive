namespace HDMIKeepAlive.Core.Models;

/// <summary>
/// Audio engine lifecycle state.
/// </summary>
public enum KeepAliveEngineState
{
    /// <summary>The engine is stopped.</summary>
    Stopped,

    /// <summary>The engine is starting.</summary>
    Starting,

    /// <summary>The engine is running.</summary>
    Running,

    /// <summary>The engine is reconnecting after an endpoint change.</summary>
    Reconnecting,

    /// <summary>The engine encountered a fault.</summary>
    Faulted,

    /// <summary>The engine is stopping.</summary>
    Stopping
}
