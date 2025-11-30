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

- [ ] Rename Samples directory to lowercase
- [ ] Rename all subdirectories to kebab-case
- [ ] Rename C# files to kebab-case
- [ ] Rename markdown files to lowercase
- [ ] Update examples.json paths
- [ ] Update any sample .csproj files
- [ ] Verify samples still build and run

## Notes

- Preserve `Properties/launchSettings.json` case (dotnet convention)
- Some samples have .csproj files that need path updates
- `CoconaComparison/` has nested structure requiring careful handling
