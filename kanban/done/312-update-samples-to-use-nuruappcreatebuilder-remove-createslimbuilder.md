# Update samples to use NuruApp.CreateBuilder (remove CreateSlimBuilder)

## Results

**COMPLETE** - All numbered samples migrated to `NuruApp.CreateBuilder()`.

**Completed samples (01-12, 99):**
- 01-hello-world (3 samples)
- 02-calculator (3 samples)
- 03-attributed-routes (csproj)
- 04-syntax-examples
- 05-aot-example (csproj)
- 06-async-examples
- 07-pipeline-middleware (6 samples)
- 08-testing (4 samples + runfile harness)
- 09-configuration (4 samples)
- 10-type-converters (2 samples)
- 11-unified-middleware
- 12-logging (2 samples) - migrated after #322 completed ILogger<T> support
- 99-timewarp-nuru-sample

**Underscore samples requiring library-level changes tracked separately:**
- `_repl-demo/` - #338 (requires REPL library migration)
- `_aspire-*` - #339 (requires Telemetry library migration)
- `_completion-*` - #340 (requires Completion library migration)

## Summary

Many samples reference `CreateSlimBuilder` or `CreateFullBuilder` which no longer exist.
Update all samples to use `NuruApp.CreateBuilder()`.

Also some samples use Mediator which is being replaced with TimeWarp.Nuru message interfaces.

## Progress

**Numbered samples (01-12) are working with `NuruApp.CreateBuilder()`:**
- 01-hello-world (3 samples)
- 02-calculator (3 samples)
- 03-attributed-routes (csproj)
- 04-syntax-examples
- 05-aot-example (csproj)
- 06-async-examples
- 07-pipeline-middleware (6 samples)
- 08-testing (4 samples + runfile harness)
- 09-configuration (4 samples)
- 10-type-converters (2 samples)
- 11-unified-middleware
- 12-logging (2 samples) - completed via #322

**Remaining underscore samples tracked in separate tasks:**
- `_repl-demo/` - #338 (requires REPL library migration)
- `_aspire-*` - #339 (requires Telemetry library migration)
- `_completion-*` - #340 (requires Completion library migration)

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
- [x] `12-logging/console-logging.cs` - DONE (migrated via #322)
- [x] `12-logging/serilog-logging.cs` - DONE (migrated via #322)
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

### Underscore samples - Tracked in separate tasks
- [x] `_logging/` - DONE, renamed to `12-logging/` (completed via #322)
- [ ] `_repl-demo/` - See #338 (requires REPL library migration)
- [ ] `_aspire-host-otel/` - See #339 (requires Telemetry library migration)
- [ ] `_aspire-telemetry/` - See #339
- [ ] `_dynamic-completion-example/` - See #340 (requires Completion library)
- [ ] `_shell-completion-example/` - See #340

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
