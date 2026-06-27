# Known Research Findings

This document records the investigation that motivated HDMIKeepAlive.

## Observed Hardware Chain

```text
Dell Precision 3581
  -> Dell SD25TB4 Thunderbolt Dock
  -> TESmart HKS401-M24 HDMI KVM
  -> OREI BKA-1 HDMI Audio Extractor
  -> Optical S/PDIF
  -> Sony Soundbar
```

## Observed Symptom

When Windows output is set to the HDMI/Display Audio endpoint, playback after idle has an approximately 2-second delay.

Examples:

- Windows Sound Test delayed
- First YouTube video after silence delayed
- Immediate playback when switching videos while audio is already active

## Controls

### Mac Control

A Mac using the same downstream KVM, OREI extractor, optical cable, and Sony soundbar does not exhibit the delay.

Inference:

Downstream hardware can play immediately if the source keeps the audio stream active.

### Laptop Speaker Control

Windows laptop internal speakers play immediately.

Inference:

Windows general audio engine is not the root cause.

### TESmart Analog Output Control

TESmart 3.5mm analog output plays immediately using USB audio driver.

Inference:

TESmart KVM is not the root cause.

### HDMI Soundbar Endpoint

Windows HDMI/Display Audio endpoint delays after idle.

Inference:

The failing path is specific to HDMI/Display Audio endpoint behavior.

## Windows Endpoint Capabilities

The endpoint reports:

- 2-channel PCM
- No DTS
- No Dolby Digital
- No 5.1 encoded format

Inference:

The issue is not Dolby/DTS/bitstream negotiation.

## Driver Findings

Intel Display Audio driver:

- Provider: Intel Corporation
- Driver: ACX HD Audio / AcxHdAudio.sys
- INF: oem233.inf
- Service: AcxHdAudio
- Device uses Windows ACX audio framework

## Registry Findings

Active device parameters include:

```text
WdfDirectedPowerTransitionEnable = 1
IdleInWorkingState = 1
```

Inference:

The audio device is allowed to enter an idle state while the system remains awake.

The installed driver section did not expose a user-editable `PerformanceIdleTime` value for the active device.

## Energy Report

`powercfg /energy` did not expose HDMI audio-specific idle settings.

Inference:

The behavior is likely managed inside the driver/framework rather than a visible Windows power policy.

## Working Hypothesis

Windows/Intel ACX HDMI audio stops transmitting HDMI audio packets after idle. The OREI loses HDMI audio carrier and stops generating S/PDIF clock. The Sony soundbar then must relock when audio resumes.

## Proposed Workaround

Open the endpoint in WASAPI Shared Mode and, if needed, continuously render digital silence to keep the HDMI audio stream active.
