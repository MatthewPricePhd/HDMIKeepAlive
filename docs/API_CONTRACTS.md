# API and Interface Contracts

## Core Data Models

### AudioDeviceInfo

```csharp
public sealed record AudioDeviceInfo(
    string Id,
    string FriendlyName,
    bool IsDefault,
    AudioDeviceState State,
    string? InterfaceName,
    int? SampleRate,
    int? BitDepth,
    int? Channels);
```

### AudioDeviceSelection

```csharp
public sealed record AudioDeviceSelection(
    AudioTargetMode TargetMode,
    string? DeviceId,
    string? FriendlyNameFallback);
```

### KeepAliveMode

```csharp
public enum KeepAliveMode
{
    HoldOnly,
    SilentPcm,
    Auto
}
```

### KeepAliveEngineState

```csharp
public enum KeepAliveEngineState
{
    Stopped,
    Starting,
    Running,
    Reconnecting,
    Faulted,
    Stopping
}
```

### EngineDiagnostics

```csharp
public sealed record EngineDiagnostics(
    string? EndpointName,
    string? EndpointId,
    KeepAliveMode Mode,
    KeepAliveEngineState State,
    int? SampleRate,
    int? BitDepth,
    int? Channels,
    TimeSpan? BufferDuration,
    TimeSpan? Latency,
    long FramesRendered,
    int ReconnectCount,
    string? LastError,
    DateTimeOffset? LastStateChange);
```

---

## Service Contracts

### IAudioKeepAliveEngine

The engine owns the endpoint lifecycle.

Rules:

- StartAsync is idempotent where possible.
- StopAsync must be safe if already stopped.
- Disposal must release endpoint resources.
- StateChanged must be raised on meaningful transitions.

### IDeviceChangeMonitor

Reports system audio device changes.

### ISettingsStore

Reads and writes settings.

### IDiagnosticsProvider

Aggregates engine and process diagnostics.

### ILogger

Use Microsoft.Extensions.Logging abstractions.
