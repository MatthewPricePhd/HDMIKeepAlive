# Test Plan

## Test Philosophy

The product solves a hardware-adjacent timing issue. Testing must include both automated tests and manual validation on real devices.

---

## Automated Unit Tests

### Settings

- Load valid settings.
- Missing settings creates default.
- Invalid JSON falls back safely.
- Schema migration placeholder works.

### Engine State

- Start transitions state correctly.
- Stop transitions state correctly.
- Fault transitions state correctly.
- Reconnect increments count.

### Device Selection

- Default device selection.
- Specific device selection.
- Friendly name fallback.
- Missing device behavior.

---

## Integration Tests

Where possible:

- Enumerate real audio devices.
- Open default render endpoint.
- Open selected endpoint.
- Start and stop engine.
- Verify no exclusive lock.

---

## Manual Hardware Test Matrix

### Test Setup A

Windows laptop -> Dock -> HDMI KVM -> HDMI Extractor -> Optical -> Sony Soundbar

Expected:

No audio delay after 10 minutes idle.

### Test Setup B

Windows laptop -> HDMI Monitor with speakers

Expected:

No audio delay after 10 minutes idle.

### Test Setup C

Windows laptop -> USB DAC

Expected:

No negative side effects.

### Test Setup D

Windows laptop -> Bluetooth headphones

Expected:

Application should not target Bluetooth unless selected.

---

## Functional Tests

### FT-001 Initial Launch

App starts without admin rights.

### FT-002 Device Enumeration

All playback devices appear.

### FT-003 Shared Mode

Start HDMIKeepAlive. Play YouTube. Join Teams. Both work.

### FT-004 Delay Prevention

Wait 10 minutes. Press Windows Test. Audio should start immediately.

### FT-005 Dock Reconnect

Unplug dock. Reconnect. App recovers.

### FT-006 KVM Switch

Switch away and back. App recovers.

### FT-007 Sleep Wake

Sleep laptop. Wake. App recovers.

### FT-008 Lock Unlock

Lock workstation. Unlock. App still running.

### FT-009 Reboot

Restart. App launches at logon if enabled.

---

## Performance Tests

### PT-001 24-Hour Runtime

Run app for 24 hours.

Pass:

- No crash
- Memory stable
- CPU below target

### PT-002 CPU

Steady state below 0.05% target.

### PT-003 Memory

Steady state below 20 MB acceptable.

---

## Enterprise Safety Tests

- No admin prompt.
- No new service created.
- No registry writes required.
- No network traffic.
- No microphone permission requested.
- No EDR-like suspicious behavior.
