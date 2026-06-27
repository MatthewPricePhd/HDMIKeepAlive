# Troubleshooting

## Audio Still Delays

Try:

1. Verify target endpoint is correct.
2. Switch from Hold Only to Silent PCM mode.
3. Restart audio engine.
4. Reconnect HDMI/dock.
5. Check diagnostics for reconnect count.
6. Confirm app is running in tray.

## Teams Audio Problems

HDMIKeepAlive must use Shared Mode. If Teams cannot play audio, this is a bug.

## Device Disappears

This may happen when:

- KVM switches input
- Dock reconnects
- Display sleeps
- HDMI extractor resets

The app should reconnect automatically.

## Corporate Laptop Blocks App

Use portable build.

Avoid installer.

Do not enable advanced startup until approved.

## High CPU

This is a bug.

Capture logs and diagnostics.
