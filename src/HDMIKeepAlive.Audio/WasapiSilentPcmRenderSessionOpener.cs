using System.Runtime.InteropServices;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens Windows Silent PCM render sessions.
/// </summary>
public sealed class WasapiSilentPcmRenderSessionOpener : ISilentPcmRenderSessionOpener
{
    private const int ClsctxAll = 23;

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

    private static SilentPcmRenderSession OpenRenderSession(string endpointId)
    {
        var enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumerator();
        enumerator.GetDevice(endpointId, out IMMDevice device);

        Guid audioClientId = typeof(IAudioClient).GUID;
        device.Activate(ref audioClientId, ClsctxAll, IntPtr.Zero, out object audioClientObject);
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

    private enum AudioClientShareMode
    {
        Shared = 0
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

    [ComImport]
    [Guid("bcde0395-e52f-467c-8e3d-c4579291692e")]
    private sealed class MMDeviceEnumerator
    {
    }

    [ComImport]
    [Guid("a95664d2-9614-4f35-a746-de8db63617e6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(int dataFlow, int stateMask, out object devices);

        void GetDefaultAudioEndpoint(int dataFlow, int role, out object endpoint);

        void GetDevice(
            [MarshalAs(UnmanagedType.LPWStr)] string id,
            out IMMDevice device);
    }

    [ComImport]
    [Guid("d666063f-1587-4e43-81f1-b948e807363f")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        void Activate(
            ref Guid interfaceId,
            int classContext,
            IntPtr activationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);

        void OpenPropertyStore(int accessMode, out object properties);

        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

        void GetState(out int state);
    }

    [ComImport]
    [Guid("1cb9ad4c-dbfa-4c32-b178-c2f568a703b2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioClient
    {
        void Initialize(
            AudioClientShareMode shareMode,
            int streamFlags,
            long hnsBufferDuration,
            long hnsPeriodicity,
            IntPtr mixFormat,
            IntPtr audioSessionGuid);

        void GetBufferSize(out uint bufferFrameCount);

        void GetStreamLatency(out long latency);

        void GetCurrentPadding(out uint currentPadding);

        void IsFormatSupported(
            AudioClientShareMode shareMode,
            IntPtr mixFormat,
            out IntPtr closestMatch);

        void GetMixFormat(out IntPtr deviceFormat);

        void GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);

        void Start();

        void Stop();

        void Reset();

        void SetEventHandle(IntPtr eventHandle);

        void GetService(ref Guid interfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object service);
    }

    [ComImport]
    [Guid("f294acfc-3146-4483-a7bf-addca7c260e2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioRenderClient
    {
        void GetBuffer(uint requestedFrames, out IntPtr data);

        void ReleaseBuffer(uint writtenFrames, int flags);
    }
}
