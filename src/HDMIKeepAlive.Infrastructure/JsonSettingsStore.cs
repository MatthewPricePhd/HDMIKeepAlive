using System.Text.Json;
using System.Text.Json.Serialization;
using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;

namespace HDMIKeepAlive.Infrastructure;

/// <summary>
/// Persists application settings to a user-local JSON file.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private readonly string settingsPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSettingsStore"/> class.
    /// </summary>
    public JsonSettingsStore()
        : this(GetDefaultSettingsPath())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonSettingsStore"/> class.
    /// </summary>
    public JsonSettingsStore(string settingsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsPath);
        this.settingsPath = settingsPath;
    }

    /// <summary>
    /// Gets the documented settings path under LocalAppData.
    /// </summary>
    public static string GetDefaultSettingsPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HDMIKeepAlive",
            "settings.json");
    }

    /// <inheritdoc />
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(settingsPath))
        {
            return AppSettings.Default;
        }

        try
        {
            await using FileStream stream = File.OpenRead(settingsPath);
            AppSettings? settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                stream,
                SerializerOptions,
                cancellationToken).ConfigureAwait(false);

            return Validate(settings);
        }
        catch (JsonException)
        {
            return AppSettings.Default;
        }
        catch (IOException)
        {
            return AppSettings.Default;
        }
        catch (UnauthorizedAccessException)
        {
            return AppSettings.Default;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();

        AppSettings validated = Validate(settings);
        string? directory = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using FileStream stream = File.Create(settingsPath);
        await JsonSerializer.SerializeAsync(
            stream,
            validated,
            SerializerOptions,
            cancellationToken).ConfigureAwait(false);
    }

    private static AppSettings Validate(AppSettings? settings)
    {
        if (settings is null || settings.SchemaVersion < 1)
        {
            return AppSettings.Default;
        }

        return settings with
        {
            SchemaVersion = 1,
            ReconnectIntervalSeconds = Math.Clamp(settings.ReconnectIntervalSeconds, 1, 300)
        };
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
