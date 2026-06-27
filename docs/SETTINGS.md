# Settings Specification

## Location

Settings file:

```text
%LOCALAPPDATA%\HDMIKeepAlive\settings.json
```

## Example

```json
{
  "schemaVersion": 1,
  "startWithWindows": true,
  "startMinimized": true,
  "minimizeToTray": true,
  "targetMode": "SpecificDevice",
  "targetDeviceId": "SWD\\MMDEVAPI\\...",
  "targetFriendlyNameFallback": "Sony Soundbar",
  "keepAliveMode": "SilentPcm",
  "loggingEnabled": false,
  "reconnectIntervalSeconds": 5,
  "showNotifications": true
}
```

## Fields

### schemaVersion

Integer. Used for future migration.

Default: 1

### startWithWindows

Boolean.

Default: false

### startMinimized

Boolean.

Default: true

### minimizeToTray

Boolean.

Default: true

### targetMode

Enum:

- DefaultDevice
- SpecificDevice
- HdmiDevicesOnly

Default: DefaultDevice

### targetDeviceId

String or null.

### targetFriendlyNameFallback

String or null.

Used if device ID changes after reconnect.

### keepAliveMode

Enum:

- HoldOnly
- SilentPcm
- Auto

Default: SilentPcm after research is complete. During initial testing, default may be HoldOnly.

### loggingEnabled

Boolean.

Default: false

### reconnectIntervalSeconds

Integer.

Default: 5

Minimum: 1

Maximum: 300

### showNotifications

Boolean.

Default: true
