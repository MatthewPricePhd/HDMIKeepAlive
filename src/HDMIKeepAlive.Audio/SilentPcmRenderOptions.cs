namespace HDMIKeepAlive.Audio;

/// <summary>
/// Runtime options for validation builds of the Silent PCM renderer.
/// </summary>
public sealed class SilentPcmRenderOptions
{
    /// <summary>
    /// Gets or sets the PCM signal written by the render loop.
    /// </summary>
    public PcmRenderSignal Signal { get; set; } = PcmRenderSignal.DigitalSilence;
}
