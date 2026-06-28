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

        enumerator = CoreAudioInterop.CreateDeviceEnumerator();
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
}
