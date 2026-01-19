# Make NURU_DEBUG analyzer diagnostics controllable via EditorConfig

## Description

The NURU_DEBUG* analyzer diagnostics (NURU_DEBUG, NURU_DEBUG2, NURU_DEBUG3, NURU_DEBUG4, NURU_DEBUG_CONV1) currently emit warnings during builds, which is not appropriate for a shipped analyzer library. Consumers should be able to toggle these diagnostics on/off in their own projects.

**Approach:** Use EditorConfig severity configuration - the standard .NET mechanism for analyzer control.

## Context

TimeWarp.Nuru is published as a NuGet analyzer library. When developers consume it in their own apps (like the sample apps), they currently see all NURU_DEBUG* warnings during builds. These are intended for debugging/informational purposes during development of the Nuru library itself, not for end consumers.

The goal is:
- **Default behavior:** No warnings (diagnostics suppressed)
- **Opt-in:** Consumers can enable diagnostics via their project's `.editorconfig`

## Plan

### Step 1: Change NURU_DEBUG* diagnostics to Hidden severity

Update diagnostic definitions in these files:

1. **`source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`**
   - Line ~245: NURU_DEBUG2 - change `DiagnosticSeverity.Warning` to `DiagnosticSeverity.Hidden`
   - Line ~264: NURU_DEBUG4 - change `DiagnosticSeverity.Warning` to `DiagnosticSeverity.Hidden`
   - Line ~284: NURU_DEBUG3 - change `DiagnosticSeverity.Warning` to `DiagnosticSeverity.Hidden`
   - Lines ~309, ~336: NURU_DEBUG - change `DiagnosticSeverity.Warning` to `DiagnosticSeverity.Hidden`

2. **`source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`**
   - Line ~1284: NURU_DEBUG_CONV1 - change `DiagnosticSeverity.Warning` to `DiagnosticSeverity.Hidden`

### Step 2: Add commented EditorConfig entries for developer visibility

Add to `.editorconfig` (under "Analyzer settings" section, around line 332):

```editorconfig
# NURU_DEBUG diagnostics - Hidden by default for clean release builds
# Uncomment to see informational debug output during development (info severity):
# dotnet_diagnostic.NURU_DEBUG.severity = info
# dotnet_diagnostic.NURU_DEBUG2.severity = info
# dotnet_diagnostic.NURU_DEBUG3.severity = info
# dotnet_diagnostic.NURU_DEBUG4.severity = info
# dotnet_diagnostic.NURU_DEBUG_CONV1.severity = info
```

### Step 3: Verify sample builds still pass

Run `dotnet run --file tools/dev-cli/dev.cs -- verify-samples` to confirm:
- All 39 samples build successfully
- No NURU_DEBUG* warnings appear (they're now Hidden)

## Consumer Experience

When a developer uses TimeWarp.Nuru in their own project:

**Default (no configuration):**
- Clean builds with no NURU_DEBUG* warnings
- Diagnostics are Hidden by default

**Opt-in (add to their .editorconfig to see info severity):**
```editorconfig
dotnet_diagnostic.NURU_DEBUG.severity = info
dotnet_diagnostic.NURU_DEBUG2.severity = info
dotnet_diagnostic.NURU_DEBUG3.severity = info
dotnet_diagnostic.NURU_DEBUG4.severity = info
dotnet_diagnostic.NURU_DEBUG_CONV1.severity = info
```

- Developers see informational diagnostics about route extraction, interpreter results, etc.

## Files Changed

- `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
- `.editorconfig`

## Checklist

- [x] Update NURU_DEBUG* diagnostics to Hidden severity in app-extractor.cs
- [x] Update NURU_DEBUG_CONV1 to Hidden severity in dsl-interpreter.cs
- [x] Add commented EditorConfig entries for NURU_DEBUG* diagnostics
- [x] Verify all samples build successfully with no warnings
- [ ] Commit changes

## Notes

This approach aligns with the existing EditorConfig configuration pattern already used in this project (see lines 332-430 in .editorconfig for other analyzer settings).

## Results

### Implementation Complete

All NURU_DEBUG* diagnostics changed from `Warning` to `Hidden` severity:

**Files Changed:**
1. `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`
   - NURU_DEBUG2 (line 245): Warning → Hidden
   - NURU_DEBUG4 (line 264): Warning → Hidden
   - NURU_DEBUG3 (line 284): Warning → Hidden
   - NURU_DEBUG (lines 309, 336): Warning → Hidden

2. `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
   - NURU_DEBUG_CONV1 (line 1284): Warning → Hidden

3. `.editorconfig`
   - Added commented entries for opt-in debug diagnostics

### Verification
- All 39 samples build successfully
- No NURU_DEBUG* warnings appear in build output
- Consumers can enable diagnostics via EditorConfig if needed
