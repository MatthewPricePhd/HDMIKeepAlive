# Security and Enterprise Compatibility

## Principle

HDMIKeepAlive must behave like a normal user-space Windows multimedia application.

It must not attempt to bypass enterprise controls.

---

## Prohibited Behaviors

The application must not:

- Install a driver.
- Install a Windows Service.
- Require admin rights.
- Modify HKLM registry keys.
- Inject into other processes.
- Hook APIs.
- Intercept audio from other applications.
- Record audio.
- Access microphone devices.
- Use network access.
- Phone home.
- Auto-update silently.
- Obfuscate code.
- Hide from Task Manager.
- Use suspicious persistence methods.

---

## Allowed Behaviors

The application may:

- Run in user session.
- Open playback endpoint in WASAPI Shared Mode.
- Render digital silence.
- Store JSON settings under LocalAppData.
- Create a startup shortcut for the current user.
- Create a non-elevated per-user scheduled task if implemented.
- Write optional local logs.

---

## Corporate Laptop Considerations

For managed devices:

- Portable EXE is preferred.
- No installer is required.
- Startup should be user-controllable.
- Logs should be local and optional.
- App name and description should be clear and non-suspicious.
- Binary should not be packed or obfuscated.
