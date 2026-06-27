using System.Runtime.InteropServices;
using HDMIKeepAlive.Core.Models;

namespace HDMIKeepAlive.Audio;

/// <summary>
/// Reads Windows playback endpoints using Core Audio MMDevice APIs.
/// </summary>
public sealed class WasapiAudioEndpointSource : IAudioEndpointSource
{
    private const int DeviceStateActive = 0x00000001;
    private const int StgmRead = 0x00000000;

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

    private static IReadOnlyList<AudioEndpointSnapshot> GetPlaybackEndpoints()
    {
        var endpoints = new List<AudioEndpointSnapshot>();
        var enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumerator();
        enumerator.EnumAudioEndpoints(EDataFlow.ERender, DeviceStateActive, out IMMDeviceCollection collection);
        collection.GetCount(out uint count);

        for (uint index = 0; index < count; index++)
        {
            collection.Item(index, out IMMDevice device);
            endpoints.Add(CreateSnapshot(device));
        }

        return endpoints;
    }

    private static string? GetDefaultPlaybackEndpointId()
    {
        var enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumerator();
        enumerator.GetDefaultAudioEndpoint(EDataFlow.ERender, ERole.EConsole, out IMMDevice device);
        device.GetId(out string id);
        return id;
    }

    private static AudioEndpointSnapshot CreateSnapshot(IMMDevice device)
    {
        device.GetId(out string id);
        device.GetState(out int state);
        device.OpenPropertyStore(StgmRead, out IPropertyStore propertyStore);

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

    private enum EDataFlow
    {
        ERender = 0,
        ECapture = 1,
        EAll = 2
    }

    private enum ERole
    {
        EConsole = 0,
        EMultimedia = 1,
        ECommunications = 2
    }

    [ComImport]
    [Guid("bcde0395-e52f-467c-8e3d-c4579291692e")]
    private sealed class MMDeviceEnumerator
    {
    }

    [ComImport]
    [Guid("a95664d2-9614-4f35-a746-de8db63617e6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(EDataFlow dataFlow, int stateMask, out IMMDeviceCollection devices);

        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
    }

    [ComImport]
    [Guid("0bd7a1be-7a1a-44db-8397-cc5392387b5e")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        void GetCount(out uint count);

        void Item(uint deviceNumber, out IMMDevice device);
    }

    [ComImport]
    [Guid("d666063f-1587-4e43-81f1-b948e807363f")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        void Activate(
            ref Guid interfaceId,
            int classContext,
            IntPtr activationParams,
            [MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);

        void OpenPropertyStore(int accessMode, out IPropertyStore properties);

        void GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);

        void GetState(out int state);
    }

    [ComImport]
    [Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint propertyCount);

        void GetAt(uint propertyIndex, out PropertyKey key);

        void GetValue(ref PropertyKey key, out PropVariant value);

        void SetValue(ref PropertyKey key, ref PropVariant value);

        void Commit();
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PropertyKey
    {
        public PropertyKey(Guid formatId, int propertyId)
        {
            formatIdField = formatId;
            propertyIdField = propertyId;
        }

        private readonly Guid formatIdField;

        private readonly int propertyIdField;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        private ushort valueType;
        private ushort reserved1;
        private ushort reserved2;
        private ushort reserved3;
        private IntPtr value;
        private int value2;

        public string? GetString()
        {
            const ushort vtLpwstr = 31;
            return valueType == vtLpwstr && value != IntPtr.Zero
                ? Marshal.PtrToStringUni(value)
                : null;
        }
    }
}
