using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens shared-mode render sessions for Silent PCM mode.
/// </summary>
public interface ISilentPcmRenderSessionFactory
{
    /// <summary>Opens a render session for the selected endpoint.</summary>
    Task<ISilentPcmRenderSession> OpenAsync(
        AudioDeviceSelection selection,
        CancellationToken cancellationToken);
}
