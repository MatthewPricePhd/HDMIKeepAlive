# ADR-0002-no-windows-service: Do Not Use Windows Service

## Status

Accepted

## Context

HDMIKeepAlive should be safe, transparent, and maintainable.

## Decision

Do Not Use Windows Service.

## Consequences

A service would require admin rights and would be less appropriate for a user-session audio endpoint. Use a tray app instead.
