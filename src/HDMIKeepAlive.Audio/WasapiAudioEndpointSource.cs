using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Reads Windows playback endpoints using Core Audio MMDevice APIs.
/// </summary>
public sealed class WasapiAudioEndpointSource : IAudioEndpointSource
{
    /// <inheritdoc />
    public Task<IReadOnlyList<AudioEndpointSnapshot>> GetPlaybackEndpointsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<IReadOnlyList<AudioEndpointSnapshot>>(Array.Empty<AudioEndpointSnapshot>());
        }

        return Task.FromResult<IReadOnlyList<AudioEndpointSnapshot>>(GetPlaybackEndpoints());
    }

    /// <inheritdoc />
    public Task<string?> GetDefaultPlaybackEndpointIdAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            return Task.FromResult<string?>(null);
        }

        return Task.FromResult(GetDefaultPlaybackEndpointId());
    }

    [SupportedOSPlatform("windows")]
    private static IReadOnlyList<AudioEndpointSnapshot> GetPlaybackEndpoints()
    {
        var endpoints = new List<AudioEndpointSnapshot>();
        var enumerator = CoreAudioInterop.CreateDeviceEnumerator();
        enumerator.EnumAudioEndpoints(EDataFlow.ERender, CoreAudioInterop.DeviceStateActive, out IMMDeviceCollection collection);
        collection.GetCount(out uint count);

        for (uint index = 0; index < count; index++)
        {
            collection.Item(index, out IMMDevice device);
            endpoints.Add(CreateSnapshot(device));
        }

        return endpoints;
    }

    [SupportedOSPlatform("windows")]
    private static string? GetDefaultPlaybackEndpointId()
    {
        var enumerator = CoreAudioInterop.CreateDeviceEnumerator();
        enumerator.GetDefaultAudioEndpoint(EDataFlow.ERender, ERole.EConsole, out IMMDevice device);
        device.GetId(out string id);
        return id;
    }

    private static AudioEndpointSnapshot CreateSnapshot(IMMDevice device)
    {
        device.GetId(out string id);
        device.GetState(out int state);
        device.OpenPropertyStore(CoreAudioInterop.StgmRead, out IPropertyStore propertyStore);

        string friendlyName = GetPropertyString(propertyStore, PropertyKeys.DeviceFriendlyName) ?? id;
        string? interfaceName = GetPropertyString(propertyStore, PropertyKeys.DeviceInterfaceFriendlyName);

        return new AudioEndpointSnapshot(
            id,
            friendlyName,
            ToAudioDeviceState(state),
            interfaceName,
            SampleRate: null,
            BitDepth: null,
            Channels: null);
    }

    private static AudioDeviceState ToAudioDeviceState(int state)
    {
        return state switch
        {
            0x00000001 => AudioDeviceState.Active,
            0x00000002 => AudioDeviceState.Disabled,
            0x00000004 => AudioDeviceState.NotPresent,
            0x00000008 => AudioDeviceState.Unplugged,
            _ => AudioDeviceState.Unknown
        };
    }

    private static string? GetPropertyString(IPropertyStore propertyStore, PropertyKey propertyKey)
    {
        propertyStore.GetValue(ref propertyKey, out PropVariant value);
        return value.GetString();
    }

    private static class PropertyKeys
    {
        public static PropertyKey DeviceFriendlyName { get; } = new(
            new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"),
            14);

        public static PropertyKey DeviceInterfaceFriendlyName { get; } = new(
            new Guid("026e516e-b814-414b-83cd-856d6fef4822"),
            2);
    }

}
