# agents.md

## Build Commands
- Full build: `dotnet build TimeWarp.Nuru.slnx -c Release`
- Scripted build (with format/analyze): `cd Scripts && ./Build.cs`
- Clean & rebuild: `cd Scripts && ./CleanAndBuild.cs`
- Single project: `dotnet build Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj -c Release`

## Lint & Analyze Commands
- Format code: `dotnet format` (enforced in build; follows .editorconfig)
- Analyze: `cd Scripts && ./Analyze.cs` (Roslynator rules from Directory.Build.props; warnings as errors)
- Check style: Build fails on RCS1037 (no trailing whitespace), CA1031 (specific exceptions)

## Test Commands
- All integration tests (Delegate vs Mediator, JIT/AOT): `cd Tests && ./test-both-versions.sh`
- Single test app (Delegate): `./Tests/TimeWarp.Nuru.TestApp.Delegates/bin/Release/net9.0/TimeWarp.Nuru.TestApp.Delegates git status`
- Single test app (Mediator): `./Tests/TimeWarp.Nuru.TestApp.Mediator/bin/Release/net9.0/TimeWarp.Nuru.TestApp.Mediator git status`
- Run specific runfile: `dotnet run Tests/TimeWarp.Nuru.Tests/Lexer/lexer-01-basic-token-types.cs`
- Add to runner: Update `Tests/Scripts/run-all-tests.cs` for new tests

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