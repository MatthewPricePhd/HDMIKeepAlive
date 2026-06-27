# Codex / Claude Code Kickoff Prompt

Use this prompt to start the project from scratch.

---

You are the lead software engineer for a production-quality Windows open-source application named HDMIKeepAlive.

The repository contains documentation in the `/docs` folder. Read all documentation before writing code, especially:

- `docs/PRD.md`
- `docs/DEVELOPMENT_PLAN.md`
- `docs/ARCHITECTURE.md`
- `docs/AUDIO_ENGINE.md`
- `docs/CODING_AGENT.md`
- `docs/CODING_STANDARDS.md`
- `docs/API_CONTRACTS.md`
- `docs/SECURITY_ENTERPRISE.md`
- `docs/KNOWN_RESEARCH.md`

Your task is to implement the project incrementally.

Do not build the entire app in one pass.

Start with Milestone 0 from `docs/DEVELOPMENT_PLAN.md`.

Project requirements:

- C#
- .NET 8 LTS
- WinUI 3 UI eventually, but start with solution scaffolding
- MVVM
- Dependency Injection
- Async throughout
- Nullable enabled
- Structured logging
- Unit tests
- GitHub Actions CI
- No admin rights
- No Windows Service
- No drivers
- No registry modifications
- No API hooking
- No DLL injection
- No microphone access
- No network access
- WASAPI Shared Mode only
- Exclusive Mode is prohibited

Milestone 0 deliverables:

1. Create the solution.
2. Create these projects:
   - HDMIKeepAlive.UI
   - HDMIKeepAlive.Core
   - HDMIKeepAlive.Audio
   - HDMIKeepAlive.Diagnostics
   - HDMIKeepAlive.Infrastructure
   - HDMIKeepAlive.Tests
3. Configure project references according to `docs/ARCHITECTURE.md`.
4. Enable nullable reference types.
5. Add basic dependency-injection setup.
6. Add basic logging abstractions.
7. Add initial core interfaces from `docs/API_CONTRACTS.md`.
8. Add unit test project with at least one placeholder test per core model/service.
9. Add GitHub Actions workflow to build and test.
10. Ensure `dotnet build` and `dotnet test` pass.

Important:

- Do not implement Silent PCM rendering yet.
- Do not implement a tray icon yet.
- Do not add undocumented dependencies.
- If you think a dependency is needed, document the rationale in a new ADR under `docs/DECISIONS/`.

After completing Milestone 0, summarize:

- Files created
- Architecture decisions made
- Commands to build/test
- Risks or blockers
- Recommended next milestone
