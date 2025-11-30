# Migrate Scripts Directory

## Description

Rename the Scripts directory and all contents to kebab-case naming convention.

## Parent

087_Migrate-Repository-To-Kebab-Case-Naming

## Requirements

- Rename `Scripts/` → `scripts/`
- Rename PascalCase C# runfiles to kebab-case:
  - `Analyze.cs` → `analyze.cs`
  - `Build.cs` → `build.cs`
  - `CheckVersion.cs` → `check-version.cs`
  - `Clean.cs` → `clean.cs`
  - `CleanAndBuild.cs` → `clean-and-build.cs`
  - `Format.cs` → `format.cs`
  - `generate-internals-visible-to.cs` (already kebab-case)

## Checklist

- [x] Rename Scripts directory to lowercase
- [x] Rename C# runfiles to kebab-case
- [x] Update any script cross-references
- [x] Verify scripts still execute correctly

## Notes

- These are .NET 10 runfiles, not traditional scripts
- Verify shebang lines and execution permissions preserved
