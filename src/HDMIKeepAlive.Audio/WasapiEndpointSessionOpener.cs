using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens a Windows audio client in WASAPI shared mode without rendering buffers.
/// </summary>
public sealed class WasapiEndpointSessionOpener : IAudioEndpointSessionOpener
{
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

    [SupportedOSPlatform("windows")]
    private static WasapiEndpointSession OpenHoldSession(string endpointId)
    {
        var enumerator = CoreAudioInterop.CreateDeviceEnumerator();
        enumerator.GetDevice(endpointId, out IMMDevice device);

        Guid audioClientId = typeof(IAudioClient).GUID;
        device.Activate(ref audioClientId, CoreAudioInterop.ClsctxAll, IntPtr.Zero, out object audioClientObject);
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

}
