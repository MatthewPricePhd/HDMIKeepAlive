using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class AudioDeviceEnumerationTests
{
    [Fact]
    public async Task GetPlaybackDevicesAsync_ReturnsOnlyActivePlaybackEndpoints()
    {
        IAudioDeviceEnumerator enumerator = new WindowsAudioDeviceEnumerator(
            new FakeAudioEndpointSource(
                [
                    new AudioEndpointSnapshot(
                        Id: "active-device",
                        FriendlyName: "Display Audio",
                        State: AudioDeviceState.Active,
                        InterfaceName: "HDMI",
                        SampleRate: 48000,
                        BitDepth: 16,
                        Channels: 2),
                    new AudioEndpointSnapshot(
                        Id: "disabled-device",
                        FriendlyName: "Disabled Audio",
                        State: AudioDeviceState.Disabled,
                        InterfaceName: null,
                        SampleRate: null,
                        BitDepth: null,
                        Channels: null)
                ],
                defaultPlaybackEndpointId: null));

        IReadOnlyList<AudioDeviceInfo> devices = await enumerator.GetPlaybackDevicesAsync(CancellationToken.None);

        AudioDeviceInfo device = Assert.Single(devices);
        Assert.Equal("active-device", device.Id);
        Assert.Equal("Display Audio", device.FriendlyName);
        Assert.Equal(AudioDeviceState.Active, device.State);
        Assert.Equal(48000, device.SampleRate);
        Assert.Equal(16, device.BitDepth);
        Assert.Equal(2, device.Channels);
    }

    [Fact]
    public async Task GetPlaybackDevicesAsync_MarksDefaultPlaybackEndpoint()
    {
        IAudioDeviceEnumerator enumerator = new WindowsAudioDeviceEnumerator(
            new FakeAudioEndpointSource(
                [
                    new AudioEndpointSnapshot("device-a", "Device A", AudioDeviceState.Active, null, null, null, null),
                    new AudioEndpointSnapshot("device-b", "Device B", AudioDeviceState.Active, null, null, null, null)
                ],
                defaultPlaybackEndpointId: "DEVICE-B"));

        IReadOnlyList<AudioDeviceInfo> devices = await enumerator.GetPlaybackDevicesAsync(CancellationToken.None);

        Assert.False(devices.Single(device => device.Id == "device-a").IsDefault);
        Assert.True(devices.Single(device => device.Id == "device-b").IsDefault);
    }

    [Fact]
    public async Task GetDefaultPlaybackDeviceAsync_ReturnsActiveDefaultEndpoint()
    {
        IAudioDeviceEnumerator enumerator = new WindowsAudioDeviceEnumerator(
            new FakeAudioEndpointSource(
                [
                    new AudioEndpointSnapshot("device-a", "Device A", AudioDeviceState.Active, null, null, null, null),
                    new AudioEndpointSnapshot("device-b", "Device B", AudioDeviceState.Active, null, null, null, null)
                ],
                defaultPlaybackEndpointId: "device-b"));

        AudioDeviceInfo? defaultDevice = await enumerator.GetDefaultPlaybackDeviceAsync(CancellationToken.None);

        Assert.NotNull(defaultDevice);
        Assert.Equal("device-b", defaultDevice.Id);
        Assert.True(defaultDevice.IsDefault);
    }

    [Fact]
    public async Task GetDefaultPlaybackDeviceAsync_ReturnsNullWhenDefaultIsUnavailable()
    {
        IAudioDeviceEnumerator enumerator = new WindowsAudioDeviceEnumerator(
            new FakeAudioEndpointSource(
                [
                    new AudioEndpointSnapshot("device-a", "Device A", AudioDeviceState.Active, null, null, null, null)
                ],
                defaultPlaybackEndpointId: "missing-device"));

        AudioDeviceInfo? defaultDevice = await enumerator.GetDefaultPlaybackDeviceAsync(CancellationToken.None);

        Assert.Null(defaultDevice);
    }

    private sealed class FakeAudioEndpointSource : IAudioEndpointSource
    {
        private readonly IReadOnlyList<AudioEndpointSnapshot> endpoints;
        private readonly string? defaultPlaybackEndpointId;

        public FakeAudioEndpointSource(
            IReadOnlyList<AudioEndpointSnapshot> endpoints,
            string? defaultPlaybackEndpointId)
        {
            this.endpoints = endpoints;
            this.defaultPlaybackEndpointId = defaultPlaybackEndpointId;
        }

        public Task<IReadOnlyList<AudioEndpointSnapshot>> GetPlaybackEndpointsAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(endpoints);
        }

        public Task<string?> GetDefaultPlaybackEndpointIdAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(defaultPlaybackEndpointId);
        }
    }
}
