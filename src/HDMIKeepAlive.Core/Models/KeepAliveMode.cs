namespace HDMIKeepAlive.Core.Models;

/// <summary>
/// Keep-alive strategy selected by the user.
/// </summary>
public enum KeepAliveMode
{
    /// <summary>Open and hold the endpoint without rendering audio.</summary>
    HoldOnly,

    /// <summary>Render zero-valued PCM buffers. Not implemented before Milestone 3.</summary>
    SilentPcm,

    /// <summary>Automatically choose a keep-alive strategy. Not implemented in Milestone 0.</summary>
    Auto
}
