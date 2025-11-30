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

- [x] Rename Tests directory to lowercase
- [x] Rename all test project directories
- [x] Rename all .csproj files
- [x] Rename PascalCase C# files (many already kebab-case)
- [x] Update ProjectReference paths
- [x] Update test scripts (test-both-versions.sh)
- [x] Run tests to verify

## Completed

All items completed on 2025-11-30. Changes include:

### Directory Renames
- `Tests/` → `tests/`
- `TestApps/` → `test-apps/`
- `TimeWarp.Nuru.Tests/` → `timewarp-nuru-tests/`
- `TimeWarp.Nuru.Analyzers.Tests/` → `timewarp-nuru-analyzers-tests/`
- `TimeWarp.Nuru.Completion.Tests/` → `timewarp-nuru-completion-tests/`
- `TimeWarp.Nuru.Mcp.Tests/` → `timewarp-nuru-mcp-tests/`
- `TimeWarp.Nuru.Repl.Tests/` → `timewarp-nuru-repl-tests/`
- `Scripts/` → `scripts/`
- All nested subdirectories (Lexer → lexer, Configuration → configuration, etc.)

### File Renames
- `.csproj` files renamed to kebab-case
- PascalCase C# files renamed (LexerTestHelper.cs → lexer-test-helper.cs, etc.)
- Program.cs → program.cs in test apps

### Reference Updates
- Updated all `#:project` directives in test files
- Updated Directory.Build.props with new paths
- Updated repository.props TestsDirectory
- Updated TimeWarp.Nuru.slnx
- Updated test-both-versions.sh
- Updated run-all-tests.cs, run-mcp-tests.cs, run-nuru-tests.cs, run-repl-tests.cs

### Assembly Names
- Added AssemblyName and RootNamespace to timewarp-nuru-analyzers-tests.csproj
- Test apps already had AssemblyName configured

## Notes

- HIGH RISK - project reference dependencies
- Many test files already use kebab-case naming
- `test-both-versions.sh` may have hardcoded paths
- Test apps in TestApps/ have their own .csproj files
