# Milestone 1 Prompt: Audio Device Enumeration

Implement Milestone 1 from `docs/DEVELOPMENT_PLAN.md`.

Before coding, read:

- `docs/AUDIO_ENGINE.md`
- `docs/API_CONTRACTS.md`
- `docs/CODING_AGENT.md`

Goal:

Implement playback device enumeration behind an interface.

Requirements:

1. Add `IAudioDeviceEnumerator`.
2. Add `AudioDeviceInfo`.
3. Enumerate active playback endpoints.
4. Identify default playback device if possible.
5. Return friendly name and stable device ID.
6. Keep implementation outside the UI.
7. Add tests using mocks where possible.
8. Add a temporary diagnostics path if needed.

Do not implement keep-alive rendering yet.

End with build/test commands and next steps.
