# Polish help output with colors, spacing, and command-only names

## Description

Improve help output to match top-tier CLI frameworks (kubectl, gh). Current output shows full patterns like `analyze [--diagnostic,-d {diagnostic}]` which causes truncation. Top CLIs show just command names in the main help table.

## Changes Required

### 1. Command Names Only (Not Full Patterns)
In `help-emitter.cs`, change `EmitCommands()` to show just the command name (first segment), not the full pattern with parameters/options.

### 2. Use Fluent API
Replace separate `terminal.WriteLine()` calls with fluent chaining:
```csharp
terminal
  .WriteLine("dev v2.1.0".BrightCyan().Bold())
  .WriteLine("Development CLI".Gray())
  .WriteLine()
  .WriteLine("USAGE: dev [command] [options]".Yellow())
  .WriteLine()
  .WriteLine("COMMANDS".Cyan().Bold())
  .WriteTable(...)
  .WriteLine()  // Section spacing
  .WriteLine("OPTIONS".Cyan().Bold())
  .WriteTable(...);
```

### 3. Add Section Spacing
Add blank line between Commands table and Options table.

### 4. Colored Headers
- App name/version in cyan bold
- USAGE line in yellow
- "COMMANDS" and "OPTIONS" headers in cyan bold
- App description in gray

### 5. Version in Header
Show version number next to app name.

### 6. USAGE Line
Add `USAGE: app [command] [options]` line in yellow after description.

## Expected Output

```
dev v2.1.0
Development CLI for TimeWarp.Nuru

USAGE: dev [command] [options]

COMMANDS
┌─────────────────┬─────────────────────────────────────┐
│ Command         │ Description                         │
├─────────────────┼─────────────────────────────────────┤
│ analyze         │ Run Roslynator analysis and fixes   │
│ build           │ Build all TimeWarp.Nuru projects    │
│ check-version   │ Check if packages already published │
│ ci              │ Run full CI/CD pipeline             │
│ clean           │ Clean solution and build artifacts  │
│ format          │ Check or fix code formatting        │
│ self-install    │ AOT compile and install dev CLI    │
│ test            │ Run the CI test suite               │
│ verify-samples  │ Verify all samples compile          │
└─────────────────┴─────────────────────────────────────┘

OPTIONS
┌────────────────┬────────────────────────────────┐
│ Option         │ Description                    │
├────────────────┼────────────────────────────────┤
│ --help, -h     │ Show this help message         │
│ --version      │ Show version information       │
│ --capabilities │ Show capabilities for AI tools │
└────────────────┴────────────────────────────────┘
```

## Files to Modify

| File | Changes |
|------|---------|
| `help-emitter.cs` | Update `EmitHeader()`, `EmitUsage()`, `EmitCommands()`, `EmitOptions()` to use fluent API and colors |

## Checklist

- [ ] Update `EmitHeader()` to show version and use colors
- [ ] Add `EmitUsage()` method with yellow "USAGE:" line
- [ ] Update `EmitCommands()` to show command names only (not full patterns)
- [ ] Update `EmitCommands()` to use fluent API chaining
- [ ] Add section spacing (blank line) between Commands and Options
- [ ] Update `EmitOptions()` to use fluent API chaining
- [ ] Add colored headers ("COMMANDS", "OPTIONS" in cyan bold)
- [ ] Run existing tests to verify no regressions
- [ ] Run new table formatting tests
- [ ] Verify help output visually

## Notes

Based on TimeWarp.Terminal API:
- `.BrightCyan().Bold()` for app name
- `.Gray()` for descriptions
- `.Yellow()` for USAGE line
- `.Cyan().Bold()` for section headers
- `.WriteTable(...)` with `BorderStyle.Rounded`

## Related Tasks

- #400 - Use terminal.WriteTable for generated --help output (completed - established table format)
