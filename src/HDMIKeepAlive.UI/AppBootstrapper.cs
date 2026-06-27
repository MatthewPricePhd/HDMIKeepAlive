using HDMIKeepAlive.Audio;
using HDMIKeepAlive.Core.Services;
using HDMIKeepAlive.Diagnostics;
using HDMIKeepAlive.Infrastructure;
using HDMIKeepAlive.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HDMIKeepAlive.UI;

/// <summary>
/// Configures the Milestone 0 application host.
/// </summary>
public static class AppBootstrapper
{
    /// <summary>
    /// Creates the application host builder.
    /// </summary>
    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
            })
            .ConfigureServices(ConfigureServices);
    }

    /// <summary>
    /// Registers application services owned by the UI composition root.
    /// </summary>
    public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<IStartupManager, NoOpStartupManager>();
        services.AddSingleton<IAudioDeviceEnumerator, WindowsAudioDeviceEnumerator>();
        services.AddSingleton<IDeviceChangeMonitor, WindowsDeviceChangeMonitor>();
        services.AddSingleton<IReconnectDelay, SystemReconnectDelay>();
        services.AddSingleton<EndpointHoldKeepAliveEngine>();
        services.AddSingleton<SilentPcmKeepAliveEngine>();
        services.AddSingleton<ModeRoutingAudioKeepAliveEngine>(serviceProvider => new ModeRoutingAudioKeepAliveEngine(
            serviceProvider.GetRequiredService<EndpointHoldKeepAliveEngine>(),
            serviceProvider.GetRequiredService<SilentPcmKeepAliveEngine>()));
        services.AddSingleton<IAudioKeepAliveEngine>(serviceProvider => new ReconnectingAudioKeepAliveEngine(
            serviceProvider.GetRequiredService<ModeRoutingAudioKeepAliveEngine>(),
            serviceProvider.GetRequiredService<IDeviceChangeMonitor>(),
            serviceProvider.GetRequiredService<IReconnectDelay>(),
            TimeSpan.FromSeconds(5)));
        services.AddSingleton<IDiagnosticsProvider, DiagnosticsProvider>();
        services.AddTransient<MainViewModel>();
    }
}
