using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens Windows Silent PCM render sessions.
/// </summary>
public sealed class WasapiSilentPcmRenderSessionOpener : ISilentPcmRenderSessionOpener
{
    private readonly SilentPcmRenderOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasapiSilentPcmRenderSessionOpener"/> class.
    /// </summary>
    public WasapiSilentPcmRenderSessionOpener()
        : this(new SilentPcmRenderOptions())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WasapiSilentPcmRenderSessionOpener"/> class.
    /// </summary>
    public WasapiSilentPcmRenderSessionOpener(SilentPcmRenderOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task<ISilentPcmRenderSession> OpenAsync(string endpointId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("WASAPI Silent PCM rendering requires Windows.");
        }

        return Task.FromResult<ISilentPcmRenderSession>(OpenRenderSession(endpointId, options));
    }

    [SupportedOSPlatform("windows")]
    private static SilentPcmRenderSession OpenRenderSession(string endpointId, SilentPcmRenderOptions options)
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

            return new SilentPcmRenderSession(endpointId, endpointName: null, client, options);
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

        public void Render(uint frameCount, PcmRenderSignal signal)
        {
            if (frameCount == 0)
            {
                return;
            }

            byte[] bufferBytes = PcmKeepAliveBuffer.Create(Format, frameCount, signal);
            renderClient.GetBuffer(frameCount, out IntPtr buffer);
            Marshal.Copy(bufferBytes, 0, buffer, bufferBytes.Length);
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
        private const ushort WaveFormatPcm = 0x0001;
        private const ushort WaveFormatIeeeFloat = 0x0003;
        private const ushort WaveFormatExtensible = 0xfffe;
        private static readonly Guid PcmSubFormat = new("00000001-0000-0010-8000-00aa00389b71");
        private static readonly Guid IeeeFloatSubFormat = new("00000003-0000-0010-8000-00aa00389b71");

        public static AudioRenderFormat Parse(IntPtr waveFormatPointer)
        {
            WaveFormatEx format = Marshal.PtrToStructure<WaveFormatEx>(waveFormatPointer);
            AudioRenderSampleFormat sampleFormat = ParseSampleFormat(waveFormatPointer, format);
            return new AudioRenderFormat(
                SampleRate: checked((int)format.SamplesPerSecond),
                BitDepth: format.BitsPerSample,
                Channels: format.Channels,
                BlockAlign: format.BlockAlign,
                SampleFormat: sampleFormat);
        }

        private static AudioRenderSampleFormat ParseSampleFormat(IntPtr waveFormatPointer, WaveFormatEx format)
        {
            return format.FormatTag switch
            {
                WaveFormatPcm => AudioRenderSampleFormat.PcmInteger,
                WaveFormatIeeeFloat => AudioRenderSampleFormat.IeeeFloat,
                WaveFormatExtensible => ParseExtensibleSampleFormat(waveFormatPointer),
                _ => AudioRenderSampleFormat.Unknown
            };
        }

        private static AudioRenderSampleFormat ParseExtensibleSampleFormat(IntPtr waveFormatPointer)
        {
            WaveFormatExtensible format = Marshal.PtrToStructure<WaveFormatExtensible>(waveFormatPointer);
            if (format.SubFormat == PcmSubFormat)
            {
                return AudioRenderSampleFormat.PcmInteger;
            }

            return format.SubFormat == IeeeFloatSubFormat
                ? AudioRenderSampleFormat.IeeeFloat
                : AudioRenderSampleFormat.Unknown;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct WaveFormatEx
    {
        public readonly ushort FormatTag;

        public readonly ushort Channels;

        public readonly uint SamplesPerSecond;

        private readonly uint averageBytesPerSecond;

        public readonly ushort BlockAlign;

        public readonly ushort BitsPerSample;

        private readonly ushort extraSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct WaveFormatExtensible
    {
        private readonly WaveFormatEx format;
        private readonly ushort validBitsPerSample;
        private readonly uint channelMask;

        public readonly Guid SubFormat;
    }

}
