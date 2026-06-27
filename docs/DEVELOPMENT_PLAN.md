# Development Plan

## Strategy

Build the product in controlled milestones. Each milestone should leave the repository buildable and testable.

Do not attempt to build the entire app in one pass.

---

## Milestone 0: Repository Bootstrap

### Goal

Create a clean .NET solution and repository structure.

### Tasks

- Create solution file.
- Create projects:
  - HDMIKeepAlive.UI
  - HDMIKeepAlive.Core
  - HDMIKeepAlive.Audio
  - HDMIKeepAlive.Diagnostics
  - HDMIKeepAlive.Infrastructure
  - HDMIKeepAlive.Tests
- Configure nullable reference types.
- Configure analyzers.
- Configure StyleCop if feasible.
- Configure GitHub Actions CI.
- Add README.
- Add MIT license.
- Add docs folder.

### Exit Criteria

- `dotnet build` succeeds.
- `dotnet test` succeeds.
- CI builds on push.
- No application logic required yet.

---

## Milestone 1: Audio Device Enumeration

### Goal

Enumerate Windows playback endpoints.

### Tasks

- Implement `IAudioDeviceEnumerator`.
- List active render endpoints.
- Return device ID, friendly name, state, and default flag.
- Add unit-testable abstraction.
- Add console or diagnostic test path.

### Exit Criteria

- Running the app shows available playback devices.
- HDMI/Display Audio endpoints are visible.
- Tests cover empty list and sample device list using mocks.

---

## Milestone 2: Research Endpoint Hold Mode

### Goal

Test whether opening and holding a WASAPI endpoint is enough.

### Tasks

- Implement `IAudioKeepAliveEngine`.
- Add `EndpointHoldKeepAliveEngine`.
- Open endpoint in Shared Mode.
- Do not render audio.
- Keep endpoint open.
- Expose state.
- Add start/stop lifecycle.

### Exit Criteria

- User can choose endpoint and start "Hold Only" mode.
- App shows endpoint open state.
- User can test Windows Test sound after idle.

---

## Milestone 3: Silent PCM Engine

### Goal

Render digital silence if hold-only is insufficient.

### Tasks

- Implement `SilentPcmKeepAliveEngine`.
- Use WASAPI Shared Mode.
- Use endpoint mix format or safe 48kHz stereo PCM.
- Render zeroed buffers.
- Avoid exclusive access.
- Avoid audible artifacts.
- Handle buffer underruns.

### Exit Criteria

- Endpoint remains active indefinitely.
- No audio is audible.
- Other apps can play audio normally.
- YouTube/Windows Test no longer delay after idle.

---

## Milestone 4: Reconnect and Device Events

### Goal

Recover automatically from device changes.

### Tasks

- Listen for endpoint changes.
- Detect default device change.
- Detect device removal.
- Detect target device reappearance.
- Implement retry loop.
- Track reconnect count.

### Exit Criteria

- Unplug/replug dock and app reconnects.
- KVM switching does not permanently break the engine.
- App recovers after sleep/wake.

---

## Milestone 5: Settings

### Goal

Persist user preferences.

### Tasks

- Create settings model.
- Store JSON in `%LOCALAPPDATA%\HDMIKeepAlive\settings.json`.
- Include schema version.
- Validate settings.
- Handle missing or malformed settings.

### Exit Criteria

- Selected device persists.
- Mode persists.
- Logging preference persists.
- App starts with last known configuration.

---

## Milestone 6: UI

### Goal

Create a simple WinUI 3 interface.

### Tasks

- Implement MVVM.
- Device selector.
- Mode selector:
  - Hold Only
  - Silent PCM
  - Auto
- Status display.
- Diagnostics panel.
- Start/stop controls.
- Settings controls.

### Exit Criteria

- User can configure app without editing files.
- App remains responsive.
- No UI thread blocking.

---

## Milestone 7: Tray App

### Goal

Run unobtrusively.

### Tasks

- Add tray icon.
- Add context menu.
- Hide to tray.
- Start minimized.
- Show status through icon or tooltip.

### Exit Criteria

- App can run without visible window.
- Tray menu can pause/resume/exit.

---

## Milestone 8: Startup

### Goal

Start at logon without admin rights.

### Tasks

- Implement startup folder shortcut.
- Optionally implement per-user scheduled task.
- Never require elevated startup.
- Allow user to disable startup.

### Exit Criteria

- App restarts after reboot.
- No admin prompt.

---

## Milestone 9: Observability and Logs

### Goal

Make behavior inspectable.

### Tasks

- Add structured logging.
- Log device changes.
- Log engine transitions.
- Log reconnect attempts.
- Add optional log viewer or log path button.

### Exit Criteria

- Logs explain what happened after a device loss.
- Logging can be disabled.

---

## Milestone 10: Packaging and Release

### Goal

Create a usable release.

### Tasks

- Publish framework-dependent build.
- Publish self-contained build.
- Create release zip.
- Add checksums.
- Update README.
- Add GitHub release notes.

### Exit Criteria

- User can download, unzip, and run.
- No installer required.
