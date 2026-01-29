# Refactor: Extract built-in flags list to constants

## Description

Currently, the built-in flags (`--help`, `-h`, `--version`, `--capabilities`) are hardcoded in multiple places:

1. `route-matcher-emitter.cs` (Bug #403 fix) - lines 147
2. `interceptor-emitter.cs` - built-in flags emission
3. Any other locations that need to reference these flags

This makes maintenance difficult when adding new built-in flags. We should extract these to a shared constant or configuration.

## Current State

### In `route-matcher-emitter.cs`:
```csharp
if (minPositionalArgs == 0)
{
  sb.AppendLine("      // Skip built-in flags for default/option-only routes");
  sb.AppendLine("      if (routeArgs is [\"--help\" or \"-h\"] or [\"--version\"] or [\"--capabilities\"])");
  sb.AppendLine($"        goto route_skip_{routeIndex};");
}
```

### In `interceptor-emitter.cs` (EmitBuiltInFlags):
```csharp
// Check for --help or -h
if (routeArgs is ["--help" or "-h"])
{
  PrintHelp(app.Terminal);
  return 0;
}

// Check for --version
if (routeArgs is ["--version"])
{
  PrintVersion(app.Terminal);
  return 0;
}

// Check for --capabilities
if (routeArgs is ["--capabilities"])
{
  PrintCapabilities(app.Terminal);
  return 0;
}
```

## Proposed Solution

Create a shared constants class in the generators project:

```csharp
// source/timewarp-nuru-analyzers/generators/models/built-in-flags.cs
namespace TimeWarp.Nuru.Generators;

internal static class BuiltInFlags
{
  public static readonly string[] HelpForms = ["--help", "-h"];
  public static readonly string[] VersionForms = ["--version"];
  public static readonly string[] CapabilitiesForms = ["--capabilities"];
  
  public static readonly string[] All = ["--help", "-h", "--version", "--capabilities"];
  
  // For pattern matching generation
  public static string GetPatternMatchExpression()
  {
    return "[\"--help\" or \"-h\"] or [\"--version\"] or [\"--capabilities\"]";
  }
}
```

## Benefits

1. **Single source of truth** - All built-in flags defined in one place
2. **Easier maintenance** - Add new flags by updating one file
3. **Consistency** - Ensures all emitters use the same flag names
4. **Future-proof** - Makes it easier to add new built-in flags like `--check-updates`

## Files to Modify

| File | Change |
|------|--------|
| `source/timewarp-nuru-analyzers/generators/models/built-in-flags.cs` | Create new constants file |
| `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs` | Use constants instead of hardcoded strings |
| `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` | Use constants instead of hardcoded strings |

## Checklist

- [x] Create `built-in-flags.cs` with constant definitions
- [x] Refactor `route-matcher-emitter.cs` to use constants
- [x] Refactor `interceptor-emitter.cs` to use constants
- [x] Search for any other hardcoded flag references
- [x] Run tests to ensure no regressions
- [x] Consider if any other flags should be included (e.g., `--interactive`)

## Results

### Summary
Successfully refactored built-in flags to use shared constants, improving maintainability.

### Files Changed
- **Created**: `source/timewarp-nuru-analyzers/generators/models/built-in-flags.cs` (new constants file)
- **Modified**: `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- **Modified**: `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs`
- **Modified**: `source/timewarp-nuru-analyzers/generators/emitters/route-help-emitter.cs`

### Implementation Details
1. Created `BuiltInFlags` constants class with:
   - `HelpForms`, `VersionForms`, `CapabilitiesForms` arrays
   - `All` array combining all flags
   - `PatternMatchExpression` for generated pattern matching code
   - `IsBuiltInFlagRoutePattern` for route pattern checking

2. Refactored 4 emitter files to use constants instead of hardcoded strings

3. Search found additional usage in `route-help-emitter.cs` that was also updated

### Key Decisions
- Kept some patterns inline where C# pattern matching requires literal strings
- Did not refactor documentation/help text emitters as they serve different purposes

### Test Results
- All 1052 tests passed
- Bug #403 specific tests passed (6/6)
- Build succeeded with 0 warnings, 0 errors
- Manual verification of built-in flags (`--help`, `--version`, `--capabilities`) confirmed working

## Notes

This is a refactoring task identified during PR #161 review. It improves code maintainability but is not urgent.

### Related
- PR #161 (Bug #403 fix)
- Future built-in flags like `--check-updates`
