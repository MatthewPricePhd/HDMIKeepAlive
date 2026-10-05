using System.Buffers.Binary;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Creates PCM buffers for keep-alive rendering.
/// </summary>
public static class PcmKeepAliveBuffer
{
    private const float LowAmplitudeFloatSample = 0.000001f;

    /// <summary>
    /// Creates a render buffer for the selected signal.
    /// </summary>
    public static byte[] Create(AudioRenderFormat format, uint frameCount, PcmRenderSignal signal)
    {
        ArgumentNullException.ThrowIfNull(format);

        int byteCount = checked((int)(frameCount * (uint)format.BlockAlign));
        if (signal == PcmRenderSignal.DigitalSilence)
        {
            return PcmSilence.CreateBuffer(byteCount);
        }

        byte[] buffer = new byte[byteCount];
        WriteLowAmplitudeSignal(buffer, format, frameCount);
        return buffer;
    }

    private static void WriteLowAmplitudeSignal(byte[] buffer, AudioRenderFormat format, uint frameCount)
    {
        int bytesPerSample = format.Channels <= 0 ? 0 : format.BlockAlign / format.Channels;
        if (bytesPerSample <= 0)
        {
            return;
        }

        for (uint frame = 0; frame < frameCount; frame++)
        {
            bool positive = frame % 2 == 0;
            for (int channel = 0; channel < format.Channels; channel++)
            {
                int offset = checked(((int)frame * format.BlockAlign) + (channel * bytesPerSample));
                WriteSample(buffer.AsSpan(offset, bytesPerSample), format, positive);
            }
        }
    }

    private static void WriteSample(Span<byte> destination, AudioRenderFormat format, bool positive)
    {
        if (format.SampleFormat == AudioRenderSampleFormat.IeeeFloat && destination.Length == 4)
        {
            int sampleBits = BitConverter.SingleToInt32Bits(positive ? LowAmplitudeFloatSample : -LowAmplitudeFloatSample);
            BinaryPrimitives.WriteInt32LittleEndian(destination, sampleBits);
            return;
        }

        switch (destination.Length)
        {
            case 1:
                destination[0] = positive ? (byte)129 : (byte)127;
                break;
            case 2:
                BinaryPrimitives.WriteInt16LittleEndian(destination, positive ? (short)1 : (short)-1);
                break;
            case 3:
                WriteInt24LittleEndian(destination, positive ? 1 : -1);
                break;
            case 4:
                BinaryPrimitives.WriteInt32LittleEndian(destination, positive ? 256 : -256);
                break;
        }
    }

    private static void WriteInt24LittleEndian(Span<byte> destination, int sample)
    {
        destination[0] = (byte)(sample & 0xff);
        destination[1] = (byte)((sample >> 8) & 0xff);
        destination[2] = (byte)((sample >> 16) & 0xff);
    }
}
