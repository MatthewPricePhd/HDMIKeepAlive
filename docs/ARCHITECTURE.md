# Architecture

## Architectural Principles

1. Keep audio code isolated.
2. UI must never directly call WASAPI.
3. Use interfaces at project boundaries.
4. Prefer simple, inspectable code over clever code.
5. Avoid global mutable state.
6. Keep the app portable and user-space only.
7. Every long-running component must support cancellation.

---

## Project Layout

```text
src/
  HDMIKeepAlive.UI/
  HDMIKeepAlive.Core/
  HDMIKeepAlive.Audio/
  HDMIKeepAlive.Diagnostics/
  HDMIKeepAlive.Infrastructure/

tests/
  HDMIKeepAlive.Tests/
```

---

## Dependency Direction

```text
HDMIKeepAlive.UI
  -> HDMIKeepAlive.Core
  -> HDMIKeepAlive.Diagnostics
  -> HDMIKeepAlive.Infrastructure

HDMIKeepAlive.Audio
  -> HDMIKeepAlive.Core

HDMIKeepAlive.Infrastructure
  -> HDMIKeepAlive.Core

HDMIKeepAlive.Tests
  -> all projects as needed
```

The Core project must not reference UI or Windows-specific implementation projects.

---

## Core Interfaces

### IAudioDeviceEnumerator

```csharp
public interface IAudioDeviceEnumerator
{
    Task<IReadOnlyList<AudioDeviceInfo>> GetPlaybackDevicesAsync(CancellationToken cancellationToken);
    Task<AudioDeviceInfo?> GetDefaultPlaybackDeviceAsync(CancellationToken cancellationToken);
}
```

### IAudioKeepAliveEngine

```csharp
public interface IAudioKeepAliveEngine : IAsyncDisposable
{
    KeepAliveEngineState State { get; }
    event EventHandler<KeepAliveStateChangedEventArgs>? StateChanged;

    Task StartAsync(AudioDeviceSelection selection, KeepAliveMode mode, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

### ISettingsStore

```csharp
public interface ISettingsStore
{
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken);
}
```

### IStartupManager

```csharp
public interface IStartupManager
{
    Task<bool> IsEnabledAsync(CancellationToken cancellationToken);
    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken);
}
```

---

## Threading Model

- UI work occurs on UI thread.
- Audio engine runs on background thread/task.
- Device notification callbacks must marshal updates safely.
- Long-running tasks must accept CancellationToken.
- No blocking waits on async operations.

---

## Lifecycle

```text
App starts
  -> Load settings
  -> Enumerate devices
  -> Restore selected target
  -> Start engine if enabled
  -> Monitor device changes
  -> Reconnect as needed
```

---

## Error Handling

Errors should be:

- Captured
- Logged
- Reported to UI
- Non-fatal where possible

The app should not crash because an endpoint disappears.

---

## State Machine

```text
Stopped
  -> Starting
  -> Running
  -> Reconnecting
  -> Faulted
  -> Stopping
  -> Stopped
```

---

## Implementation Notes

The first implementation may use a Windows audio library if justified, but the architecture must not leak that dependency into Core or UI.

Suitable libraries may include:

- NAudio for WASAPI access
- Vanara for Windows interop
- Direct COM interop if keeping dependencies minimal

If a library is used, document the decision in an ADR.
