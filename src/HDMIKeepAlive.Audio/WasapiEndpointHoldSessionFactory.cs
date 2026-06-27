using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Opens Windows playback endpoints in WASAPI shared mode for hold-only research mode.
/// </summary>
public sealed class WasapiEndpointHoldSessionFactory : IAudioEndpointSessionFactory
{
    private readonly IAudioEndpointSource endpointSource;
    private readonly IAudioEndpointSessionOpener sessionOpener;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasapiEndpointHoldSessionFactory"/> class.
    /// </summary>
    public WasapiEndpointHoldSessionFactory()
        : this(new WasapiAudioEndpointSource(), new WasapiEndpointSessionOpener())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WasapiEndpointHoldSessionFactory"/> class.
    /// </summary>
    public WasapiEndpointHoldSessionFactory(
        IAudioEndpointSource endpointSource,
        IAudioEndpointSessionOpener sessionOpener)
    {
        this.endpointSource = endpointSource ?? throw new ArgumentNullException(nameof(endpointSource));
        this.sessionOpener = sessionOpener ?? throw new ArgumentNullException(nameof(sessionOpener));
    }

    /// <inheritdoc />
    public async Task<IAudioEndpointSession> OpenAsync(
        AudioDeviceSelection selection,
        KeepAliveMode mode,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(selection);
        cancellationToken.ThrowIfCancellationRequested();

        if (mode != KeepAliveMode.HoldOnly)
        {
            throw new NotSupportedException("Only HoldOnly mode is supported by this session factory.");
        }

        string endpointId = await ResolveEndpointIdAsync(selection, cancellationToken).ConfigureAwait(false);
        return await sessionOpener.OpenHoldSessionAsync(endpointId, cancellationToken).ConfigureAwait(false);
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
