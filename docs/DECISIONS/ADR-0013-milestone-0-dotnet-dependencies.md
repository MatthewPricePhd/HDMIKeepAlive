# ADR-0013-milestone-0-dotnet-dependencies: Use Standard .NET Host and Test Packages

## Status

Accepted

## Context

Milestone 0 requires dependency injection, structured logging abstractions, and unit tests while keeping the project buildable and avoiding undocumented runtime behavior.

## Decision

Use `Microsoft.Extensions.Hosting` in the UI composition root to provide dependency injection and Microsoft logging abstractions.

Use `Microsoft.NET.Test.Sdk`, `xunit`, and `xunit.runner.visualstudio` for the unit test project.

Do not add an audio/WASAPI library in Milestone 0.

## Consequences

The initial host uses standard .NET 8 infrastructure without introducing audio implementation dependencies early. Tests can run in GitHub Actions with the standard .NET test runner. A future ADR is required before adding any audio interop package such as NAudio or Vanara.
