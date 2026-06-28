using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.UI;

/// <summary>
/// Command-line options for early Windows hardware validation.
/// </summary>
public sealed record CommandLineOptions(
    bool ShowHelp,
    bool ListDevices,
    bool Run,
    KeepAliveMode Mode,
    AudioTargetMode TargetMode,
    string? DeviceId)
{
    /// <summary>
    /// Parses command-line arguments.
    /// </summary>
    public static CommandLineOptions Parse(string[] args)
    {
        if (args.Length == 0)
        {
            return Default with { ShowHelp = true };
        }

        CommandLineOptions options = Default;

        for (int index = 0; index < args.Length; index++)
        {
            string arg = args[index];
            switch (arg)
            {
                case "--help":
                case "-h":
                    options = options with { ShowHelp = true };
                    break;
                case "--list-devices":
                    options = options with { ListDevices = true };
                    break;
                case "--run":
                    options = options with { Run = true };
                    break;
                case "--mode":
                    options = options with { Mode = ParseEnum<KeepAliveMode>(ReadValue(args, ref index, arg)) };
                    break;
                case "--target":
                    options = options with { TargetMode = ParseTargetMode(ReadValue(args, ref index, arg)) };
                    break;
                case "--device-id":
                    options = options with
                    {
                        DeviceId = ReadValue(args, ref index, arg),
                        TargetMode = AudioTargetMode.SpecificDevice
                    };
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}", nameof(args));
            }
        }

        return options;
    }

    /// <summary>
    /// Gets usage text.
    /// </summary>
    public static string GetUsage()
    {
        return """
        HDMIKeepAlive hardware validation CLI

        Usage:
          HDMIKeepAlive.exe --list-devices
          HDMIKeepAlive.exe --run --mode HoldOnly
          HDMIKeepAlive.exe --run --mode SilentPcm
          HDMIKeepAlive.exe --run --mode SilentPcm --device-id "<endpoint-id>"
          HDMIKeepAlive.exe --run --mode SilentPcm --target hdmi

        Stop a running keep-alive session with Ctrl+C.
        """;
    }

    private static CommandLineOptions Default { get; } = new(
        ShowHelp: false,
        ListDevices: false,
        Run: false,
        Mode: KeepAliveMode.HoldOnly,
        TargetMode: AudioTargetMode.DefaultDevice,
        DeviceId: null);

    private static string ReadValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {optionName}.", nameof(args));
        }

        index++;
        return args[index];
    }

    private static TEnum ParseEnum<TEnum>(string value)
        where TEnum : struct
    {
        return Enum.TryParse(value, ignoreCase: true, out TEnum parsed)
            ? parsed
            : throw new ArgumentException($"Invalid {typeof(TEnum).Name}: {value}");
    }

    private static AudioTargetMode ParseTargetMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "default" => AudioTargetMode.DefaultDevice,
            "specific" => AudioTargetMode.SpecificDevice,
            "hdmi" => AudioTargetMode.HdmiDevicesOnly,
            _ => ParseEnum<AudioTargetMode>(value)
        };
    }
}
