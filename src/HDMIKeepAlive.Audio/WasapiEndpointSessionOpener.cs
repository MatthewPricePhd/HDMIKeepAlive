using System.Runtime.InteropServices;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens a Windows audio client in WASAPI shared mode without rendering buffers.
/// </summary>
public sealed class WasapiEndpointSessionOpener : IAudioEndpointSessionOpener
{
    private const int ClsctxAll = 23;

    /// <inheritdoc />
    public Task<IAudioEndpointSession> OpenHoldSessionAsync(string endpointId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpointId);
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("WASAPI endpoint hold sessions require Windows.");
        }

        return Task.FromResult<IAudioEndpointSession>(OpenHoldSession(endpointId));
    }

    private static WasapiEndpointSession OpenHoldSession(string endpointId)
    {
        var enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumerator();
        enumerator.GetDevice(endpointId, out IMMDevice device);

        Guid audioClientId = typeof(IAudioClient).GUID;
        device.Activate(ref audioClientId, ClsctxAll, IntPtr.Zero, out object audioClientObject);
        var audioClient = (IAudioClient)audioClientObject;

        IntPtr mixFormat = IntPtr.Zero;
        try
        {
            audioClient.GetMixFormat(out mixFormat);
            audioClient.Initialize(
                AudioClientShareMode.Shared,
                streamFlags: 0,
                hnsBufferDuration: 0,
                hnsPeriodicity: 0,
                mixFormat,
                audioSessionGuid: IntPtr.Zero);
        }
        finally
        {
            if (mixFormat != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(mixFormat);
            }
        }

        return new WasapiEndpointSession(endpointId, audioClientObject, device, enumerator);
    }

    private sealed class WasapiEndpointSession : IAudioEndpointSession
    {
        private object? audioClient;
        private object? device;
        private object? enumerator;

        public WasapiEndpointSession(string endpointId, object audioClient, object device, object enumerator)
        {
            EndpointId = endpointId;
            this.audioClient = audioClient;
            this.device = device;
            this.enumerator = enumerator;
        }

        public string? EndpointId { get; }

        public string? EndpointName => null;

        public ValueTask DisposeAsync()
        {
            ReleaseComObject(audioClient);
            ReleaseComObject(device);
            ReleaseComObject(enumerator);
            audioClient = null;
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

    private enum AudioClientShareMode
    {
        Shared = 0
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
}
