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

- [ ] Rename Source directory to lowercase
- [ ] Rename all project directories to kebab-case
- [ ] Rename all .csproj files
- [ ] Rename all C# source files (~150 files)
- [ ] Update ProjectReference paths in all .csproj files
- [ ] Verify assembly names remain PascalCase
- [ ] Run `dotnet build` to verify

## Notes

- HIGH RISK - this is the core of the migration
- ~150+ C# files to rename
- ~30+ ProjectReference updates across all .csproj files
- Consider creating automation script for bulk renames
- Commit incrementally (per project if needed)
- Keep `Directory.Build.props` case as-is (MSBuild convention)
