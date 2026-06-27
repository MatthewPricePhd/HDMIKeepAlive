# ADR-0001-use-wasapi-shared-mode: Use WASAPI Shared Mode

## Status

Accepted

## Context

HDMIKeepAlive should be safe, transparent, and maintainable.

## Decision

Use WASAPI Shared Mode.

## Consequences

Shared Mode allows normal audio mixing with Teams, Zoom, browsers, and media apps. Exclusive Mode is prohibited because it could block other applications.
