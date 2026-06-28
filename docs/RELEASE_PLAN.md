# Release Plan

## Versioning

Use Semantic Versioning.

```text
MAJOR.MINOR.PATCH
```

## Release Artifacts

- `HDMIKeepAlive-win-x64-portable.zip`
  - Framework-dependent.
  - Requires .NET 8 on the target machine.
- `HDMIKeepAlive-win-x64-self-contained.zip`
  - Includes the .NET runtime.
  - Preferred for corporate/work machines where installing .NET may not be practical.
- Checksums
- Release notes

Early hardware validation builds should be uploaded as GitHub Actions workflow artifacts. Users should download the ZIP, extract it, and run `HDMIKeepAlive.exe`. Installer and MSIX packaging are deferred until the app is stable.

## Test-Ready Handoff

When a local update is ready for Windows hardware validation, the coding agent must provide a copy/paste Claude Code prompt that asks Claude to:

- Review the local diff.
- Run build and test commands.
- Commit the focused change.
- Push to GitHub.
- Confirm the GitHub Actions workflow completed.
- Report the downloadable artifact names and workflow URL.

The coding agent must also provide an updated UAT process for the specific behavior under test.

## Pre-Release Checklist

- Build passes.
- Tests pass.
- Manual delay-prevention test passes.
- No admin required.
- No network access.
- README updated.
- CHANGELOG updated.
- Known issues documented.

## Release Notes Format

```markdown
# HDMIKeepAlive vX.Y.Z

## Added
## Changed
## Fixed
## Known Issues
```
