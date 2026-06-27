using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Core.Services;

/// <summary>
/// Reads and writes user-local application settings.
/// </summary>
public interface ISettingsStore
{
    /// <summary>Loads settings, returning defaults when no saved settings exist.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Saves settings for the current user.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
