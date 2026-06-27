# Product Requirements Document

# HDMIKeepAlive

Version: 2.0 Documentation Draft  
Target Product Version: 1.0  
Status: Ready for implementation planning

---

## 1. Executive Summary

HDMIKeepAlive is a lightweight Windows desktop utility that prevents HDMI audio endpoints from sleeping or dropping their audio carrier during periods of silence.

Many users with soundbars, AV receivers, HDMI audio extractors, HDMI KVMs, Thunderbolt docks, and HTPC setups experience a delay when playback resumes after silence. The result is often a clipped notification sound, a missing first word in a video, or a 1–2 second delay before audio begins.

The utility solves this by keeping the selected Windows playback endpoint active using standard Windows audio APIs.

The application must be enterprise-safe, transparent, offline, and non-invasive.

---

## 2. Problem Statement

On some Windows systems, HDMI or DisplayPort audio endpoints enter an idle or low-power state when no application is producing audio. When playback resumes, the endpoint, downstream HDMI device, audio extractor, S/PDIF transmitter, soundbar, or AV receiver must re-lock to the stream.

This re-lock process can take 0.5–2 seconds.

The issue is especially visible in chains such as:

```text
Windows PC
  -> USB-C / Thunderbolt Dock
  -> HDMI KVM
  -> HDMI Audio Extractor
  -> Optical S/PDIF
  -> Soundbar
```

The same hardware may behave correctly on macOS because macOS may keep the HDMI audio stream alive with silent PCM, while Windows may stop transmitting HDMI audio packets when idle.

---

## 3. Target Users

Primary users:

- Users with HDMI audio delay after silence
- HTPC users
- Soundbar users
- AV receiver users
- HDMI audio extractor users
- KVM users
- Thunderbolt dock users
- Windows power users
- Corporate laptop users who cannot install drivers or services

Secondary users:

- Audio/video troubleshooters
- Home lab users
- IT support technicians
- Streamers
- Conference room support teams
- Accessibility users affected by clipped alerts

---

## 4. Product Goals

### 4.1 Primary Goal

Prevent HDMI audio endpoints from losing synchronization during idle periods.

### 4.2 Secondary Goals

- Preserve normal audio behavior.
- Avoid exclusive device access.
- Avoid interfering with Teams, Zoom, browsers, games, media players, or system audio.
- Avoid administrator privileges.
- Avoid registry modifications.
- Avoid drivers and services.
- Maintain low resource usage.
- Provide diagnostics.
- Provide a clear open-source codebase.

---

## 5. Non-Goals

HDMIKeepAlive will not:

- Record audio.
- Access microphones.
- Enhance audio.
- Equalize audio.
- Route audio between devices.
- Create virtual audio devices.
- Replace audio drivers.
- Modify Windows registry settings.
- Install kernel drivers.
- Inject DLLs.
- Hook system APIs.
- Require network access.
- Require cloud services.
- Bypass enterprise security controls.

---

## 6. Core Functional Requirements

### FR-001 Playback Device Enumeration

The application must enumerate active playback devices and display useful metadata:

- Friendly name
- Device ID
- Device state
- Default status
- Data flow
- Current mix format if available
- Channel count
- Sample rate
- Bit depth
- Device interface where available

### FR-002 Device Selection

The user must be able to select:

- Default playback device
- Specific playback device
- HDMI-like devices only

For v1.0, support one active target device.

### FR-003 Shared-Mode Endpoint Activation

The application must open the selected endpoint using WASAPI Shared Mode.

Exclusive Mode is prohibited.

### FR-004 Research Mode

Before continuous silent rendering is used, the application must implement a research mode that opens and holds the endpoint without rendering audio, then allows the user to test whether this is sufficient to prevent timeout.

### FR-005 Silent PCM Rendering

If holding the endpoint open is insufficient, the application must render digital silence using PCM buffers.

The rendered audio must be zero-valued samples.

### FR-006 Auto-Reconnect

The application must recover from:

- HDMI unplug/replug
- USB-C dock reconnect
- KVM input switching
- Default playback device change
- Device removal
- Device reappearance
- Sleep/wake
- Lock/unlock

### FR-007 Tray Operation

The application must run as a tray app.

Required tray actions:

- Open
- Pause
- Resume
- Restart audio engine
- Diagnostics
- Settings
- Exit

### FR-008 Startup

The application must support starting at user logon without admin rights.

Preferred approaches:

1. Startup folder shortcut
2. Per-user scheduled task

No Windows Service in v1.0.

### FR-009 Settings Persistence

Settings must be stored in a user-local JSON file.

No registry required.

### FR-010 Diagnostics

The application must show:

- Current endpoint
- Target endpoint mode
- WASAPI mode
- Stream state
- Format
- Buffer size
- Latency
- Reconnect count
- Last error
- CPU usage
- Memory usage
- Uptime

---

## 7. Non-Functional Requirements

### NFR-001 Performance

Target resource usage:

- CPU: under 0.05% during steady state
- RAM: under 20 MB acceptable, under 10 MB target
- Disk: under 50 MB for self-contained build preferred
- Startup: under 1 second after user logon

### NFR-002 Reliability

The app should run continuously for at least 24 hours without:

- Crashing
- Memory leaks
- Audio artifacts
- Device lockups
- Excessive logs
- UI freezes

### NFR-003 Security

The app must:

- Run as current user
- Use no elevation
- Use no network
- Use no telemetry
- Install no service
- Install no driver
- Modify no machine-level settings by default

### NFR-004 Enterprise Compatibility

The app should look like a normal user-space desktop utility.

Avoid behaviors that endpoint protection may flag:

- Code injection
- Shellcode
- Process hollowing
- Kernel access
- Scheduled tasks with elevated privileges
- Self-modifying binaries
- Suspicious persistence
- Obfuscated code
- Hidden network activity

### NFR-005 Privacy

The app must not collect user data.

Logs must stay local.

Logs must not contain audio content.

---

## 8. User Stories

### US-001

As a user with a soundbar, I want the first second of YouTube audio to play immediately after silence.

### US-002

As a corporate laptop user, I want a tool that does not require admin privileges.

### US-003

As a Teams user, I want the keep-alive tool not to interfere with calls.

### US-004

As a power user, I want to see diagnostics so I can verify which device is being kept alive.

### US-005

As a KVM user, I want the app to recover when switching between computers.

### US-006

As a dock user, I want the app to recover when I unplug and replug the dock.

---

## 9. Acceptance Criteria

### AC-001 HDMI Audio Delay Prevention

Given a Windows PC with delayed HDMI audio after idle, when HDMIKeepAlive is running against the target endpoint, then Windows audio test sounds and YouTube videos should begin immediately after at least 10 minutes of silence.

### AC-002 Shared Mode

Given Teams and HDMIKeepAlive are both active, Teams audio must continue playing normally.

### AC-003 No Exclusive Lock

No other audio application should receive an "audio device in use" error due to HDMIKeepAlive.

### AC-004 Restart Recovery

After reboot, if Start with Windows is enabled, the application should start automatically and resume keep-alive.

### AC-005 Device Recovery

After unplugging and reconnecting the dock, the app should reconnect without requiring manual restart.

### AC-006 Silent Output

The keep-alive stream must be inaudible.

### AC-007 No Admin

The application must run fully as a standard user.

---

## 10. Release Criteria

Version 1.0 is releasable when:

- Shared-mode engine works.
- Silent PCM rendering works when needed.
- UI allows device selection.
- Tray operation works.
- Settings persist.
- Reconnect works.
- README and docs are complete.
- Basic tests pass.
- A portable release build is available.
