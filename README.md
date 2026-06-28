# HDMIKeepAlive

**HDMIKeepAlive** is a lightweight Windows utility that prevents HDMI, DisplayPort, and HDMI-to-S/PDIF audio paths from going idle by keeping a selected playback endpoint active through **WASAPI Shared Mode**.

It is designed for users who experience a delay or clipped first second of audio when playback resumes after silence, especially with:

- HDMI audio extractors
- AV receivers
- Soundbars
- TVs
- HDMI KVM switches
- USB-C / Thunderbolt docks
- Intel, NVIDIA, or AMD HDMI audio endpoints
- Home theater PCs
- Workstations with complex display/audio chains

## Product Principle

HDMIKeepAlive should behave like a normal, quiet Windows multimedia app.

It must not install drivers, services, kernel components, hooks, virtual devices, network services, or anything that would look suspicious on a managed workstation.

## Core v1.0 Behavior

1. Enumerate playback endpoints.
2. Allow user to choose a target endpoint.
3. Open the endpoint using WASAPI Shared Mode.
4. Research whether holding the endpoint open prevents HDMI audio idle timeout.
5. If needed, render silent PCM buffers to keep the endpoint clock active.
6. Auto-reconnect after HDMI, dock, KVM, or default-device changes.
7. Run from the system tray.
8. Optionally start with Windows.

## Documentation

See `/docs` for the complete product specification, architecture, test plan, coding standards, and coding-agent kickoff prompts.

## Early Windows Hardware Testing

GitHub Actions produces portable Windows ZIP artifacts for hardware validation:

- `HDMIKeepAlive-win-x64-portable.zip`: framework-dependent, requires .NET 8 on the target machine.
- `HDMIKeepAlive-win-x64-self-contained.zip`: larger, includes the runtime and is preferred for corporate/work machines.

Download an artifact from a successful workflow run, extract it, and run `HDMIKeepAlive.exe` from PowerShell or Command Prompt. No installer, administrator rights, service, driver, or registry modification is required.

Useful validation commands:

```powershell
.\HDMIKeepAlive.exe --list-devices
.\HDMIKeepAlive.exe --run --mode HoldOnly
.\HDMIKeepAlive.exe --run --mode SilentPcm
.\HDMIKeepAlive.exe --run --mode SilentPcm --device-id "<endpoint-id>"
.\HDMIKeepAlive.exe --run --mode SilentPcm --target hdmi
```

## License

MIT License. See `LICENSE`.
