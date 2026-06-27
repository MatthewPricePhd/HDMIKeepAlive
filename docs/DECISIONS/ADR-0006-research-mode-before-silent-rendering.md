# ADR-0006-research-mode-before-silent-rendering: Implement Research Mode First

## Status

Accepted

## Context

HDMIKeepAlive should be safe, transparent, and maintainable.

## Decision

Implement Research Mode First.

## Consequences

Holding an endpoint open may be sufficient. The app should test this before continuously rendering silence.
