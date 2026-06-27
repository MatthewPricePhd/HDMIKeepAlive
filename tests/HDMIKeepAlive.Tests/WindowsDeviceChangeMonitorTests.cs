using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Services;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class WindowsDeviceChangeMonitorTests
{
    [Fact]
    public async Task StartAsync_RegistersForDeviceNotifications()
    {
        var registration = new FakeWindowsDeviceNotificationRegistration();
        var monitor = new WindowsDeviceChangeMonitor(registration);

        await monitor.StartAsync(CancellationToken.None);

        Assert.True(registration.Registered);
    }

    [Fact]
    public async Task StopAsync_UnregistersDeviceNotifications()
    {
        var registration = new FakeWindowsDeviceNotificationRegistration();
        var monitor = new WindowsDeviceChangeMonitor(registration);
        await monitor.StartAsync(CancellationToken.None);

        await monitor.StopAsync(CancellationToken.None);

        Assert.True(registration.Unregistered);
    }

    [Fact]
    public async Task NotificationCallback_RaisesDeviceChangedEvent()
    {
        var registration = new FakeWindowsDeviceNotificationRegistration();
        var monitor = new WindowsDeviceChangeMonitor(registration);
        var reasons = new List<string>();
        monitor.DeviceChanged += (_, args) => reasons.Add(args.Reason);

        await monitor.StartAsync(CancellationToken.None);
        registration.Callback!.NotifyDeviceChanged("default render endpoint changed");

        Assert.Contains("default render endpoint changed", reasons);
    }

    private sealed class FakeWindowsDeviceNotificationRegistration : IWindowsDeviceNotificationRegistration
    {
        public IWindowsDeviceNotificationCallback? Callback { get; private set; }

        public bool Registered { get; private set; }

        public bool Unregistered { get; private set; }

        public Task RegisterAsync(IWindowsDeviceNotificationCallback callback, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Registered = true;
            Callback = callback;
            return Task.CompletedTask;
        }

        public Task UnregisterAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Unregistered = true;
            Callback = null;
            return Task.CompletedTask;
        }
    }
}
