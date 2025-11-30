# Migrate Repository To Kebab-Case Naming

## Description

Migrate the entire repository from PascalCase/mixed naming conventions to all-lowercase kebab-case file and directory naming, matching the standard established in timewarp-state repository.

See analysis: `@.agent/workspace/kebab-case-migration-analysis-2025-11-30.md`

## Requirements

- All directories use lowercase with hyphens (e.g., `source/timewarp-nuru/`)
- All C# files use kebab-case (e.g., `nuru-app-builder.cs`)
- All markdown files use lowercase (e.g., `readme.md`)
- Kanban tasks use hyphen format (e.g., `087-task-name.md`)
- Project references updated in all .csproj files
- Solution file updated with new paths
- CI/CD pipeline paths updated
- Preserve case for framework conventions (Directory.Build.props, *.razor, etc.)
- Build and tests pass after migration

## Checklist

### Phase 1: Low Risk
- [ ] 087_001: Migrate Kanban directory structure
- [ ] 087_002: Migrate root-level files (readme, license, etc.)
- [ ] 087_003: Migrate documentation directory

### Phase 2: Medium Risk
- [ ] 087_004: Migrate Samples directory
- [ ] 087_005: Migrate Scripts directory
- [ ] 087_006: Migrate Assets and msbuild directories

### Phase 3: High Risk
- [ ] 087_007: Migrate Source directory and update project references
- [ ] 087_008: Migrate Tests directory and update project references
- [ ] 087_009: Migrate Benchmarks directory
- [ ] 087_010: Update solution file and validate build
- [ ] 087_011: Update CI/CD pipeline and validate

## Notes

- Use `git mv` for all renames to preserve history
- Commit after each subtask for easier rollback
- Assembly names and NuGet package names remain PascalCase (only files/folders change)
- Framework conventions to preserve:
  - `Directory.Build.props`, `Directory.Packages.props`
  - `*.razor` files (Blazor components)
  - `_Imports.cs`, `_imports.razor`
  - `Properties/launchSettings.json`
