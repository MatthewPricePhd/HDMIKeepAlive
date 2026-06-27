using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Models;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class WasapiSilentPcmRenderSessionFactoryTests
{
    [Fact]
    public async Task OpenAsync_WithDefaultDeviceSelectionOpensDefaultEndpoint()
    {
        var opener = new FakeSilentPcmRenderSessionOpener();
        var factory = new WasapiSilentPcmRenderSessionFactory(
            new FakeAudioEndpointSource(
                [new AudioEndpointSnapshot("device-a", "Device A", AudioDeviceState.Active, null, null, null, null)],
                defaultPlaybackEndpointId: "device-a"),
            opener);

        await using ISilentPcmRenderSession session = await factory.OpenAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
            CancellationToken.None);

        Assert.Equal("device-a", opener.OpenedEndpointId);
    }

    [Fact]
    public async Task OpenAsync_WithSpecificDeviceSelectionOpensRequestedEndpoint()
    {
        var opener = new FakeSilentPcmRenderSessionOpener();
        var factory = new WasapiSilentPcmRenderSessionFactory(
            new FakeAudioEndpointSource(
                [new AudioEndpointSnapshot("device-b", "Device B", AudioDeviceState.Active, null, null, null, null)],
                defaultPlaybackEndpointId: null),
            opener);

        await using ISilentPcmRenderSession session = await factory.OpenAsync(
            new AudioDeviceSelection(AudioTargetMode.SpecificDevice, "device-b", null),
            CancellationToken.None);

        Assert.Equal("device-b", opener.OpenedEndpointId);
    }

    [Fact]
    public async Task OpenAsync_WithMissingSpecificDeviceUsesFriendlyNameFallback()
    {
        var opener = new FakeSilentPcmRenderSessionOpener();
        var factory = new WasapiSilentPcmRenderSessionFactory(
            new FakeAudioEndpointSource(
                [new AudioEndpointSnapshot("new-device-id", "Sony Soundbar", AudioDeviceState.Active, null, null, null, null)],
                defaultPlaybackEndpointId: null),
            opener);

        await using ISilentPcmRenderSession session = await factory.OpenAsync(
            new AudioDeviceSelection(AudioTargetMode.SpecificDevice, "old-device-id", "sony soundbar"),
            CancellationToken.None);

        Assert.Equal("new-device-id", opener.OpenedEndpointId);
    }

    [Fact]
    public async Task OpenAsync_WithHdmiOnlySelectionOpensFirstHdmiLikeEndpoint()
    {
        var opener = new FakeSilentPcmRenderSessionOpener();
        var factory = new WasapiSilentPcmRenderSessionFactory(
            new FakeAudioEndpointSource(
                [
                    new AudioEndpointSnapshot("speakers", "Speakers", AudioDeviceState.Active, null, null, null, null),
                    new AudioEndpointSnapshot("hdmi", "Display Audio", AudioDeviceState.Active, "Intel HDMI Audio", null, null, null)
                ],
                defaultPlaybackEndpointId: null),
            opener);

        await using ISilentPcmRenderSession session = await factory.OpenAsync(
            new AudioDeviceSelection(AudioTargetMode.HdmiDevicesOnly, null, null),
            CancellationToken.None);

        Assert.Equal("hdmi", opener.OpenedEndpointId);
    }

    [Fact]
    public async Task OpenAsync_WhenSelectionCannotResolveThrowsInvalidOperationException()
    {
        var factory = new WasapiSilentPcmRenderSessionFactory(
            new FakeAudioEndpointSource([], defaultPlaybackEndpointId: null),
            new FakeSilentPcmRenderSessionOpener());

        await Assert.ThrowsAsync<InvalidOperationException>(() => factory.OpenAsync(
            new AudioDeviceSelection(AudioTargetMode.DefaultDevice, null, null),
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

    private sealed class FakeSilentPcmRenderSessionOpener : ISilentPcmRenderSessionOpener
    {
        public string? OpenedEndpointId { get; private set; }

        public Task<ISilentPcmRenderSession> OpenAsync(string endpointId, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OpenedEndpointId = endpointId;
            return Task.FromResult<ISilentPcmRenderSession>(new FakeSilentPcmRenderSession(endpointId));
        }
    }

    private sealed class FakeSilentPcmRenderSession : ISilentPcmRenderSession
    {
        public FakeSilentPcmRenderSession(string endpointId)
        {
            EndpointId = endpointId;
        }

        public string? EndpointId { get; }

        public string? EndpointName => null;

        public int? SampleRate => null;

        public int? BitDepth => null;

        public int? Channels => null;

        public TimeSpan? BufferDuration => null;

        public TimeSpan? Latency => null;

        public long FramesRendered => 0;

        public Task RenderSilenceAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }
}
