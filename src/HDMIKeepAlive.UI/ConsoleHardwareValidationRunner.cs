using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HDMIKeepAlive.UI;

/// <summary>
/// Console runner for early Windows hardware validation before the WinUI shell is available.
/// </summary>
public sealed class ConsoleHardwareValidationRunner
{
    private readonly IHost host;
    private readonly TextWriter output;
    private readonly ILogger<ConsoleHardwareValidationRunner> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConsoleHardwareValidationRunner"/> class.
    /// </summary>
    public ConsoleHardwareValidationRunner(IHost host, TextWriter output)
    {
        this.host = host ?? throw new ArgumentNullException(nameof(host));
        this.output = output ?? throw new ArgumentNullException(nameof(output));
        logger = host.Services.GetRequiredService<ILogger<ConsoleHardwareValidationRunner>>();
    }

    /// <summary>
    /// Executes the requested command-line action.
    /// </summary>
    public async Task<int> RunAsync(CommandLineOptions options, CancellationToken cancellationToken)
    {
        if (options.ShowHelp)
        {
            await output.WriteLineAsync(CommandLineOptions.GetUsage()).ConfigureAwait(false);
            return 0;
        }

        if (options.ListDevices)
        {
            return await ListDevicesAsync(cancellationToken).ConfigureAwait(false);
        }

        if (options.Run)
        {
            return await RunKeepAliveAsync(options, cancellationToken).ConfigureAwait(false);
        }

        await output.WriteLineAsync(CommandLineOptions.GetUsage()).ConfigureAwait(false);
        return 1;
    }

    private async Task<int> ListDevicesAsync(CancellationToken cancellationToken)
    {
        IAudioDeviceEnumerator enumerator = host.Services.GetRequiredService<IAudioDeviceEnumerator>();
        IReadOnlyList<AudioDeviceInfo> devices = await enumerator
            .GetPlaybackDevicesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (devices.Count == 0)
        {
            await output.WriteLineAsync("No active playback devices found.").ConfigureAwait(false);
            return 0;
        }

        foreach (AudioDeviceInfo device in devices)
        {
            string marker = device.IsDefault ? " [default]" : string.Empty;
            await output.WriteLineAsync($"{device.FriendlyName}{marker}").ConfigureAwait(false);
            await output.WriteLineAsync($"  ID: {device.Id}").ConfigureAwait(false);
            await output.WriteLineAsync($"  State: {device.State}").ConfigureAwait(false);
            await output.WriteLineAsync($"  Format: {FormatDevice(device)}").ConfigureAwait(false);
        }

        return 0;
    }

    private async Task<int> RunKeepAliveAsync(CommandLineOptions options, CancellationToken cancellationToken)
    {
        IAudioKeepAliveEngine engine = host.Services.GetRequiredService<IAudioKeepAliveEngine>();
        AudioDeviceSelection selection = new(
            options.TargetMode,
            options.DeviceId,
            FriendlyNameFallback: null);

        await output.WriteLineAsync($"Starting {options.Mode} keep-alive. Press Ctrl+C to stop.").ConfigureAwait(false);
        await engine.StartAsync(selection, options.Mode, cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync("Keep-alive running.").ConfigureAwait(false);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Stop requested.");
        }
        finally
        {
            await engine.StopAsync(CancellationToken.None).ConfigureAwait(false);
            await output.WriteLineAsync("Keep-alive stopped.").ConfigureAwait(false);
        }

        return 0;
    }

    private static string FormatDevice(AudioDeviceInfo device)
    {
        if (device.SampleRate is null || device.BitDepth is null || device.Channels is null)
        {
            return "Unavailable";
        }

        return $"{device.SampleRate / 1000} kHz / {device.BitDepth}-bit / {device.Channels} ch";
    }
}
