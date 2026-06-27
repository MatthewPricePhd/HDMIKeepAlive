using System.Runtime.InteropServices;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Registers an IMMNotificationClient callback with Windows Core Audio.
/// </summary>
public sealed class WindowsDeviceNotificationRegistration : IWindowsDeviceNotificationRegistration
{
    private IMMDeviceEnumerator? enumerator;
    private AudioNotificationClient? notificationClient;

    /// <inheritdoc />
    public Task RegisterAsync(IWindowsDeviceNotificationCallback callback, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(callback);
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.CompletedTask;
        }

        if (enumerator is not null)
        {
            return Task.CompletedTask;
        }

        enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumerator();
        notificationClient = new AudioNotificationClient(callback);
        enumerator.RegisterEndpointNotificationCallback(notificationClient);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnregisterAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.CompletedTask;
        }

        if (enumerator is not null && notificationClient is not null)
        {
            enumerator.UnregisterEndpointNotificationCallback(notificationClient);
        }

        ReleaseComObject(enumerator);
        enumerator = null;
        notificationClient = null;
        return Task.CompletedTask;
    }

    private static void ReleaseComObject(object? value)
    {
        if (OperatingSystem.IsWindows() && value is not null && Marshal.IsComObject(value))
        {
            Marshal.ReleaseComObject(value);
        }
    }

    private enum EDataFlow
    {
        ERender = 0,
        ECapture = 1,
        EAll = 2
    }

    private enum ERole
    {
        EConsole = 0,
        EMultimedia = 1,
        ECommunications = 2
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
        void EnumAudioEndpoints(EDataFlow dataFlow, int stateMask, out object devices);

        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out object endpoint);

        void GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out object device);

        void RegisterEndpointNotificationCallback(IMMNotificationClient client);

        void UnregisterEndpointNotificationCallback(IMMNotificationClient client);
    }

    [ComImport]
    [Guid("7991eec9-7e89-4d85-8390-6c703cec60c0")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMNotificationClient
    {
        void OnDeviceStateChanged(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            int newState);

        void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

        void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

        void OnDefaultDeviceChanged(
            EDataFlow flow,
            ERole role,
            [MarshalAs(UnmanagedType.LPWStr)] string? defaultDeviceId);

        void OnPropertyValueChanged(
            [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
            PropertyKey key);
    }

    private sealed class AudioNotificationClient : IMMNotificationClient
    {
        private readonly IWindowsDeviceNotificationCallback callback;

        public AudioNotificationClient(IWindowsDeviceNotificationCallback callback)
        {
            this.callback = callback;
        }

        public void OnDeviceStateChanged(string deviceId, int newState)
        {
            callback.NotifyDeviceChanged($"device state changed: {deviceId}");
        }

        public void OnDeviceAdded(string deviceId)
        {
            callback.NotifyDeviceChanged($"device added: {deviceId}");
        }

        public void OnDeviceRemoved(string deviceId)
        {
            callback.NotifyDeviceChanged($"device removed: {deviceId}");
        }

        public void OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? defaultDeviceId)
        {
            if (flow == EDataFlow.ERender)
            {
                callback.NotifyDeviceChanged($"default render endpoint changed: {defaultDeviceId}");
            }
        }

        public void OnPropertyValueChanged(string deviceId, PropertyKey key)
        {
            callback.NotifyDeviceChanged($"device property changed: {deviceId}");
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PropertyKey
    {
        private readonly Guid formatId;
        private readonly int propertyId;
    }
}
