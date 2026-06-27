# Coding Standards

## Language and Framework

- C#
- .NET 8 LTS
- Nullable reference types enabled
- WinUI 3 for UI
- MVVM pattern

## Required Practices

- Dependency Injection
- Async/await
- CancellationToken for long-running operations
- XML documentation on public APIs
- Structured logging
- Unit tests for core behavior
- Clear error handling
- No blocking async calls using `.Result` or `.Wait()`

## Prohibited Practices

- Global mutable singletons
- Registry hacks
- Admin-only code paths
- Service installation
- Driver installation
- Exclusive Mode audio
- Network telemetry
- Obfuscated code
- Hardcoded machine-specific device IDs

## Naming

- Interfaces start with `I`.
- Async methods end with `Async`.
- Records for immutable DTOs.
- Use explicit names over abbreviations.

## Comments

Document why, not just what.

## Dependencies

Minimize dependencies.

Any dependency must be justified in an ADR.
