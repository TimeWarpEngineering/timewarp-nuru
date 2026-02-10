# Polish help output with colors, spacing, and command-only names

## Description

Improve help output to match top-tier CLI frameworks (kubectl, gh). Current output shows full patterns like `analyze [--diagnostic,-d {diagnostic}]` which causes truncation. Top CLIs show just command names with category headers.

## Reference: How Top CLIs Handle Groups

**GitHub CLI (gh):**
```
CORE COMMANDS
  auth:          Authenticate gh and git with GitHub
  browse:        Open repositories, issues, pull requests
  pr:            Manage pull requests
  repo:          Manage repositories

GITHUB ACTIONS COMMANDS
  cache:         Manage GitHub Actions caches
  run:           View details about workflow runs
```

**kubectl:**
```
Basic Commands (Beginner):
  create          Create a resource from a file or stdin
  expose          Take a replication controller and expose it

Deploy Commands:
  rollout         Manage the rollout of a resource
  scale           Set a new size for a deployment
```

**Key patterns:**
1. Command names ONLY (no parameters/options)
2. Category headers for groups (not indentation)
3. Commands listed under their category header

## Changes Required

### 1. Command Names Only
In `help-emitter.cs`, show just the command name (first segment), not full pattern with parameters/options.

### 2. Use Fluent API
```csharp
terminal
  .WriteLine("dev v2.1.0".BrightCyan().Bold())
  .WriteLine("Development CLI".Gray())
  .WriteLine()
  .WriteLine("USAGE: dev [command] [options]".Yellow())
  .WriteLine()
  .WriteLine("COMMANDS".Cyan().Bold())
  .WriteTable(...)
  .WriteLine()
  .WriteLine("OPTIONS".Cyan().Bold())
  .WriteTable(...);
```

### 3. Category Headers for Groups
Commands with the same `[NuruRouteGroup]` should be grouped under a category header:

```
┌─────────────────┬─────────────────────────────────────┐
│ dev             │ Commands for dev CLI               │
├─────────────────┼─────────────────────────────────────┤
│ analyze         │ Run Roslynator analysis and fixes   │
│ build           │ Build all TimeWarp.Nuru projects   │
│ clean           │ Clean solution and build artifacts │
│ format          │ Check or fix code formatting       │
│ test            │ Run the CI test suite              │
│ verify-samples  │ Verify all samples compile         │
└─────────────────┴─────────────────────────────────────┘

┌─────────────────┬─────────────────────────────────────┐
│ docker          │ Docker commands                    │
├─────────────────┼─────────────────────────────────────┤
│ docker build    │ Build a Docker image              │
│ docker run      │ Run a Docker container            │
│ docker ps       │ List Docker containers            │
│ docker tag      │ Tag an image                       │
└─────────────────┴─────────────────────────────────────┘
```

### 4. Add Section Spacing
Blank line between each category table.

### 5. Colored Headers
- App name/version: cyan bold
- USAGE line: yellow
- Category headers ("COMMANDS", "docker"): cyan bold
- App description: gray

### 6. Version in Header
Show version number next to app name.

## Expected Output

```
dev v2.1.0
Development CLI for TimeWarp.Nuru

USAGE: dev [command] [options]

COMMANDS
┌─────────────────┬─────────────────────────────────────┐
│ dev             │ Commands for dev CLI               │
├─────────────────┼─────────────────────────────────────┤
│ analyze         │ Run Roslynator analysis and fixes  │
│ build           │ Build all TimeWarp.Nuru projects   │
│ clean           │ Clean solution and build artifacts│
│ format          │ Check or fix code formatting       │
│ self-install    │ AOT compile and install dev CLI   │
│ test            │ Run the CI test suite             │
│ verify-samples  │ Verify all samples compile        │
└─────────────────┴─────────────────────────────────────┘

CONFIG
┌─────────────────┬─────────────────────────────────────┐
│ config set      │ Set a configuration value          │
│ config get      │ Get a configuration value          │
│ config list     │ List all configurations           │
└─────────────────┴─────────────────────────────────────┘

DOCKER
┌─────────────────┬─────────────────────────────────────┐
│ docker build    │ Build a Docker image              │
│ docker run      │ Run a Docker container            │
│ docker ps       │ List Docker containers            │
└─────────────────┴─────────────────────────────────────┘

OPTIONS
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │
│ --version      │ Show version information       │
│ --capabilities │ Show capabilities for AI tools │
└────────────────┴────────────────────────────────┘
```

## Files to Modify

| File | Changes |
|------|---------|
| `help-emitter.cs` | Update `EmitHeader()`, `EmitUsage()`, `EmitCommands()` to use fluent API, colors, and category headers |

## Checklist

- [ ] Update `EmitHeader()` to show version and use colors
- [ ] Add `EmitUsage()` method with yellow "USAGE:" line
- [ ] Group commands by `[NuruRouteGroup]` attribute
- [ ] Show category header for each group (e.g., "CONFIG", "DOCKER")
- [ ] Show just command name (not full pattern) in table
- [ ] Use fluent API chaining throughout
- [ ] Add section spacing between category tables
- [ ] Add colored category headers ("COMMANDS", "CONFIG", etc. in cyan bold)
- [ ] Update `EmitOptions()` with fluent API
- [ ] Run existing tests to verify no regressions
- [ ] Run new table formatting tests
- [ ] Verify help output visually

## Notes

Based on TimeWarp.Terminal API:
- `.BrightCyan().Bold()` for app name
- `.Gray()` for descriptions
- `.Yellow()` for USAGE line
- `.Cyan().Bold()` for category headers
- `.WriteTable(...)` with `BorderStyle.Rounded`

## Related Tasks

- #400 - Use terminal.WriteTable for generated --help output (completed - established table format)
