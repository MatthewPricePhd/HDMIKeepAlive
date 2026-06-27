# Observability and Diagnostics

## Goals

Users need to know whether the app is actually keeping the endpoint active.

## Diagnostics Data

Show:

- App version
- Uptime
- Selected endpoint
- Endpoint state
- Device ID
- Friendly name
- WASAPI mode
- Keep-alive mode
- Current format
- Buffer duration
- Stream latency
- Frames rendered
- Reconnect count
- Last reconnect time
- Last error
- CPU usage
- Memory usage

## Log Events

Log:

- AppStart
- AppExit
- SettingsLoaded
- SettingsSaved
- DeviceEnumerated
- DeviceSelected
- EngineStarting
- EngineRunning
- EngineStopped
- EngineFaulted
- DeviceLost
- DeviceRecovered
- ReconnectAttempt
- ReconnectSucceeded
- ReconnectFailed

## Log Location

```text
%LOCALAPPDATA%\HDMIKeepAlive\logs\
```

Logging disabled by default.
