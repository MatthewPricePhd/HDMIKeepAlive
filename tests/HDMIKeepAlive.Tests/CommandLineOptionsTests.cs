using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.UI;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class CommandLineOptionsTests
{
    [Fact]
    public void Parse_WithNoArgumentsShowsHelp()
    {
        CommandLineOptions options = CommandLineOptions.Parse([]);

        Assert.True(options.ShowHelp);
        Assert.False(options.ListDevices);
        Assert.False(options.Run);
    }

    [Fact]
    public void Parse_ListDevicesSetsListMode()
    {
        CommandLineOptions options = CommandLineOptions.Parse(["--list-devices"]);

        Assert.True(options.ListDevices);
        Assert.False(options.ShowHelp);
    }

    [Fact]
    public void Parse_RunSilentPcmSpecificDevice()
    {
        CommandLineOptions options = CommandLineOptions.Parse(
            ["--run", "--mode", "SilentPcm", "--device-id", "device-a"]);

        Assert.True(options.Run);
        Assert.Equal(KeepAliveMode.SilentPcm, options.Mode);
        Assert.Equal("device-a", options.DeviceId);
        Assert.Equal(PcmRenderSignal.DigitalSilence, options.Signal);
    }

    [Fact]
    public void Parse_RunSilentPcmLowAmplitudeSignal()
    {
        CommandLineOptions options = CommandLineOptions.Parse(
            ["--run", "--mode", "SilentPcm", "--device-id", "device-a", "--signal", "low"]);

        Assert.Equal(PcmRenderSignal.LowAmplitude, options.Signal);
    }

    [Fact]
    public void Parse_RunHdmiOnlyTarget()
    {
        CommandLineOptions options = CommandLineOptions.Parse(["--run", "--target", "hdmi"]);

        Assert.Equal(AudioTargetMode.HdmiDevicesOnly, options.TargetMode);
    }

    [Fact]
    public void Parse_WithInvalidModeThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => CommandLineOptions.Parse(["--run", "--mode", "Noise"]));
    }
}
