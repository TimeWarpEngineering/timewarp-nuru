# Add `<UseNewGen>` Toggle Infrastructure

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Add MSBuild property `<UseNewGen>` that controls which source generator path runs. When false (default), existing V1 generators run. When true, new V2 generator runs instead.

## Checklist

- [x] Add `<CompilerVisibleProperty Include="UseNewGen" />` to Directory.Build.props
- [x] Modify V1 generators to check property and skip if `UseNewGen=true`
  - `nuru-attributed-route-generator.cs`
  - `nuru-delegate-command-generator.cs`
  - `nuru-invoker-generator.cs`
  - `nuru-route-analyzer.cs` (left unchanged - pattern validation stays active)
- [x] Create V2 generator stub that only runs when `UseNewGen=true`
- [x] V2 initially emits marker type to prove it ran
- [x] Verify: build with default (V1 runs), build with `UseNewGen=true` (V2 runs)

## Results

Implemented the `UseNewGen` toggle infrastructure:

### Files Created
- `source/timewarp-nuru-analyzers/analyzers/generator-helpers.cs` - Shared helper with `GetUseNewGenProvider()` method
- `source/timewarp-nuru-analyzers/analyzers/nuru-v2-generator.cs` - V2 stub generator that emits `NuruV2GeneratorMarker`

### Files Modified
- `Directory.Build.props` - Added `<UseNewGen>false</UseNewGen>` property and `<CompilerVisibleProperty Include="UseNewGen" />`
- `nuru-attributed-route-generator.cs` - Added UseNewGen check to skip generation
- `nuru-delegate-command-generator.cs` - Combined UseNewGen with existing suppress attribute check
- `nuru-invoker-generator.cs` - Combined UseNewGen with existing suppress attribute check

### Verification
| Build Command | V1 Generators | V2 Generator |
|--------------|---------------|--------------|
| `dotnet build` (default) | Ran | Skipped |
| `dotnet build -p:UseNewGen=true` | Skipped | Ran |

V2 marker emitted to `TimeWarp.Nuru.V2.Generated.NuruV2GeneratorMarker` namespace.
