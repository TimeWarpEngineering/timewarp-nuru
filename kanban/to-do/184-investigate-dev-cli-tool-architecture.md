# Investigate dev CLI tool architecture

## Description

Design and plan the unified `dev` CLI tool that consolidates all developer tooling into a single AOT-compiled binary. This task captures the architectural concepts to be refined after completing prerequisites (Task 150: Attributed Routes).

## Prerequisites

- Task 150: Implement Attributed Routes Phase 1

## Proposed Directory Structure

```
runfiles/
├── dev/                          # The dev CLI project
│   ├── dev.csproj               # AOT-enabled project, includes command files
│   ├── Program.cs               # Minimal entry point
│   ├── bootstrap.cs             # Runfile to build dev binary for current platform
│   └── Docs/
│       ├── build.md             # Extended help for build command
│       ├── test.md              # Extended help for test command
│       └── capabilities.md      # What this dev CLI can do
├── build.cs                      # Build command (works standalone AND in dev CLI)
├── clean.cs                      # Clean command
├── format.cs                     # Format command
├── analyze.cs                    # Analyze command
├── test.cs                       # Test command (currently just calls CI runner)
├── test-all.cs                   # Full test suite command
├── test-aot.cs                   # AOT/JIT integration tests (convert from .sh)
└── ...                           # Other command runfiles
```

## Key Concepts

### Self-Registering Commands
- Each runfile uses `[ModuleInitializer]` to self-register
- Attributed routes: `[Route("build")]`, `[Route("test ci")]`
- `dev.csproj` includes files à la carte via `<Compile Include="../build.cs" />`

### Dual-Mode Execution
- Standalone: `dotnet runfiles/build.cs` (development, debugging)
- Aggregated: `./dev build` (fast AOT binary)
- Use `#if !DEV_CLI` guard for standalone entry point

### AOT Binary
- Platform-specific build via `bootstrap.cs`
- Binary at repo root: `./dev` (gitignored, built locally)
- Each developer builds for their platform

### AI Discoverability
- `dev --capabilities` outputs JSON/YAML of all commands
- Enables AI agents to understand available tooling
- Consistent pattern across all TimeWarp repos

### Extended Help
- `dev extended-help {command}` displays colocated markdown
- Docs embedded as resources in AOT build

## Proposed Routes

```
# Build commands
dev build [--release] [--verbose]
dev clean
dev format [--check]
dev analyze

# Test commands
dev test                          # Fast CI tests (alias for test ci)
dev test ci                       # Fast CI tests (~1700, ~12s)
dev test all                      # Full suite (~1759, ~25s)
dev test aot                      # AOT/JIT integration comparison
dev test unit [--tag {tag}]       # Unit tests with optional filter

# Help/Discovery
dev --help
dev --capabilities
dev extended-help {command}
```

## Open Questions

- [ ] Should `dev test` (no subcommand) run `ci` or `all`?
- [ ] How to handle `tests/test-both-versions.sh` conversion to C#?
- [ ] What's the right strategy for cross-platform AOT binaries?
- [ ] Should command grouping use attributes or directory structure?

## Notes

This investigation task captures the vision discussed. Implementation depends on Task 150 (Attributed Routes) completing first. The goal is to establish a consistent `dev` CLI pattern that can be replicated across all TimeWarp repositories.
