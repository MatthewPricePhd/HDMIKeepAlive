using HDMIKeepAlive.Core.Models;
using HDMIKeepAlive.Core.Services;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Enumerates Windows playback endpoints through an injectable endpoint source.
/// </summary>
public sealed class WindowsAudioDeviceEnumerator : IAudioDeviceEnumerator
{
    private readonly IAudioEndpointSource endpointSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsAudioDeviceEnumerator"/> class.
    /// </summary>
    public WindowsAudioDeviceEnumerator()
        : this(new WasapiAudioEndpointSource())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsAudioDeviceEnumerator"/> class.
    /// </summary>
    public WindowsAudioDeviceEnumerator(IAudioEndpointSource endpointSource)
    {
        this.endpointSource = endpointSource ?? throw new ArgumentNullException(nameof(endpointSource));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AudioDeviceInfo>> GetPlaybackDevicesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<AudioEndpointSnapshot> endpoints = await endpointSource
            .GetPlaybackEndpointsAsync(cancellationToken)
            .ConfigureAwait(false);
        string? defaultEndpointId = await endpointSource
            .GetDefaultPlaybackEndpointIdAsync(cancellationToken)
            .ConfigureAwait(false);

        return endpoints
            .Where(endpoint => endpoint.State == AudioDeviceState.Active)
            .Select(endpoint => ToAudioDeviceInfo(endpoint, IsDefault(endpoint.Id, defaultEndpointId)))
            .ToArray();
    }

    /// <inheritdoc />
    public async Task<AudioDeviceInfo?> GetDefaultPlaybackDeviceAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<AudioDeviceInfo> devices = await GetPlaybackDevicesAsync(cancellationToken)
            .ConfigureAwait(false);

        return devices.SingleOrDefault(device => device.IsDefault);
    }

    private static AudioDeviceInfo ToAudioDeviceInfo(AudioEndpointSnapshot endpoint, bool isDefault)
    {
        return new AudioDeviceInfo(
            endpoint.Id,
            endpoint.FriendlyName,
            isDefault,
            endpoint.State,
            endpoint.InterfaceName,
            endpoint.SampleRate,
            endpoint.BitDepth,
            endpoint.Channels);
    }

    private static bool IsDefault(string endpointId, string? defaultEndpointId)
    {
        return string.Equals(endpointId, defaultEndpointId, StringComparison.OrdinalIgnoreCase);
    }
}
