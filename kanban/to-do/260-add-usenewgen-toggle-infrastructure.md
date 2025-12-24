# Add `<UseNewGen>` Toggle Infrastructure

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Add MSBuild property `<UseNewGen>` that controls which source generator path runs. When false (default), existing V1 generators run. When true, new V2 generator runs instead.

## Checklist

- [ ] Add `<CompilerVisibleProperty Include="UseNewGen" />` to Directory.Build.props
- [ ] Modify V1 generators to check property and skip if `UseNewGen=true`
  - `nuru-attributed-route-generator.cs`
  - `nuru-delegate-command-generator.cs`
  - `nuru-invoker-generator.cs`
  - `nuru-route-analyzer.cs`
- [ ] Create V2 generator stub that only runs when `UseNewGen=true`
- [ ] V2 initially emits marker type to prove it ran
- [ ] Verify: build with default (V1 runs), build with `UseNewGen=true` (V2 runs)
