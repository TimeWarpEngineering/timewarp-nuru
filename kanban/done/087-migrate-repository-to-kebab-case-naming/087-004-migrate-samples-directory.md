# Migrate Samples Directory

## Description

Rename the Samples directory and all contents to kebab-case naming convention.

## Parent

087_Migrate-Repository-To-Kebab-Case-Naming

## Requirements

- Rename `Samples/` → `samples/`
- Rename subdirectories to kebab-case:
  - `AspireHostOtel/` → `aspire-host-otel/`
  - `AspireTelemetry/` → `aspire-telemetry/`
  - `AsyncExamples/` → `async-examples/`
  - `Calculator/` → `calculator/`
  - `CoconaComparison/` → `cocona-comparison/`
  - `Configuration/` → `configuration/`
  - `DynamicCompletionExample/` → `dynamic-completion-example/`
  - `HelloWorld/` → `hello-world/`
  - `Logging/` → `logging/`
  - `PipelineMiddleware/` → `pipeline-middleware/`
  - `ReplDemo/` → `repl-demo/`
  - `ShellCompletionExample/` → `shell-completion-example/`
  - `Testing/` → `testing/`
  - `UnifiedMiddleware/` → `unified-middleware/`
- Rename PascalCase C# files to kebab-case
- Rename `README.md` → `readme.md`, `Overview.md` → `overview.md`
- Update `examples.json` with new paths

## Checklist

- [x] Rename Samples directory to lowercase
- [x] Rename all subdirectories to kebab-case
- [x] Rename C# files to kebab-case
- [x] Rename markdown files to lowercase
- [x] Update examples.json paths
- [x] Update any sample .csproj files
- [x] Verify samples still build and run

## Notes

- Preserved `Properties/launchSettings.json` case (dotnet convention)
- Renamed .csproj files to kebab-case (async-examples.csproj, user-secrets-demo.csproj, timewarp-nuru-sample.csproj)
- Renamed nested directories: `GettingStarted/` → `getting-started/`, `InAction/` → `in-action/`, `UserSecretsDemo/` → `user-secrets-demo/`
- Updated all internal references in documentation and source files
- Updated solution file (TimeWarp.Nuru.slnx) with new paths
- Updated MCP csproj embedded resource path for syntax-examples.cs

## Implementation Notes

Files renamed:
- Root: `BuiltInTypesExample.cs` → `builtin-types-example.cs`, `CustomTypeConverterExample.cs` → `custom-type-converter-example.cs`, `SyntaxExamples.cs` → `syntax-examples.cs`, `Overview.md` → `overview.md`
- async-examples: `Program.cs` → `program.cs`, `AsyncExamples.csproj` → `async-examples.csproj`
- logging: `ConsoleLogging.cs` → `console-logging.cs`, `SerilogLogging.cs` → `serilog-logging.cs`
- dynamic-completion-example: `DynamicCompletionExample.cs` → `dynamic-completion-example.cs`
- shell-completion-example: `ShellCompletionExample.cs` → `shell-completion-example.cs`
- cocona-comparison: `CLAUDE.md` → `claude.md`, `Agent.md` → `agent.md`, `CoconaComparisonTemplate.md` → `cocona-comparison-template.md`, `CoconaComparisonUpdateTracking.md` → `cocona-comparison-update-tracking.md`
- user-secrets-demo: `Program.cs` → `program.cs`, `UserSecretsDemo.csproj` → `user-secrets-demo.csproj`
- timewarp-nuru-sample: `Program.cs` → `program.cs`, `TimeWarp.Nuru.Sample.csproj` → `timewarp-nuru-sample.csproj`
- All Overview.md and README.md files renamed to lowercase
