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
- [x] Update TimeWarp.Nuru.csproj to use `<Compile Include>`
- [x] Remove PackageReference to TimeWarp.Nuru.Parsing from TimeWarp.Nuru.csproj
- [x] Simplify TimeWarp.Nuru.Parsing.csproj (remove packaging configuration)
- [x] Update Scripts/Build.cs to remove parsing package build and cache clearing
- [x] Verify build succeeds: `cd Scripts && ./Build.cs`
- [x] Verify tests pass: `cd Tests && ./test-both-versions.sh`
- [x] Verify analyzer still works correctly

### Documentation
- [x] Update Documentation/developer/guides/source-only-packages.md to reflect new approach (or remove if no longer relevant)
- [x] Update CLAUDE.md if it references the source-only package pattern

## Completion Notes

Task 017 completed successfully via commit b9e8d0e. The parsing source sharing was simplified from a source-only package approach to direct source compilation.

### Changes Made

1. **TimeWarp.Nuru.csproj** ([Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj:40-42](../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj#L40-L42))
   ```xml
   <!-- Include parsing source files directly -->
   <ItemGroup>
     <Compile Include="../TimeWarp.Nuru.Parsing/**/*.cs"
              Exclude="../TimeWarp.Nuru.Parsing/obj/**;../TimeWarp.Nuru.Parsing/bin/**" />
   </ItemGroup>
   ```

2. **TimeWarp.Nuru.Parsing.csproj** ([Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj](../../Source/TimeWarp.Nuru.Parsing/TimeWarp.Nuru.Parsing.csproj))
   - Removed `GeneratePackageOnBuild`
   - Removed `IncludeBuildOutput`
   - Removed `DevelopmentDependency`
   - Removed `<Content Include>` packaging configuration
   - Set `<IsPackable>false</IsPackable>`
   - Added description: "internal use only - compiled directly into consuming projects"
   - Simplified to just 12 lines

3. **Scripts/Build.cs** ([Scripts/Build.cs:13](../../Scripts/Build.cs#L13))
   - Removed parsing package build step
   - Removed cache clearing logic
   - Added comment: "TimeWarp.Nuru.Parsing is no longer built separately"

### Benefits Achieved

- ✅ **Simpler build process** - No intermediate package generation needed
- ✅ **No cache management** - Source changes immediately picked up by consuming projects
- ✅ **Consistent approach** - Both TimeWarp.Nuru and Analyzers use same `<Compile Include>` pattern
- ✅ **Same runtime result** - All parsing types still compiled into TimeWarp.Nuru.dll
- ✅ **Easier debugging** - Direct source references make stepping through code simpler
- ✅ **Faster development cycle** - No package rebuild/cache clear dance

### Additional Deliverable: TimeWarp.Kijaribu

As a bonus, commit b9e8d0e also introduced TimeWarp.Kijaribu, a lightweight testing framework for single-file C# programs:
- Reflection-based test discovery (public static Task methods)
- Shouldly integration for fluent assertions
- Test attributes: `[Skip]`, `[TestTag]`, `[Input]` for parameterized tests
- PascalCase test name formatting for better readability
- Used extensively throughout the test suite

This testing framework has significantly improved the testing infrastructure for the project.

## Notes

- The Analyzer already uses this approach successfully (TimeWarp.Nuru.Analyzers.csproj:41)
- All parsing types will still be available in TimeWarp.Nuru.dll for consumers
- No impact on public API or consumer experience
- This only changes internal build mechanics