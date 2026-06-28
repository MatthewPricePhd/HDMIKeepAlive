using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens Windows Silent PCM render sessions.
/// </summary>
public sealed class WasapiSilentPcmRenderSessionOpener : ISilentPcmRenderSessionOpener
{
    /// <inheritdoc />
    public Task<ISilentPcmRenderSession> OpenAsync(string endpointId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("WASAPI Silent PCM rendering requires Windows.");
        }

        return Task.FromResult<ISilentPcmRenderSession>(OpenRenderSession(endpointId));
    }

    [SupportedOSPlatform("windows")]
    private static SilentPcmRenderSession OpenRenderSession(string endpointId)
    {
        var enumerator = CoreAudioInterop.CreateDeviceEnumerator();
        enumerator.GetDevice(endpointId, out IMMDevice device);

        Guid audioClientId = typeof(IAudioClient).GUID;
        device.Activate(ref audioClientId, CoreAudioInterop.ClsctxAll, IntPtr.Zero, out object audioClientObject);
        var audioClient = (IAudioClient)audioClientObject;

        IntPtr mixFormatPointer = IntPtr.Zero;
        try
        {
            audioClient.GetMixFormat(out mixFormatPointer);
            AudioRenderFormat format = AudioRenderFormatParser.Parse(mixFormatPointer);
            audioClient.Initialize(
                AudioClientShareMode.Shared,
                streamFlags: 0,
                hnsBufferDuration: 0,
                hnsPeriodicity: 0,
                mixFormatPointer,
                audioSessionGuid: IntPtr.Zero);
            audioClient.GetBufferSize(out uint bufferFrameCount);
            audioClient.GetStreamLatency(out long latency);

            Guid renderClientId = typeof(IAudioRenderClient).GUID;
            audioClient.GetService(ref renderClientId, out object renderClientObject);
            var renderClient = (IAudioRenderClient)renderClientObject;

            audioClient.Start();

            var client = new WasapiRenderClient(
                format,
                bufferDuration: CalculateBufferDuration(bufferFrameCount, format.SampleRate),
                latency: TimeSpan.FromTicks(latency),
                bufferFrameCount,
                audioClient,
                renderClient,
                audioClientObject,
                renderClientObject,
                device,
                enumerator);

            return new SilentPcmRenderSession(endpointId, endpointName: null, client);
        }
        finally
        {
            if (mixFormatPointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(mixFormatPointer);
            }
        }
    }

    private static TimeSpan CalculateBufferDuration(uint bufferFrameCount, int sampleRate)
    {
        return sampleRate <= 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds(bufferFrameCount / (double)sampleRate);
    }

    private sealed class WasapiRenderClient : IWasapiRenderClient
    {
        private readonly uint bufferFrameCount;
        private readonly IAudioClient audioClient;
        private readonly IAudioRenderClient renderClient;
        private object? audioClientObject;
        private object? renderClientObject;
        private object? device;
        private object? enumerator;

        public WasapiRenderClient(
            AudioRenderFormat format,
            TimeSpan bufferDuration,
            TimeSpan latency,
            uint bufferFrameCount,
            IAudioClient audioClient,
            IAudioRenderClient renderClient,
            object audioClientObject,
            object renderClientObject,
            object device,
            object enumerator)
        {
            Format = format;
            BufferDuration = bufferDuration;
            Latency = latency;
            this.bufferFrameCount = bufferFrameCount;
            this.audioClient = audioClient;
            this.renderClient = renderClient;
            this.audioClientObject = audioClientObject;
            this.renderClientObject = renderClientObject;
            this.device = device;
            this.enumerator = enumerator;
        }

        public AudioRenderFormat Format { get; }

        public TimeSpan BufferDuration { get; }

        public TimeSpan Latency { get; }

        public uint GetBufferFrameCount()
        {
            return bufferFrameCount;
        }

        public uint GetCurrentPadding()
        {
            audioClient.GetCurrentPadding(out uint currentPadding);
            return currentPadding;
        }

        public void RenderSilence(uint frameCount)
        {
            if (frameCount == 0)
            {
                return;
            }

            int byteCount = checked((int)(frameCount * (uint)Format.BlockAlign));
            byte[] silence = PcmSilence.CreateBuffer(byteCount);
            renderClient.GetBuffer(frameCount, out IntPtr buffer);
            Marshal.Copy(silence, 0, buffer, silence.Length);
            renderClient.ReleaseBuffer(frameCount, flags: 0);
        }

        public void Stop()
        {
            audioClient.Stop();
        }

        public ValueTask DisposeAsync()
        {
            ReleaseComObject(renderClientObject);
            ReleaseComObject(audioClientObject);
            ReleaseComObject(device);
            ReleaseComObject(enumerator);
            renderClientObject = null;
            audioClientObject = null;
            device = null;
            enumerator = null;
            return ValueTask.CompletedTask;
        }

        private static void ReleaseComObject(object? value)
        {
            if (OperatingSystem.IsWindows() && value is not null && Marshal.IsComObject(value))
            {
                Marshal.ReleaseComObject(value);
            }
        }
    }

    private static class AudioRenderFormatParser
    {
        public static AudioRenderFormat Parse(IntPtr waveFormatPointer)
        {
            WaveFormatEx format = Marshal.PtrToStructure<WaveFormatEx>(waveFormatPointer);
            return new AudioRenderFormat(
                SampleRate: checked((int)format.SamplesPerSecond),
                BitDepth: format.BitsPerSample,
                Channels: format.Channels,
                BlockAlign: format.BlockAlign);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct WaveFormatEx
    {
        private readonly ushort formatTag;

        public readonly ushort Channels;

        public readonly uint SamplesPerSecond;

        private readonly uint averageBytesPerSecond;

        public readonly ushort BlockAlign;

        public readonly ushort BitsPerSample;

        private readonly ushort extraSize;
    }

}
