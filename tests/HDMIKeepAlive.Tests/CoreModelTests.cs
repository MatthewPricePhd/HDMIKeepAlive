using HDMIKeepAlive.Core.Models;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class CoreModelTests
{
    [Fact]
    public void AudioDeviceInfo_PreservesEndpointMetadata()
    {
        var device = new AudioDeviceInfo(
            Id: "device-id",
            FriendlyName: "HDMI Output",
            IsDefault: true,
            State: AudioDeviceState.Active,
            InterfaceName: "Intel Display Audio",
            SampleRate: 48000,
            BitDepth: 16,
            Channels: 2);

        Assert.Equal("device-id", device.Id);
        Assert.True(device.IsDefault);
        Assert.Equal(AudioDeviceState.Active, device.State);
    }

    [Fact]
    public void AudioDeviceSelection_AllowsDefaultDeviceWithoutId()
    {
        var selection = new AudioDeviceSelection(
            AudioTargetMode.DefaultDevice,
            DeviceId: null,
            FriendlyNameFallback: null);

        Assert.Equal(AudioTargetMode.DefaultDevice, selection.TargetMode);
        Assert.Null(selection.DeviceId);
    }

    [Fact]
    public void AppSettings_DefaultsUseHoldOnlyForInitialResearch()
    {
        AppSettings settings = AppSettings.Default;

        Assert.Equal(1, settings.SchemaVersion);
        Assert.Equal(KeepAliveMode.HoldOnly, settings.KeepAliveMode);
        Assert.False(settings.StartWithWindows);
    }

    [Fact]
    public void EngineDiagnostics_CanRepresentStoppedEngine()
    {
        var diagnostics = new EngineDiagnostics(
            EndpointName: null,
            EndpointId: null,
            Mode: KeepAliveMode.HoldOnly,
            State: KeepAliveEngineState.Stopped,
            SampleRate: null,
            BitDepth: null,
            Channels: null,
            BufferDuration: null,
            Latency: null,
            FramesRendered: 0,
            ReconnectCount: 0,
            LastError: null,
            LastStateChange: null);

        Assert.Equal(KeepAliveEngineState.Stopped, diagnostics.State);
        Assert.Equal(0, diagnostics.FramesRendered);
    }

    [Fact]
    public void Enums_ExposeDocumentedValues()
    {
        Assert.Contains(KeepAliveMode.SilentPcm, Enum.GetValues<KeepAliveMode>());
        Assert.Contains(KeepAliveEngineState.Reconnecting, Enum.GetValues<KeepAliveEngineState>());
        Assert.Contains(AudioTargetMode.HdmiDevicesOnly, Enum.GetValues<AudioTargetMode>());
        Assert.Contains(AudioDeviceState.Unplugged, Enum.GetValues<AudioDeviceState>());
    }
}
