# 017 Simplify Parsing Source Sharing

## Description

Simplify how TimeWarp.Nuru.Parsing source code is shared between TimeWarp.Nuru and TimeWarp.Nuru.Analyzers by removing the source-only NuGet package approach and using direct source compilation (`<Compile Include>`) for both projects.

## Current State

- **TimeWarp.Nuru.Parsing**: Configured as a source-only NuGet package with `GeneratePackageOnBuild`, content file packaging, etc.
- **TimeWarp.Nuru**: References parsing via `PackageReference` with `PrivateAssets="all"`
- **TimeWarp.Nuru.Analyzers**: Already uses `<Compile Include>` to compile parsing sources directly
- **Scripts/Build.cs**: Contains cache-clearing logic (lines 30-37) to handle stale source-only package caching

## Problem

The source-only package approach adds unnecessary complexity:
1. Requires building and publishing the parsing package to LocalNuGetFeed
2. Requires cache clearing to avoid stale source files
3. More complex build process
4. Only valuable if we intended to publish TimeWarp.Nuru.Parsing as a standalone library (which we don't)

## Proposed Solution

Convert TimeWarp.Nuru to use the same `<Compile Include>` approach that the Analyzer already uses:

### TimeWarp.Nuru.csproj
Replace the PackageReference with direct source compilation:
```xml
<ItemGroup>
  <Compile Include="..\TimeWarp.Nuru.Parsing\**\*.cs"
           Exclude="..\TimeWarp.Nuru.Parsing\obj\**;..\TimeWarp.Nuru.Parsing\bin\**" />
</ItemGroup>
```

### TimeWarp.Nuru.Parsing.csproj
Remove packaging-related properties and items:
- Remove `GeneratePackageOnBuild`
- Remove `IncludeBuildOutput`
- Remove `DevelopmentDependency`
- Remove `<Content Include>` packaging configuration
- Keep only the basic project structure and dependency on Microsoft.Extensions.Logging.Abstractions

### Scripts/Build.cs
Remove the parsing package build step and cache clearing logic (lines 13-37)

## Benefits

- ✅ Simpler build process (no intermediate package generation)
- ✅ No cache management needed
- ✅ Consistent approach across both consuming projects
- ✅ Same runtime result: all parsing types compiled into TimeWarp.Nuru.dll
- ✅ Easier to debug and understand

## Checklist

### Implementation
- [ ] Update TimeWarp.Nuru.csproj to use `<Compile Include>`
- [ ] Remove PackageReference to TimeWarp.Nuru.Parsing from TimeWarp.Nuru.csproj
- [ ] Simplify TimeWarp.Nuru.Parsing.csproj (remove packaging configuration)
- [ ] Update Scripts/Build.cs to remove parsing package build and cache clearing
- [ ] Verify build succeeds: `cd Scripts && ./Build.cs`
- [ ] Verify tests pass: `cd Tests && ./test-both-versions.sh`
- [ ] Verify analyzer still works correctly

### Documentation
- [ ] Update Documentation/developer/guides/source-only-packages.md to reflect new approach (or remove if no longer relevant)
- [ ] Update CLAUDE.md if it references the source-only package pattern

## Notes

- The Analyzer already uses this approach successfully (TimeWarp.Nuru.Analyzers.csproj:41)
- All parsing types will still be available in TimeWarp.Nuru.dll for consumers
- No impact on public API or consumer experience
- This only changes internal build mechanics