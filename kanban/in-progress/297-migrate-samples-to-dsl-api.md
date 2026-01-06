# Migrate samples to DSL API

## Description

Update all samples to use the current DSL API. Many samples use deprecated APIs like `CreateSlimBuilder()` or methods the source generator doesn't recognize (`ConfigureServices`, `AddMediator`, `AddAutoHelp`).

## Current Sample Structure

Samples are organized as:
- `NN-name/` - Numbered folders for complete/working samples
- `_name/` - Underscore prefix for WIP/incomplete samples

## Checklist

### Completed (NN-* folders)
- [x] `01-hello-world/hello-world.cs` - Works
- [x] `02-calculator/01-calc-delegate.cs` - Works (delegates only)
- [x] `02-calculator/02-calc-commands.cs` - Works (attributed routes)
- [x] `02-calculator/03-calc-mixed.cs` - Works (mixed delegates + attributed)
- [x] `03-attributed-routes/` - Works (full attributed routes sample, #304)
- [x] `04-syntax-examples/syntax-examples.cs` - **BROKEN** (wrong project reference)
- [x] `05-aot-example/` - Works (AOT publish verified)
- [x] `06-async-examples/async-examples.cs` - Works
- [x] `07-pipeline-middleware/` - Works (all 6 samples, #315)

### WIP Samples (_* folders) - Need Migration

#### Configuration (_configuration/)
- [ ] `command-line-overrides.cs`
- [ ] `configuration-basics.cs`
- [ ] `configuration-validation.cs`
- [ ] `user-secrets-property.cs`

#### Logging (_logging/)
- [ ] `console-logging.cs` - Uses `UseConsoleLogging`, Mediator
- [ ] `serilog-logging.cs`

#### REPL (_repl-demo/)
- [ ] `repl-basic-demo.cs`
- [ ] `repl-custom-keybindings.cs`
- [ ] `repl-interactive-mode.cs`
- [ ] `repl-options-showcase.cs`
- [ ] `repl-prompt-fix-demo.cs`

#### Completion
- [ ] `_dynamic-completion-example/dynamic-completion-example.cs`
- [ ] `_shell-completion-example/shell-completion-example.cs`

#### Testing (_testing/)
- [ ] `debug-test.cs` - Uses `CreateSlimBuilder`
- [ ] `test-colored-output.cs` - Uses `CreateSlimBuilder`
- [ ] `test-output-capture.cs` - Uses `CreateSlimBuilder`
- [ ] `test-terminal-injection.cs` - Uses `CreateSlimBuilder`

#### Other
- [ ] `_aspire-host-otel/` - Aspire integration
- [ ] `_aspire-telemetry/aspire-telemetry.cs` - Aspire integration
- [ ] `_timewarp-nuru-sample/` - Reference sample project
- [ ] `_unified-middleware/unified-middleware.cs`

### Root-level samples (need to organize)
- [ ] `builtin-types-example.cs` - Block body handler formatting (#313)
- [ ] `custom-type-converter-example.cs` - Block body handler formatting (#313)

## Notes

- Terminal samples moved to `timewarp-terminal` repo
- Pipeline middleware samples all complete (#315, #316)
- Attributed routes decoupled from Mediator (#304)
- Testing samples use obsolete `CreateSlimBuilder` (#312)

## Related Tasks

- #312 - Update samples to use NuruApp.CreateBuilder
- #313 - Fix generator block body handler formatting issues
- #314 - Generator support for IConfiguration and IOptions
