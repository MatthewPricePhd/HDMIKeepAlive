namespace HDMIKeepAlive.Audio;

/// <summary>
/// Creates zero-valued PCM buffers.
/// </summary>
public static class PcmSilence
{
    /// <summary>
    /// Creates a PCM byte buffer containing digital silence.
    /// </summary>
    public static byte[] CreateBuffer(int byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        return new byte[byteCount];
    }
}
