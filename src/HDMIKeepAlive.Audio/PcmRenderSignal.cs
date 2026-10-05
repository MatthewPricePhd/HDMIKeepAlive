namespace HDMIKeepAlive.Audio;

/// <summary>
/// Selects the PCM pattern written by the keep-alive render loop.
/// </summary>
public enum PcmRenderSignal
{
    DigitalSilence = 0,
    LowAmplitude = 1
}
