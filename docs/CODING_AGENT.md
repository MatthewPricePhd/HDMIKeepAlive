# Coding Agent Instructions

This document is the operating manual for Codex, Claude Code, or any coding agent working on HDMIKeepAlive.

## Mission

Build HDMIKeepAlive as a production-quality open-source Windows utility.

Do not treat this as a throwaway prototype.

## Core Rules

1. Keep the project buildable after every change.
2. Do not bypass the documented architecture.
3. Do not put WASAPI implementation details in the UI project.
4. Use interfaces for core services.
5. Use dependency injection.
6. Use async APIs.
7. Include tests with meaningful logic changes.
8. Do not invent requirements that conflict with the docs.
9. Do not use Exclusive Mode audio.
10. Do not require administrator privileges.
11. Do not modify registry settings.
12. Do not create a Windows Service.
13. Do not install drivers.
14. Do not access microphones.
15. Do not use network access.

## First Implementation Priority

Milestone 0 first.

Do not skip scaffolding.

Then Milestone 1.

Then Milestone 2.

Only implement Silent PCM after Hold Only mode exists.

## Expected Output Style

When making changes:

- Summarize files changed.
- Explain architectural decisions.
- Identify any risks.
- List next steps.
- If the update is ready for Windows testing, provide:
  - A copy/paste prompt for Claude Code to commit, push, and let GitHub Actions package the ZIP artifacts.
  - Updated Windows UAT steps tailored to the behavior changed in that update.
- Keep changes focused.

## If Ambiguous

Prefer the safest, simplest, most testable implementation.

Ask before making large architectural deviations.

## Definition of Done for a Coding Session

- Code builds.
- Tests pass.
- Documentation updated if behavior changed.
- No TODOs left without issue references.
