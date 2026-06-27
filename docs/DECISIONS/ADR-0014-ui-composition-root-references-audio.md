# ADR-0014-ui-composition-root-references-audio: UI Project Owns Application Composition

## Status

Accepted

## Context

The main UI host must construct a working application graph for device enumeration, keep-alive engines, reconnect handling, settings, diagnostics, and MVVM view models.

Core must remain free of Windows audio implementation references, and UI code must not call WASAPI APIs directly.

## Decision

Allow `HDMIKeepAlive.UI` to reference `HDMIKeepAlive.Audio` only as the application composition root.

The UI binds to view models and Core interfaces. WASAPI details remain isolated in `HDMIKeepAlive.Audio`.

## Consequences

The application host can resolve a fully wired `MainViewModel` without moving audio implementation details into UI controls or view models. Future packaging work can split composition into a dedicated host project if the UI project becomes too broad.
