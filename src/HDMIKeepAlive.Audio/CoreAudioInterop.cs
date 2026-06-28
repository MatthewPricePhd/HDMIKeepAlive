using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace HDMIKeepAlive.Audio;

internal static class CoreAudioInterop
{
    public const int ClsctxAll = 23;
    public const int DeviceStateActive = 0x00000001;
    public const int StgmRead = 0x00000000;
    private static readonly Guid MMDeviceEnumeratorClassId = new("bcde0395-e52f-467c-8e3d-c4579291692e");

    [SupportedOSPlatform("windows")]
    public static IMMDeviceEnumerator CreateDeviceEnumerator()
    {
        Type enumeratorType = Type.GetTypeFromCLSID(MMDeviceEnumeratorClassId, throwOnError: true)!;
        object enumeratorObject = Activator.CreateInstance(enumeratorType)
            ?? throw new InvalidOperationException("Unable to create the Windows Core Audio device enumerator.");

        IntPtr unknown = Marshal.GetIUnknownForObject(enumeratorObject);
        try
        {
            return (IMMDeviceEnumerator)Marshal.GetTypedObjectForIUnknown(unknown, typeof(IMMDeviceEnumerator));
        }
        finally
        {
            Marshal.Release(unknown);
        }
    }
}

internal enum EDataFlow
{
    ERender = 0,
    ECapture = 1,
    EAll = 2
}

internal enum ERole
{
    EConsole = 0,
    EMultimedia = 1,
    ECommunications = 2
}

internal enum AudioClientShareMode
{
    Shared = 0
}

[ComImport]
[Guid("a95664d2-9614-4f35-a746-de8db63617e6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    void EnumAudioEndpoints(EDataFlow dataFlow, int stateMask, out IMMDeviceCollection devices);

    void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);

    void GetDevice(
        [MarshalAs(UnmanagedType.LPWStr)] string id,
        out IMMDevice device);

    void RegisterEndpointNotificationCallback(IMMNotificationClient client);

    void UnregisterEndpointNotificationCallback(IMMNotificationClient client);
}

[ComImport]
[Guid("0bd7a1be-7a1a-44db-8397-cc5392387b5e")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    void GetCount(out uint count);

    void Item(uint deviceNumber, out IMMDevice device);
}

[ComImport]
[Guid("d666063f-1587-4e43-81f1-b948e807363f")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
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
internal interface IPropertyStore
{
    void GetCount(out uint propertyCount);

    void GetAt(uint propertyIndex, out PropertyKey key);

    void GetValue(ref PropertyKey key, out PropVariant value);

    void SetValue(ref PropertyKey key, ref PropVariant value);

    void Commit();
}

[ComImport]
[Guid("1cb9ad4c-dbfa-4c32-b178-c2f568a703b2")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioClient
{
    void Initialize(
        AudioClientShareMode shareMode,
        int streamFlags,
        long hnsBufferDuration,
        long hnsPeriodicity,
        IntPtr mixFormat,
        IntPtr audioSessionGuid);

    void GetBufferSize(out uint bufferFrameCount);

    void GetStreamLatency(out long latency);

    void GetCurrentPadding(out uint currentPadding);

    void IsFormatSupported(
        AudioClientShareMode shareMode,
        IntPtr mixFormat,
        out IntPtr closestMatch);

    void GetMixFormat(out IntPtr deviceFormat);

    void GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);

    void Start();

    void Stop();

    void Reset();

    void SetEventHandle(IntPtr eventHandle);

    void GetService(ref Guid interfaceId, [MarshalAs(UnmanagedType.IUnknown)] out object service);
}

[ComImport]
[Guid("f294acfc-3146-4483-a7bf-addca7c260e2")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IAudioRenderClient
{
    void GetBuffer(uint requestedFrames, out IntPtr data);

    void ReleaseBuffer(uint writtenFrames, int flags);
}

[ComImport]
[Guid("7991eec9-7e89-4d85-8390-6c703cec60c0")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMNotificationClient
{
    void OnDeviceStateChanged(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        int newState);

    void OnDeviceAdded([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    void OnDeviceRemoved([MarshalAs(UnmanagedType.LPWStr)] string deviceId);

    void OnDefaultDeviceChanged(
        EDataFlow flow,
        ERole role,
        [MarshalAs(UnmanagedType.LPWStr)] string? defaultDeviceId);

    void OnPropertyValueChanged(
        [MarshalAs(UnmanagedType.LPWStr)] string deviceId,
        PropertyKey key);
}

[StructLayout(LayoutKind.Sequential)]
internal readonly struct PropertyKey
{
    public PropertyKey(Guid formatId, int propertyId)
    {
        FormatId = formatId;
        PropertyId = propertyId;
    }

    private Guid FormatId { get; }

    private int PropertyId { get; }
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropVariant
{
    private ushort valueType;
    private ushort reserved1;
    private ushort reserved2;
    private ushort reserved3;
    private IntPtr value;
    private int value2;

    public readonly string? GetString()
    {
        const ushort vtLpwstr = 31;
        return valueType == vtLpwstr && value != IntPtr.Zero
            ? Marshal.PtrToStringUni(value)
            : null;
    }
}
