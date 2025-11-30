# Migrate Tests Directory

## Description

Rename the Tests directory and all test project directories to kebab-case. Update all project references.

## Parent

087_Migrate-Repository-To-Kebab-Case-Naming

## Requirements

- Rename `Tests/` → `tests/`
- Rename subdirectories:
  - `TestApps/` → `test-apps/`
  - `TimeWarp.Nuru.Tests/` → `timewarp-nuru-tests/`
  - `TimeWarp.Nuru.Analyzers.Tests/` → `timewarp-nuru-analyzers-tests/`
  - `TimeWarp.Nuru.Completion.Tests/` → `timewarp-nuru-completion-tests/`
  - `TimeWarp.Nuru.Mcp.Tests/` → `timewarp-nuru-mcp-tests/`
  - `TimeWarp.Nuru.Repl.Tests/` → `timewarp-nuru-repl-tests/`
  - Nested test app directories
- Rename all .csproj files to kebab-case
- Rename PascalCase C# test files to kebab-case
- Update all `<ProjectReference>` paths

## Checklist

- [ ] Rename Tests directory to lowercase
- [ ] Rename all test project directories
- [ ] Rename all .csproj files
- [ ] Rename PascalCase C# files (many already kebab-case)
- [ ] Update ProjectReference paths
- [ ] Update test scripts (test-both-versions.sh)
- [ ] Run tests to verify

## Notes

- HIGH RISK - project reference dependencies
- Many test files already use kebab-case naming
- `test-both-versions.sh` may have hardcoded paths
- Test apps in TestApps/ have their own .csproj files
