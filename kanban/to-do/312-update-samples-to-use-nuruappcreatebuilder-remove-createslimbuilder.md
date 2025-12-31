# Update samples to use NuruApp.CreateBuilder (remove CreateSlimBuilder)

## Summary

Many samples reference `CreateSlimBuilder` or `CreateFullBuilder` which no longer exist.
Update all samples to use `NuruApp.CreateBuilder()`.

Also some samples use Mediator which is being replaced with TimeWarp.Nuru message interfaces.

## Working Samples (NN-* numbered convention)

These samples have been verified working with the new API:

- [x] `01-hello-world/hello-world.cs`
- [x] `02-calculator/01-calc-delegate.cs`
- [x] `02-calculator/02-calc-commands.cs`
- [x] `02-calculator/03-calc-mixed.cs`
- [x] `03-attributed-routes/` (csproj)
- [x] `04-syntax-examples/syntax-examples.cs`

## Samples Needing Updates

### Uses CreateSlimBuilder (obsolete)
- [ ] `testing/test-output-capture.cs`
- [ ] `testing/test-colored-output.cs`
- [ ] `testing/test-terminal-injection.cs`
- [ ] `testing/debug-test.cs`
- [ ] `testing/runfile-test-harness/real-app.cs`

### Uses Mediator (replace with TimeWarp.Nuru interfaces)
- [ ] `aot-example/aot-example.cs` - AddMediator
- [ ] `logging/console-logging.cs` - UseConsoleLogging, Mediator
- [ ] `logging/serilog-logging.cs` - likely same issues
- [ ] `async-examples/async-examples.cs` - CreateSlimBuilder

### Uses private methods as handlers (NURU_H004)
- [ ] `configuration/configuration-basics.cs`
- [ ] `configuration/configuration-validation.cs`
- [ ] Other configuration samples

### Generator formatting issues (#313)
- [ ] `builtin-types-example.cs` - block body handler formatting
- [ ] `custom-type-converter-example.cs` - likely same

### Unchecked (need verification)
- [ ] `aspire-host-otel/`
- [ ] `aspire-telemetry/`
- [ ] `dynamic-completion-example/`
- [ ] `pipeline-middleware/`
- [ ] `repl-demo/`
- [ ] `shell-completion-example/`
- [ ] `terminal/`
- [ ] `unified-middleware/`
- [ ] `timewarp-nuru-sample/`

## Notes

When updating samples:
1. Replace `CreateSlimBuilder` with `NuruApp.CreateBuilder(args)`
2. Replace Mediator interfaces with TimeWarp.Nuru interfaces
3. Make handler methods `internal` or `public` (not `private`)
4. Move working samples to numbered folders (NN-name)
