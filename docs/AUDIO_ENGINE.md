# Audio Engine Specification

## Purpose

The audio engine keeps a selected playback endpoint active so that downstream HDMI, S/PDIF, soundbar, TV, AVR, or extractor devices do not lose synchronization during silence.

---

## Required API

Use WASAPI Shared Mode.

Do not use Exclusive Mode.

---

## Modes

### Hold Only Mode

Open the endpoint and keep it open without rendering audio.

Purpose:

Determine whether endpoint activation alone prevents idle timeout.

### Silent PCM Mode

Render zero-valued PCM buffers continuously.

Purpose:

Keep endpoint clock and stream active if Hold Only Mode is insufficient.

### Auto Mode

Future mode.

Try Hold Only first. If endpoint still sleeps, switch to Silent PCM.

---

## Format Strategy

Preferred:

Use the endpoint mix format.

Fallback:

48kHz, 16-bit, stereo PCM.

The app must not force an unsupported format.

---

## Silence Definition

Digital silence means every rendered sample is zero.

Examples:

- 16-bit PCM: `0x0000`
- 32-bit float PCM: `0.0f`

No dither, no noise, no test tone.

---

## Buffer Strategy

Use small, stable buffers.

Avoid CPU spin.

Avoid sleeping so long that buffers underrun.

Recommended approach:

- Initialize shared-mode audio client.
- Query buffer duration.
- Use event-driven rendering if available.
- Otherwise use a conservative periodic render loop.

---

## Reconnect Strategy

When endpoint becomes unavailable:

1. Stop current stream.
2. Dispose audio resources.
3. Wait short retry interval.
4. Re-enumerate devices.
5. Resolve target:
   - Same device ID if available
   - Default device if configured
   - Friendly-name match as fallback
6. Restart engine.

---

## Teams and Other Apps

The engine must use Shared Mode so Windows Audio Engine mixes the keep-alive stream with normal app audio.

Because the stream is zero-valued silence, mixing produces no audible change.

---

## Failure Cases

- Device unavailable
- Unsupported format
- Audio client initialization failure
- Access denied
- Device invalidated
- Buffer errors
- COM exception
- KVM device disappears
- Dock reconnect changes device ID

All failures must be logged and surfaced in diagnostics.

---

## Validation Procedure

1. Select target HDMI endpoint.
2. Start Hold Only Mode.
3. Wait 10 minutes.
4. Press Windows Sound Test.
5. If audio delay persists, switch to Silent PCM Mode.
6. Wait 10 minutes.
7. Press Windows Sound Test.
8. Confirm immediate playback.
