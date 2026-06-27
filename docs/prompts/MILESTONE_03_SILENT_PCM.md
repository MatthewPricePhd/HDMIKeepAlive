# Milestone 3 Prompt: Silent PCM Engine

Implement Silent PCM rendering after Hold Only mode exists.

Requirements:

1. Use WASAPI Shared Mode.
2. Never use Exclusive Mode.
3. Render zero-valued PCM buffers.
4. Use endpoint mix format where possible.
5. Avoid audible artifacts.
6. Keep CPU very low.
7. Support cancellation.
8. Report diagnostics.
9. Add error handling.
10. Add tests for lifecycle behavior.

Do not add UI complexity beyond what is needed to test the engine.
