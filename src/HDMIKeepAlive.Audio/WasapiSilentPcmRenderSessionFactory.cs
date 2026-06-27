using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens Windows playback endpoints for Silent PCM rendering.
/// </summary>
public sealed class WasapiSilentPcmRenderSessionFactory : ISilentPcmRenderSessionFactory
{
    private readonly IAudioEndpointSource endpointSource;
    private readonly ISilentPcmRenderSessionOpener sessionOpener;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasapiSilentPcmRenderSessionFactory"/> class.
    /// </summary>
    public WasapiSilentPcmRenderSessionFactory()
        : this(new WasapiAudioEndpointSource(), new WasapiSilentPcmRenderSessionOpener())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WasapiSilentPcmRenderSessionFactory"/> class.
    /// </summary>
    public WasapiSilentPcmRenderSessionFactory(
        IAudioEndpointSource endpointSource,
        ISilentPcmRenderSessionOpener sessionOpener)
    {
        this.endpointSource = endpointSource ?? throw new ArgumentNullException(nameof(endpointSource));
        this.sessionOpener = sessionOpener ?? throw new ArgumentNullException(nameof(sessionOpener));
    }

    /// <inheritdoc />
    public async Task<ISilentPcmRenderSession> OpenAsync(
        AudioDeviceSelection selection,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selection);
        cancellationToken.ThrowIfCancellationRequested();

        string endpointId = await ResolveEndpointIdAsync(selection, cancellationToken).ConfigureAwait(false);
        return await sessionOpener.OpenAsync(endpointId, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> ResolveEndpointIdAsync(
        AudioDeviceSelection selection,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AudioEndpointSnapshot> endpoints = await endpointSource
            .GetPlaybackEndpointsAsync(cancellationToken)
            .ConfigureAwait(false);

        string? endpointId = selection.TargetMode switch
        {
            AudioTargetMode.DefaultDevice => await endpointSource
                .GetDefaultPlaybackEndpointIdAsync(cancellationToken)
                .ConfigureAwait(false),
            AudioTargetMode.SpecificDevice => ResolveSpecificEndpointId(selection, endpoints),
            AudioTargetMode.HdmiDevicesOnly => endpoints.FirstOrDefault(IsHdmiLikeEndpoint)?.Id,
            _ => null
        };

        return endpointId ?? throw new InvalidOperationException("No matching playback endpoint is available.");
    }

    private static string? ResolveSpecificEndpointId(
        AudioDeviceSelection selection,
        IReadOnlyList<AudioEndpointSnapshot> endpoints)
    {
        AudioEndpointSnapshot? byId = endpoints.FirstOrDefault(endpoint =>
            string.Equals(endpoint.Id, selection.DeviceId, StringComparison.OrdinalIgnoreCase));
        if (byId is not null)
        {
            return byId.Id;
        }

        if (string.IsNullOrWhiteSpace(selection.FriendlyNameFallback))
        {
            return null;
        }

        return endpoints.FirstOrDefault(endpoint =>
            string.Equals(
                endpoint.FriendlyName,
                selection.FriendlyNameFallback,
                StringComparison.OrdinalIgnoreCase))?.Id;
    }

    private static bool IsHdmiLikeEndpoint(AudioEndpointSnapshot endpoint)
    {
        return ContainsHdmiToken(endpoint.FriendlyName) || ContainsHdmiToken(endpoint.InterfaceName);
    }

    private static bool ContainsHdmiToken(string? value)
    {
        return value?.Contains("HDMI", StringComparison.OrdinalIgnoreCase) == true ||
            value?.Contains("Display Audio", StringComparison.OrdinalIgnoreCase) == true;
    }
}
