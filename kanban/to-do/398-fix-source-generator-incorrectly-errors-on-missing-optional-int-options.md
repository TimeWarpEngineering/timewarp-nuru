# Fix source generator incorrectly errors on missing optional int options

## Description

Fix bug in Nuru source generator where commands with optional `int` options cause routing failures instead of skipping to next route candidate when option not provided. This prevents ANY command from working if there's a prior route with optional int option.

GitHub Issue: #152

## Checklist

- [ ] Analyze current generated routing code for optional int options
- [ ] Identify the root cause in source generator logic
- [ ] Fix the condition to only validate when flag was actually found
- [ ] Create test case to reproduce the issue
- [ ] Verify fix doesn't break existing functionality
- [ ] Test with various scenarios: optional int, missing vs provided values
- [ ] Ensure route skipping works correctly for all affected patterns

## Notes

### Root Cause
The generated routing code incorrectly validates optional int options even when the flag wasn't found:

**Current (buggy) code:**
```csharp
if (__days_flagFound_23 && __days_raw is null) goto route_skip_23;
int days = default;
if (__days_raw is null || !int.TryParse(__days_raw, ...))
{
  app.Terminal.WriteLine($"Error: Invalid value '{__days_raw ?? "(missing)"}' for option '--days'. Expected: int");
  return 1;  // <-- BUG: Returns error instead of goto route_skip_23
}
```

### The Fix
The parse/error check should only apply when the flag was actually found:

**Fixed approach:**
```csharp
// Option 1: Add flag check to validation
if (__days_flagFound_23 && (__days_raw is null || !int.TryParse(__days_raw, ...)))
{
  app.Terminal.WriteLine($"Error: ...");
  return 1;
}

// Option 2: Better - skip route on validation failure
if (__days_flagFound_23 && __days_raw is null) goto route_skip_23;
if (__days_raw is not null && !int.TryParse(__days_raw, ...)) goto route_skip_23;
```

### Affected Repository
- Repository: `TimeWarpEngineering/timewarp-ganda`
- Generated File: `artifacts/generated/timewarp-ganda/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs`

### Example Scenario
- `WorkspaceCommitsCommand` has optional `--days` int option
- `RepoSetupCommand` has route `repo setup`
- Running `ganda repo setup --dry-run` fails with error about `--days` instead of executing repo setup

### Impact
This bug breaks routing for any command following a route with optional int options, making the CLI unusable in affected scenarios.
