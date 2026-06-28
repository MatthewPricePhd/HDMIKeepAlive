# HDMIKeepAlive — Windows UAT (User Acceptance Test)

**Build under test:** `HDMIKeepAlive-win-x64-self-contained` (console validation build, Milestone 0)
**Goal:** Confirm the app can enumerate playback endpoints and hold an HDMI/display audio path active, reducing or eliminating the clipped/delayed first second after silence.

> **This is a console build, not the final tray/WinUI app or installer.** It runs in a terminal window and stops with `Ctrl+C`. That is expected.

---

## 0. Before you start

You have already downloaded and unzipped the artifact. Confirm:

- [ ] The folder contains `HDMIKeepAlive.exe`.
- [ ] It is **self-contained** — no .NET install required.
- [ ] You can open **PowerShell** in that folder
      (in File Explorer: address bar → type `powershell` → Enter, or Shift+Right-click → "Open PowerShell window here").

What this build is allowed to do (by design — confirm nothing else happens):
no driver/service install, no registry writes, no network traffic, no microphone access. It only opens a **playback** endpoint.

### Mode reference

| Mode | Flag | What it does | Status |
|------|------|--------------|--------|
| Hold Only | `--mode HoldOnly` | Opens and holds the endpoint open, renders **no** audio | Primary test |
| Silent PCM | `--mode SilentPcm` | Renders **zero-valued** (silent) PCM buffers to keep the clock active | Secondary test |
| Auto | `--mode Auto` | **Not implemented — will error. Do not use.** | ❌ |

Targeting: `--device-id "<id>"` for a specific endpoint, or `--target hdmi` to auto-pick HDMI-like devices.

---

## 1. Show help (sanity check)

```powershell
.\HDMIKeepAlive.exe --help
```

**Expected:** usage text listing `--list-devices`, `--run --mode HoldOnly`, `--mode SilentPcm`, etc. If you see this, the exe runs on this machine.

---

## 2. List audio devices

```powershell
.\HDMIKeepAlive.exe --list-devices
```

**Expected output** — one block per playback device, e.g.:

```
Speakers (Realtek(R) Audio) [default]
  ID: {0.0.0.00000000}.{a1b2c3d4-...}
  State: Active
  Format: 48 kHz / 16-bit / 2 ch
LG TV (NVIDIA High Definition Audio)
  ID: {0.0.0.00000000}.{e5f6a7b8-...}
  State: Active
  Format: 48 kHz / 24-bit / 2 ch
```

**Do this:**
- [ ] Copy the **entire** output into the results template (Section 8, field *Device list*).
- [ ] Identify your HDMI / display / soundbar / AVR endpoint and copy its **full `ID:` string** — you'll paste it into the commands below.

> Tip: copy from PowerShell by selecting text and pressing **Enter**, or right-click the title bar → Edit → Mark.

---

## 3. Baseline (app NOT running)

This captures the problem you're trying to fix.

1. Make sure HDMIKeepAlive is **not** running.
2. Leave the machine silent for **10 minutes** (no audio at all).
3. Play audio (Windows "Test" button in Sound settings, a YouTube clip, Teams test call, or your usual app).
4. Note whether the **first ~1 second** is clipped, delayed, or fades in.

- [ ] Record the result in Section 8 (*Baseline*).

---

## 4. Test Hold Only mode

```powershell
.\HDMIKeepAlive.exe --run --mode HoldOnly --device-id "<PASTE DEVICE ID HERE>"
```

**Expected console output:**

```
Starting HoldOnly keep-alive. Press Ctrl+C to stop.
Keep-alive running.
```

**Do this:**
1. Leave it running **10+ minutes**.
2. While it runs, play audio and check the first second again.
3. Stop with **`Ctrl+C`** — you should see `Keep-alive stopped.`

- [ ] Record the result in Section 8 (*HoldOnly*).

---

## 5. Test Silent PCM mode

```powershell
.\HDMIKeepAlive.exe --run --mode SilentPcm --device-id "<PASTE DEVICE ID HERE>"
```

**Expected:** `Starting SilentPcm keep-alive...` / `Keep-alive running.` — and **no audible noise** from the endpoint. Other apps' audio should sound normal.

**Do this:**
1. Run 10+ minutes; test audio first-second behavior.
2. Listen for ANY hiss, click, or tone (there should be none).
3. Stop with `Ctrl+C`.

- [ ] Record the result in Section 8 (*SilentPcm* + *Audible noise*).

---

## 6. Optional — HDMI auto-target

Use if your device ID changes after dock/KVM reconnects (so you don't have to re-copy the ID):

```powershell
.\HDMIKeepAlive.exe --run --mode SilentPcm --target hdmi
```

**Expected:** it auto-selects an HDMI-like endpoint and behaves like Section 5.

- [ ] Record in Section 8 (*HDMI auto-target*).

---

## 7. Reconnect / resilience test

With a keep-alive running (Section 5 or 6), trigger a real-world disruption **if safe to do so**:

- Unplug/replug the dock, **or**
- Switch KVM inputs, **or**
- Power-cycle the display / soundbar / AVR.

**Expected:** the app does **not** crash, prints reconnect/running messages, and audio recovers.

- [ ] Record in Section 8 (*Reconnect*).

---

## 8. Results template — copy everything below into your reply for Codex

Fill in each field. Paste raw console text where asked. Leave a field as `N/A` if you skipped it.

```markdown
### HDMIKeepAlive Windows UAT Results

**Tester:**
**Date:**
**Build:** HDMIKeepAlive-win-x64-self-contained
**Workflow run URL (where artifact came from):**
**Windows version:** (e.g. Windows 11 23H2)

**HDMI/audio chain:** (PC → dock → KVM → TV → AVR → soundbar; list yours)

**Target device name:**
**Target device ID:**

**Device list (full --list-devices output):**
```
<paste here>
```

**Baseline (app off):**
- First second clipped/delayed?  Yes / No
- Notes:

**HoldOnly result:**
- First second clipped/delayed?  Yes / No / Improved
- Notes:

**SilentPcm result:**
- First second clipped/delayed?  Yes / No / Improved
- Notes:

**Audible noise during SilentPcm?**  Yes / No  (describe if yes)

**HDMI auto-target (--target hdmi):**  Pass / Fail / N/A
- Notes:

**Reconnect test (dock/KVM/display):**  Survived / Crashed / N/A
- What I did:
- What happened:

**Any console error text:**
```
<paste any errors here>
```

**Overall verdict:** Helps / No change / Made it worse / Inconclusive
**Other observations:**
```

---

## Troubleshooting

| Symptom | Likely cause / action |
|---------|----------------------|
| `Unknown argument: ...` | Typo in a flag. Re-check spelling; quote the device ID. |
| `Auto mode is not implemented...` | You passed `--mode Auto`. Use `HoldOnly` or `SilentPcm`. |
| `No active playback devices found.` | No active endpoints — check the device is connected & enabled in Sound settings. |
| Windows SmartScreen blocks the exe | Unsigned validation build. If policy allows: "More info" → "Run anyway". If blocked by your org, report that as the result. |
| Window closes instantly | Run from inside PowerShell (don't double-click the exe), so output stays visible. |
| Need to stop it | `Ctrl+C` in the terminal. |

Capture any error text verbatim into the results template — that's the most useful thing for Codex review.
```
