# Claude Code Instructions

This file configures Claude Code for the timewarp-nuru repository.

## Shared Instructions

<!-- Include shared instructions that apply across repos -->

- [Agent Context Regions](shared/agent-context-regions.md) - Using #region blocks for agent context
- [.NET Runfiles](shared/dotnet-runfiles.md) - File-based app conventions
- [Git Guidelines](shared/git-guidelines.md) - Git workflow and conventions

## Local Instructions

<!-- Repo-specific instructions -->

See [local/](local/) for repo-specific instructions.

## Quick Reference

### Build Commands
- Full build: `dotnet build timewarp-nuru.slnx -c Release`
- Runfile build: `dotnet runfiles/build.cs`
- Clean & rebuild: `dotnet runfiles/clean-and-build.cs`

### Test Commands
- CI tests: `dotnet run tests/ci-tests/run-ci-tests.cs`
- Single test: `dotnet run tests/timewarp-nuru-core-tests/routing/routing-01-basic.cs`

### Generated Files
Source generated files emit to `artifacts/generated/{ProjectName}/`
