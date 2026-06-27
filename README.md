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

## License

MIT License. See `LICENSE`.
