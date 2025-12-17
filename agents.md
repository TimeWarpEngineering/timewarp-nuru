# agents.md

## Build Commands
- Full build: `dotnet build timewarp-nuru.slnx -c Release`
- Runfile build (with format/analyze): `dotnet runfiles/build.cs`
- Clean & rebuild: `dotnet runfiles/clean-and-build.cs`
- Single project: `dotnet build source/timewarp-nuru/timewarp-nuru.csproj -c Release`

## Lint & Analyze Commands
- Format code: `dotnet format` (enforced in build; follows .editorconfig)
- Analyze: `dotnet runfiles/analyze.cs` (Roslynator rules from Directory.Build.props; warnings as errors)
- Check style: Build fails on RCS1037 (no trailing whitespace), CA1031 (specific exceptions)

## Test Commands
- Fast CI tests (~1700 tests, ~12s): `dotnet tests/ci-tests/run-ci-tests.cs`
- Full test suite (~1759 tests, ~25s): `dotnet tests/scripts/run-all-tests.cs`
- Integration tests (Delegate vs Mediator, JIT/AOT): `tests/test-both-versions.sh`
- Single test file: `dotnet tests/timewarp-nuru-core-tests/routing/routing-01-basic.cs`

## Kanban Task Guidelines
- **NEVER add these fields**: Status (folder location indicates status), Priority, Category, Priority Justification, Implementation Status
- **Use ONLY**: Description, Parent (optional), Requirements (optional), Checklist (optional), Notes (optional), Implementation Notes (optional)
- **WHY**: Folder structure (ToDo/InProgress/Done/Backlog) determines status; adding status fields creates redundancy
- **CRITICAL**: NEVER start a new Kanban task without explicitly asking the user first. Always wait for explicit approval before moving to a new task.

## Code Style Guidelines
- **Formatting**: Use `dotnet format`; 2-space indent (per .editorconfig), no trailing whitespace (RCS1037 fails build), blank lines between blocks (IDE2003).
- **Imports**: Prefer global usings (GlobalUsings.cs); explicit for locals; no `using static` unless necessary.
- **Types/Naming**: PascalCase public members/classes; camelCase private; explicit types over `var` (IDE0008); use collection expressions `[..]` where possible (IDE0301).
- **Error Handling**: Catch specific exceptions (CA1031); throw `InvalidOperationException` for logic errors, `ArgumentException` for inputs; log via ILogger.
- **General**: No temporal comments ("currently"); mimic existing patterns (stateless routes, DI via Microsoft.Extensions); AOT-compatible (no full reflection); no emojis/secrets in code. Follow Roslynator/CA rules in Directory.Build.props.
