# ADR-0003-json-settings: Use JSON Settings

## Status

Accepted

## Context

HDMIKeepAlive should be safe, transparent, and maintainable.

## Decision

Use JSON Settings.

## Consequences

JSON under LocalAppData avoids registry writes and is easy to inspect, backup, and migrate.
