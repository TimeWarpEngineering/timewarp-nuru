# Bug: MSBuild task DLL missing from 3.0.0-beta.28 - TimeWarp.Nuru.Build.dll not in build/net10.0/

## Description

When building a project that references `TimeWarp.Nuru 3.0.0-beta.28`, the build fails with MSB4062 because the MSBuild task assembly is missing from the NuGet package.

## Error

```
error MSB4062: The "TimeWarp.Nuru.Build.GenerateNuruJsonContextTask" task could not be loaded
from the assembly ~/.nuget/packages/timewarp.nuru/3.0.0-beta.28/build/net10.0/TimeWarp.Nuru.Build.dll.
Could not load file or assembly. The system cannot find the file specified.
```

## Expected

The package should contain `build/net10.0/TimeWarp.Nuru.Build.dll` (or appropriate TFM folder).

## Reproduction

1. Reference `TimeWarp.Nuru 3.0.0-beta.28` in a .NET 10 project
2. Run `dotnet build`
3. Observe MSB4062 error

## Environment

- Package version: 3.0.0-beta.28
- Target framework: net10.0
- OS: Linux (WSL2)

## Root Cause Analysis

The published `TimeWarp.Nuru 3.0.0-beta.28` package (551KB) was missing the build assets, while the local artifacts package (4.8MB) contained them correctly. This indicates the package was published from a build state where the `timewarp-nuru-build` project outputs didn't exist.

The packaging configuration in `timewarp-nuru.csproj` uses a wildcard:
```xml
<None Include="../timewarp-nuru-build/bin/$(Configuration)/net10.0/*.dll"
      Pack="true"
      PackagePath="build/net10.0" />
```

This can fail to include files when the build project hasn't been compiled before packing.

## Resolution

- [x] Verify `TimeWarp.Nuru.Build.dll` is included in package during publish
- [x] Check `.nuspec` or `.csproj` pack configuration for build assets
- [x] Bump version to 3.0.0-beta.29
- [x] Clean rebuild in Release configuration
- [x] Verify new package contains build DLLs (4.8MB with all assets)
- [ ] Publish 3.0.0-beta.29 to NuGet

## Notes

Discovered while migrating ccc1-cli from Nuru 2.1.0-beta.28 to 3.0.0-beta.28 Endpoints API.
