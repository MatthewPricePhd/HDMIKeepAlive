# Milestone 2 Prompt: Hold Only Research Mode

Implement Hold Only mode.

Goal:

Open the selected WASAPI Shared Mode endpoint and hold it open without rendering PCM.

Requirements:

1. Implement `IAudioKeepAliveEngine`.
2. Add `EndpointHoldKeepAliveEngine`.
3. Support StartAsync and StopAsync.
4. Expose engine state.
5. Report diagnostics.
6. Handle cancellation.
7. Dispose audio resources correctly.
8. Add tests for state transitions.

Do not implement Silent PCM yet.

The purpose is to test whether holding the endpoint open prevents HDMI audio idle timeout.
