using HDMIKeepAlive.Audio;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class SilentPcmRenderSessionTests
{
    [Fact]
    public async Task RenderSilenceAsync_WritesOnlyAvailableFramesAndTracksDiagnostics()
    {
        var client = new FakeWasapiRenderClient(bufferFrameCount: 100, currentPadding: 40);
        await using var session = new SilentPcmRenderSession(
            endpointId: "device-a",
            endpointName: "Device A",
            client);

        await session.RenderSilenceAsync(CancellationToken.None);

        Assert.Equal(60u, client.LastRenderedFrameCount);
        Assert.Equal(PcmRenderSignal.DigitalSilence, client.LastSignal);
        Assert.Equal(60, session.FramesRendered);
        Assert.Equal(48000, session.SampleRate);
        Assert.Equal(16, session.BitDepth);
        Assert.Equal(2, session.Channels);
        Assert.Equal(TimeSpan.FromMilliseconds(20), session.BufferDuration);
        Assert.Equal(TimeSpan.FromMilliseconds(10), session.Latency);
    }

    [Fact]
    public async Task RenderSilenceAsync_WhenNoFramesAvailableDoesNotRender()
    {
        var client = new FakeWasapiRenderClient(bufferFrameCount: 100, currentPadding: 100);
        await using var session = new SilentPcmRenderSession("device-a", "Device A", client);

        await session.RenderSilenceAsync(CancellationToken.None);

        Assert.Null(client.LastRenderedFrameCount);
        Assert.Equal(0, session.FramesRendered);
    }

    [Fact]
    public async Task DisposeAsync_StopsAndDisposesClient()
    {
        var client = new FakeWasapiRenderClient(bufferFrameCount: 100, currentPadding: 0);
        var session = new SilentPcmRenderSession("device-a", "Device A", client);

        await session.DisposeAsync();

        Assert.True(client.Stopped);
        Assert.True(client.Disposed);
    }

    [Fact]
    public void PcmSilence_CreateBufferReturnsZeroValuedBytes()
    {
        byte[] buffer = PcmSilence.CreateBuffer(byteCount: 32);

        Assert.Equal(32, buffer.Length);
        Assert.All(buffer, value => Assert.Equal(0, value));
    }

    [Fact]
    public void PcmKeepAliveBuffer_CreateLowAmplitudeIntegerSignalReturnsNonZeroAlternatingSamples()
    {
        var format = new AudioRenderFormat(
            SampleRate: 48000,
            BitDepth: 16,
            Channels: 2,
            BlockAlign: 4,
            SampleFormat: AudioRenderSampleFormat.PcmInteger);

        byte[] buffer = PcmKeepAliveBuffer.Create(format, frameCount: 2, PcmRenderSignal.LowAmplitude);

        Assert.Equal([1, 0, 1, 0, 255, 255, 255, 255], buffer);
    }

    [Fact]
    public void PcmKeepAliveBuffer_CreateDigitalSilenceReturnsZeroValuedBytes()
    {
        var format = new AudioRenderFormat(
            SampleRate: 48000,
            BitDepth: 32,
            Channels: 2,
            BlockAlign: 8,
            SampleFormat: AudioRenderSampleFormat.IeeeFloat);

        byte[] buffer = PcmKeepAliveBuffer.Create(format, frameCount: 2, PcmRenderSignal.DigitalSilence);

        Assert.Equal(16, buffer.Length);
        Assert.All(buffer, value => Assert.Equal(0, value));
    }

    private sealed class FakeWasapiRenderClient : IWasapiRenderClient
    {
        private readonly uint bufferFrameCount;
        private readonly uint currentPadding;

        public FakeWasapiRenderClient(uint bufferFrameCount, uint currentPadding)
        {
            this.bufferFrameCount = bufferFrameCount;
            this.currentPadding = currentPadding;
        }

        public AudioRenderFormat Format { get; } = new(
            SampleRate: 48000,
            BitDepth: 16,
            Channels: 2,
            BlockAlign: 4,
            SampleFormat: AudioRenderSampleFormat.PcmInteger);

        public TimeSpan BufferDuration { get; } = TimeSpan.FromMilliseconds(20);

        public TimeSpan Latency { get; } = TimeSpan.FromMilliseconds(10);

        public uint? LastRenderedFrameCount { get; private set; }

        public PcmRenderSignal? LastSignal { get; private set; }

        public bool Stopped { get; private set; }

        public bool Disposed { get; private set; }

        public uint GetBufferFrameCount()
        {
            return bufferFrameCount;
        }

        public uint GetCurrentPadding()
        {
            return currentPadding;
        }

        public void Render(uint frameCount, PcmRenderSignal signal)
        {
            LastRenderedFrameCount = frameCount;
            LastSignal = signal;
        }

        public void Stop()
        {
            Stopped = true;
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
