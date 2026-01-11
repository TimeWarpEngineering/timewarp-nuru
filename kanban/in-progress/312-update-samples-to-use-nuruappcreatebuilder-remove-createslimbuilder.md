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
- [x] `09-configuration/` (all 4 samples working, renamed from _configuration)
- [x] `08-testing/02-colored-output.cs` - Updated, split Test 5 for #319 workaround
- [x] `08-testing/03-terminal-injection.cs` - Updated from CreateSlimBuilder, renamed
- [x] `08-testing/04-debug-test.cs` - Updated from CreateSlimBuilder, renamed
- [x] `08-testing/runfile-test-harness/real-app.cs` - Fixed in #320 (method reference handlers)
- [x] `10-type-converters/01-builtin-types.cs` - Working, demonstrates 15 built-in types
- [x] `10-type-converters/02-custom-type-converters.cs` - Fixed in #329 (custom type converter support)
- [x] `11-unified-middleware/unified-middleware.cs` - Working, demonstrates unified pipeline for delegate + attributed routes

## Samples Needing Updates

### Remaining in 08-testing
- [x] `08-testing/runfile-test-harness/real-app.cs` - Fixed in #320 (method reference handlers)

### Uses Mediator (replace with TimeWarp.Nuru interfaces)
- [x] `05-aot-example/aot-example.cs` - DONE (removed AddMediator)
- [x] `_logging/console-logging.cs` - Uses Nuru interfaces (IQuery, IQueryHandler), but **blocked by #322** (ILogger<T> injection)
- [ ] `_logging/serilog-logging.cs` - likely same issues
- [x] `06-async-examples/async-examples.cs` - DONE (updated to CreateBuilder)

### Configuration Samples - DONE (renamed to 09-configuration/)
- [x] `09-configuration/01-configuration-basics.cs` - Migrated to DSL API
- [x] `09-configuration/02-command-line-overrides.cs` - Migrated to DSL API with IOptions<T>
- [x] `09-configuration/03-configuration-validation.cs` - Migrated to IValidateOptions<T> (removed FluentValidation)
- [x] `09-configuration/04-user-secrets-property.cs` - Migrated to DSL API

### Generator DSL support issues - DONE
- [x] `10-type-converters/02-custom-type-converters.cs` - #328 fixed `AddTypeConverter` DSL recognition
- [x] `10-type-converters/02-custom-type-converters.cs` - #329 fixed custom type converter code generation

### Pipeline Middleware Samples - DONE
- [x] `07-pipeline-middleware/*` - All 6 samples working (completed in #315)

### Unchecked (need verification)
- [ ] `_aspire-host-otel/` - Requires Mediator, Repl, Telemetry libs
- [ ] `_aspire-telemetry/` - Requires Mediator, Telemetry lib
- [ ] `_dynamic-completion-example/` - Requires Completion lib
- [ ] `_repl-demo/` - Requires Mediator, Repl lib
- [ ] `_shell-completion-example/` - Requires Completion lib

### Completed (was marked blocked but works)
- [x] `11-unified-middleware/` - ✓ Working! (moved from _unified-middleware)

### Working (deferred decision on purpose)
- [x] `99-timewarp-nuru-sample/` - Works, but redundant with other samples. Decide later if kitchen sink app or template.

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

### Generator Enhancements Made During Migration
- **Config arg filtering** - Command-line config overrides (`--Section:Key=value`) now filtered from route matching
- **IValidateOptions<T> support** - Generator auto-detects validators and runs validation at startup
- **14 generator tests** all passing
