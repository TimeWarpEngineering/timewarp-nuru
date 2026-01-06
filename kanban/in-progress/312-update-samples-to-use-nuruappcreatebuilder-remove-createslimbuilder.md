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
- [x] `05-aot-example/aot-example.cs` (removed Mediator, verified AOT publish works)
- [x] `06-async-examples/async-examples.cs` (updated to CreateBuilder)
- [x] `07-pipeline-middleware/` (all 6 samples working)
- [x] `08-testing/01-output-capture.cs` - Updated from CreateSlimBuilder, renamed
- [x] `08-testing/02-colored-output.cs` - Updated, split Test 5 for #319 workaround
- [x] `08-testing/03-terminal-injection.cs` - Updated from CreateSlimBuilder, renamed
- [x] `08-testing/04-debug-test.cs` - Updated from CreateSlimBuilder, renamed

## Samples Needing Updates

### Remaining in 08-testing
- [ ] `08-testing/runfile-test-harness/real-app.cs`

### Uses Mediator (replace with TimeWarp.Nuru interfaces)
- [x] `05-aot-example/aot-example.cs` - DONE (removed AddMediator)
- [ ] `_logging/console-logging.cs` - UseConsoleLogging, Mediator
- [ ] `_logging/serilog-logging.cs` - likely same issues
- [x] `06-async-examples/async-examples.cs` - DONE (updated to CreateBuilder)

### Uses private methods as handlers (NURU_H004)
- [ ] `configuration/configuration-basics.cs`
- [ ] `configuration/configuration-validation.cs`
- [ ] Other configuration samples

### Generator formatting issues (#313)
- [ ] `builtin-types-example.cs` - block body handler formatting
- [ ] `custom-type-converter-example.cs` - likely same

### Pipeline Middleware Samples - DONE
- [x] `07-pipeline-middleware/*` - All 6 samples working (completed in #315)

### Unchecked (need verification)
- [ ] `_aspire-host-otel/`
- [ ] `_aspire-telemetry/`
- [ ] `_dynamic-completion-example/`
- [ ] `_repl-demo/`
- [ ] `_shell-completion-example/`
- [ ] `_terminal/`
- [ ] `_unified-middleware/`
- [ ] `_timewarp-nuru-sample/`

## Notes

### Generator Bug Fix Required
During migration, discovered that multiple `RunAsync()` calls caused CS9153 error due to duplicate
intercept sites. Fixed by adding deduplication in `CombineModels`:
- `allInterceptSites.DistinctBy(site => site.GetAttributeSyntax())`
- See #318 for architectural fix (process files only once)
- See #319 for bug with multiple apps in same block

### When updating samples:
1. Replace `CreateSlimBuilder` with `NuruApp.CreateBuilder(args)`
2. Replace Mediator interfaces with TimeWarp.Nuru interfaces
3. Make handler methods `internal` or `public` (not `private`)
4. Move working samples to numbered folders (NN-name)
