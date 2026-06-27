# UI / UX Specification

## Design Goals

The UI should be plain, small, and trustworthy.

This is a utility, not a media player.

Avoid flashy design.

---

## Main Window Wireframe

```text
+------------------------------------------------+
| HDMIKeepAlive                                  |
+------------------------------------------------+
| Target Device                                  |
| [ Sony Soundbar (Intel Display Audio)      v ] |
|                                                |
| Mode                                           |
| ( ) Hold Only                                  |
| (x) Silent PCM                                 |
| ( ) Auto                                      |
|                                                |
| Status                                         |
| State: Running                                 |
| Format: 48 kHz / 16-bit / Stereo              |
| WASAPI: Shared Mode                            |
| Reconnects: 0                                  |
| Uptime: 02:14:33                               |
|                                                |
| [ Start ] [ Stop ] [ Restart Engine ]          |
|                                                |
| [x] Start with Windows                         |
| [x] Minimize to Tray                           |
| [ ] Enable Logging                             |
|                                                |
| [ Diagnostics ] [ Open Logs ] [ About ]        |
+------------------------------------------------+
```

---

## Tray Menu

```text
HDMIKeepAlive - Running
-----------------------
Open
Pause
Resume
Restart Engine
Diagnostics
Settings
Exit
```

---

## Status Colors

Green:

Running

Yellow:

Starting or Reconnecting

Red:

Faulted or Device Missing

Gray:

Stopped

---

## Diagnostics Window

```text
Endpoint
  Name:
  Device ID:
  State:
  Target Mode:

Audio
  WASAPI Mode:
  Format:
  Buffer:
  Latency:
  Frames Rendered:

Runtime
  Uptime:
  Reconnect Count:
  Last Error:
  CPU:
  Memory:
```

---

## UX Rules

- Exiting must require explicit Exit from tray or app menu.
- Closing main window should minimize to tray when enabled.
- Do not show noisy notifications.
- Show notification only on meaningful state changes.
- Never steal focus.
