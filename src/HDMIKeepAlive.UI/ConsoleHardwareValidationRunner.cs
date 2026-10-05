using HDMIKeepAlive.Audio;
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
        SilentPcmRenderOptions renderOptions = host.Services.GetRequiredService<SilentPcmRenderOptions>();
        renderOptions.Signal = options.Signal;

        IAudioKeepAliveEngine engine = host.Services.GetRequiredService<IAudioKeepAliveEngine>();
        AudioDeviceSelection selection = new(
            options.TargetMode,
            options.DeviceId,
            FriendlyNameFallback: null);

        await output.WriteLineAsync($"Starting {options.Mode} keep-alive. Press Ctrl+C to stop.").ConfigureAwait(false);
        if (options.Mode == KeepAliveMode.SilentPcm)
        {
            await output.WriteLineAsync($"PCM signal: {FormatSignal(options.Signal)}").ConfigureAwait(false);
        }

        await engine.StartAsync(selection, options.Mode, cancellationToken).ConfigureAwait(false);
        await output.WriteLineAsync("Keep-alive running.").ConfigureAwait(false);

        using var diagnosticsCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task? diagnosticsTask = options.Mode == KeepAliveMode.SilentPcm
            ? PrintSilentPcmDiagnosticsAsync(diagnosticsCancellation.Token)
            : null;

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
            await diagnosticsCancellation.CancelAsync().ConfigureAwait(false);
            if (diagnosticsTask is not null)
            {
                await diagnosticsTask.ConfigureAwait(false);
            }

            await engine.StopAsync(CancellationToken.None).ConfigureAwait(false);
            await output.WriteLineAsync("Keep-alive stopped.").ConfigureAwait(false);
        }

        return 0;
    }

    private async Task PrintSilentPcmDiagnosticsAsync(CancellationToken cancellationToken)
    {
        SilentPcmKeepAliveEngine engine = host.Services.GetRequiredService<SilentPcmKeepAliveEngine>();

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                EngineDiagnostics diagnostics = await engine.GetDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
                await output.WriteLineAsync(
                    $"Diagnostics: state={diagnostics.State}, frames={diagnostics.FramesRendered}, format={FormatDiagnostics(diagnostics)}, buffer={FormatTimeSpan(diagnostics.BufferDuration)}, latency={FormatTimeSpan(diagnostics.Latency)}")
                    .ConfigureAwait(false);

                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private static string FormatDevice(AudioDeviceInfo device)
    {
        if (device.SampleRate is null || device.BitDepth is null || device.Channels is null)
        {
            return "Unavailable";
        }

        return $"{device.SampleRate / 1000} kHz / {device.BitDepth}-bit / {device.Channels} ch";
    }

    private static string FormatSignal(PcmRenderSignal signal)
    {
        return signal switch
        {
            PcmRenderSignal.DigitalSilence => "digital silence",
            PcmRenderSignal.LowAmplitude => "low-amplitude alternating PCM",
            _ => signal.ToString()
        };
    }

    private static string FormatDiagnostics(EngineDiagnostics diagnostics)
    {
        if (diagnostics.SampleRate is null || diagnostics.BitDepth is null || diagnostics.Channels is null)
        {
            return "Unavailable";
        }

        return $"{diagnostics.SampleRate / 1000} kHz / {diagnostics.BitDepth}-bit / {diagnostics.Channels} ch";
    }

    private static string FormatTimeSpan(TimeSpan? value)
    {
        return value is null ? "Unavailable" : $"{value.Value.TotalMilliseconds:0.###} ms";
    }
}
