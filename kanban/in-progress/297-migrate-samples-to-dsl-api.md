# Migrate samples to DSL API

## Description

Update all samples to use the current DSL API. Many samples use deprecated APIs like `CreateSlimBuilder()` or methods the source generator doesn't recognize (`ConfigureServices`, `AddMediator`, `AddAutoHelp`).

## Checklist

### Working
- [x] hello-world/hello-world.cs

### Simple Samples (delegate-only)
- [ ] calculator/calc-delegate.cs - needs `CreateSlimBuilder` -> `CreateBuilder`
- [ ] calculator/calc-createbuilder.cs - generator fails on `ConfigureServices`
- [ ] calculator/calc-mediator.cs
- [ ] calculator/calc-mixed.cs

### AOT & Attributed
- [ ] aot-example/aot-example.cs - generator fails on `AddMediator`
- [ ] attributed-routes/attributed-routes.cs - generator fails on `AddAutoHelp`

### Configuration
- [ ] configuration/command-line-overrides.cs
- [ ] configuration/configuration-basics.cs
- [ ] configuration/configuration-validation.cs
- [ ] configuration/user-secrets-property.cs

### Logging
- [ ] logging/console-logging.cs
- [ ] logging/serilog-logging.cs

### Pipeline/Middleware
- [ ] pipeline-middleware/pipeline-middleware.cs
- [ ] pipeline-middleware/pipeline-middleware-authorization.cs
- [ ] pipeline-middleware/pipeline-middleware-basic.cs
- [ ] pipeline-middleware/pipeline-middleware-exception.cs
- [ ] pipeline-middleware/pipeline-middleware-retry.cs
- [ ] pipeline-middleware/pipeline-middleware-telemetry.cs

### REPL
- [ ] repl-demo/repl-basic-demo.cs
- [ ] repl-demo/repl-custom-keybindings.cs
- [ ] repl-demo/repl-interactive-mode.cs
- [ ] repl-demo/repl-options-showcase.cs
- [ ] repl-demo/repl-prompt-fix-demo.cs

### Completion
- [ ] dynamic-completion-example/dynamic-completion-example.cs
- [ ] shell-completion-example/shell-completion-example.cs

### Terminal
- [ ] terminal/hyperlink-widget.cs
- [ ] terminal/panel-widget.cs
- [ ] terminal/rule-widget.cs
- [ ] terminal/table-widget.cs

### Other
- [ ] aspire-host-otel/
- [ ] aspire-telemetry/aspire-telemetry.cs
- [ ] timewarp-nuru-sample/program.cs
- [ ] unified-middleware/unified-middleware.cs
- [ ] builtin-types-example.cs
- [ ] custom-type-converter-example.cs
- [ ] syntax-examples.cs
- [ ] async-examples/async-examples.cs

### Testing Samples
- [ ] testing/debug-test.cs
- [ ] testing/test-colored-output.cs
- [ ] testing/test-output-capture.cs
- [ ] testing/test-terminal-injection.cs

## Notes

- `hello-world.cs` works because it uses simple `NuruApp.CreateBuilder(args)` with only `.Map().WithHandler().AsQuery().Done()` pattern
- Source generator fails on:
  - `CreateSlimBuilder()` - method doesn't exist
  - `ConfigureServices()` - not recognized by generator
  - `AddMediator()`, `AddAutoHelp()` - not recognized by generator
  - `Map<TCommand>()` without `WithHandler()` - generator requires handler
