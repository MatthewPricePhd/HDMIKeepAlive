using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;
using HDMIKeepAlive.Infrastructure;
using Xunit;

namespace HDMIKeepAlive.Tests;

public sealed class JsonSettingsStoreTests
{
    [Fact]
    public async Task LoadAsync_WhenFileIsMissingReturnsDefaultSettings()
    {
        string settingsPath = CreateTempSettingsPath();
        ISettingsStore store = new JsonSettingsStore(settingsPath);

        AppSettings settings = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(AppSettings.Default, settings);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsSettingsAsJson()
    {
        string settingsPath = CreateTempSettingsPath();
        ISettingsStore store = new JsonSettingsStore(settingsPath);
        var expected = AppSettings.Default with
        {
            StartWithWindows = true,
            TargetMode = AudioTargetMode.SpecificDevice,
            TargetDeviceId = "SWD\\MMDEVAPI\\device",
            TargetFriendlyNameFallback = "Sony Soundbar",
            KeepAliveMode = KeepAliveMode.SilentPcm,
            LoggingEnabled = true,
            ReconnectIntervalSeconds = 12
        };

        await store.SaveAsync(expected, CancellationToken.None);
        AppSettings actual = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(expected, actual);
        string json = await File.ReadAllTextAsync(settingsPath, CancellationToken.None);
        Assert.Contains("\"schemaVersion\"", json, StringComparison.Ordinal);
        Assert.Contains("\"targetMode\": \"SpecificDevice\"", json, StringComparison.Ordinal);
        Assert.Contains("\"keepAliveMode\": \"SilentPcm\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadAsync_WhenJsonIsMalformedReturnsDefaultSettings()
    {
        string settingsPath = CreateTempSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        await File.WriteAllTextAsync(settingsPath, "{not valid json", CancellationToken.None);
        ISettingsStore store = new JsonSettingsStore(settingsPath);

        AppSettings settings = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(AppSettings.Default, settings);
    }

    [Fact]
    public async Task LoadAsync_ClampsReconnectIntervalToDocumentedBounds()
    {
        string settingsPath = CreateTempSettingsPath();
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        await File.WriteAllTextAsync(
            settingsPath,
            """
            {
              "schemaVersion": 1,
              "startWithWindows": false,
              "startMinimized": true,
              "minimizeToTray": true,
              "targetMode": "DefaultDevice",
              "targetDeviceId": null,
              "targetFriendlyNameFallback": null,
              "keepAliveMode": "HoldOnly",
              "loggingEnabled": false,
              "reconnectIntervalSeconds": 999,
              "showNotifications": true
            }
            """,
            CancellationToken.None);
        ISettingsStore store = new JsonSettingsStore(settingsPath);

        AppSettings settings = await store.LoadAsync(CancellationToken.None);

        Assert.Equal(300, settings.ReconnectIntervalSeconds);
    }

    [Fact]
    public void DefaultSettingsPath_UsesLocalAppDataHdmiKeepAliveSettingsJson()
    {
        string path = JsonSettingsStore.GetDefaultSettingsPath();

        Assert.EndsWith(Path.Combine("HDMIKeepAlive", "settings.json"), path, StringComparison.Ordinal);
        Assert.Contains(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            path,
            StringComparison.Ordinal);
    }

    private static string CreateTempSettingsPath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "HDMIKeepAlive.Tests",
            Guid.NewGuid().ToString("N"),
            "settings.json");
    }
}
