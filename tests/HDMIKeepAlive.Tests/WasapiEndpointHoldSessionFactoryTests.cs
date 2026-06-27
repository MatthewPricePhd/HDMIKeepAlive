using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Models;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class WasapiEndpointHoldSessionFactoryTests
{
    [Fact]
    public async Task OpenAsync_WithDefaultDeviceSelectionOpensDefaultEndpoint()
    {
        var opener = new FakeAudioEndpointSessionOpener();
        var factory = new WasapiEndpointHoldSessionFactory(
            new FakeAudioEndpointSource(
                [new AudioEndpointSnapshot("device-a", "Device A", AudioDeviceState.Active, null, null, null, null)],
                defaultPlaybackEndpointId: "device-a"),
            opener);

        await using IAudioEndpointSession session = await factory.OpenAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None);

        Assert.Equal("device-a", opener.OpenedEndpointId);
    }

    [Fact]
    public async Task OpenAsync_WithSpecificDeviceSelectionOpensRequestedEndpoint()
    {
        var opener = new FakeAudioEndpointSessionOpener();
        var factory = new WasapiEndpointHoldSessionFactory(
            new FakeAudioEndpointSource(
                [new AudioEndpointSnapshot("device-b", "Device B", AudioDeviceState.Active, null, null, null, null)],
                defaultPlaybackEndpointId: null),
            opener);

        await using IAudioEndpointSession session = await factory.OpenAsync(
            new AudioDeviceSelection(AudioTargetMode.SpecificDevice, "device-b", null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None);

        Assert.Equal("device-b", opener.OpenedEndpointId);
    }

    [Fact]
    public async Task OpenAsync_WithMissingSpecificDeviceUsesFriendlyNameFallback()
    {
        var opener = new FakeAudioEndpointSessionOpener();
        var factory = new WasapiEndpointHoldSessionFactory(
            new FakeAudioEndpointSource(
                [new AudioEndpointSnapshot("new-device-id", "Sony Soundbar", AudioDeviceState.Active, null, null, null, null)],
                defaultPlaybackEndpointId: null),
            opener);

        await using IAudioEndpointSession session = await factory.OpenAsync(
            new AudioDeviceSelection(AudioTargetMode.SpecificDevice, "old-device-id", "sony soundbar"),
            KeepAliveMode.HoldOnly,
            CancellationToken.None);

        Assert.Equal("new-device-id", opener.OpenedEndpointId);
    }

    [Fact]
    public async Task OpenAsync_WhenSelectionCannotResolveThrowsInvalidOperationException()
    {
        var factory = new WasapiEndpointHoldSessionFactory(
            new FakeAudioEndpointSource([], defaultPlaybackEndpointId: null),
            new FakeAudioEndpointSessionOpener());

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.OpenAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.HoldOnly,
            CancellationToken.None));
    }

    [Fact]
    public async Task OpenAsync_RejectsSilentPcmMode()
    {
        var factory = new WasapiEndpointHoldSessionFactory(
            new FakeAudioEndpointSource([], defaultPlaybackEndpointId: null),
            new FakeAudioEndpointSessionOpener());

        await Assert.ThrowsAsync<NotSupportedException>(() => factory.OpenAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            KeepAliveMode.SilentPcm,
            CancellationToken.None));
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

    private sealed class FakeAudioEndpointSessionOpener : IAudioEndpointSessionOpener
    {
        public string? OpenedEndpointId { get; private set; }

        public Task<IAudioEndpointSession> OpenHoldSessionAsync(
            string endpointId,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenedEndpointId = endpointId;
            return Task.FromResult<IAudioEndpointSession>(new FakeAudioEndpointSession());
        }
    }

    private sealed class FakeAudioEndpointSession : IAudioEndpointSession
    {
        public string? EndpointId => null;

        public string? EndpointName => null;

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
