# Migrate Source Directory

## Description

Rename the Source directory and all project directories to kebab-case. Update all project references.

## Parent

087_Migrate-Repository-To-Kebab-Case-Naming

## Requirements

- Rename `Source/` → `source/`
- Rename project directories:
  - `TimeWarp.Nuru/` → `timewarp-nuru/`
  - `TimeWarp.Nuru.Analyzers/` → `timewarp-nuru-analyzers/`
  - `TimeWarp.Nuru.Completion/` → `timewarp-nuru-completion/`
  - `TimeWarp.Nuru.Core/` → `timewarp-nuru-core/`
  - `TimeWarp.Nuru.Logging/` → `timewarp-nuru-logging/`
  - `TimeWarp.Nuru.Mcp/` → `timewarp-nuru-mcp/`
  - `TimeWarp.Nuru.Parsing/` → `timewarp-nuru-parsing/`
  - `TimeWarp.Nuru.Repl/` → `timewarp-nuru-repl/`
  - `TimeWarp.Nuru.Telemetry/` → `timewarp-nuru-telemetry/`
- Rename all .csproj files to kebab-case
- Rename all C# files to kebab-case
- Update all `<ProjectReference>` paths in every .csproj
- Preserve assembly names as PascalCase in .csproj

## Checklist

- [x] Rename Source directory to lowercase
- [x] Rename all project directories to kebab-case
- [x] Rename all .csproj files
- [x] Rename all C# source files (~162 files)
- [x] Update ProjectReference paths in all .csproj files
- [x] Verify assembly names remain PascalCase
- [x] Run `dotnet build` to verify

## Notes

- HIGH RISK - this is the core of the migration
- ~162 C# files renamed to kebab-case
- ~30+ ProjectReference updates across all .csproj files
- Consider creating automation script for bulk renames
- Commit incrementally (per project if needed)
- Keep `Directory.Build.props` case as-is (MSBuild convention)
- Keep `GlobalUsings.cs` case as-is (GlobalUsingsAnalyzer convention)

## Completed

Completed 2025-11-30. All source directory migrations complete and verified:

- Renamed Source/ → source/
- Renamed all 9 project directories to kebab-case
- Renamed all 9 .csproj files to kebab-case
- Renamed 162 C# source files to kebab-case (excluding GlobalUsings.cs which has convention requirements)
- Renamed all subdirectories to kebab-case (e.g., TypeConversion → type-conversion)
- Added AssemblyName and RootNamespace to all .csproj files to preserve PascalCase assembly names
- Updated all ProjectReference paths
- Updated repository.props SourceDirectory path
- Updated TimeWarp.Nuru.slnx with new paths
- Build verification successful for all source projects
