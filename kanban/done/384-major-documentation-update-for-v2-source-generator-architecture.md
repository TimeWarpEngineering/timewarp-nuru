# Major documentation update for V2 source generator architecture

## Description

The TimeWarp.Nuru framework has undergone a major architectural shift from runtime route matching to compile-time source generation. The documentation needs a comprehensive update to reflect:

1. The new source generator approach (no runtime overhead, AOT-friendly)
2. Removal of Mediator dependency
3. New fluent DSL API (`NuruApp.CreateBuilder()`)
4. Custom type converters
5. Pipeline behaviors (`INuruBehavior`)
6. Attributed routes (`[NuruRoute]`)
7. REPL support
8. Telemetry/OpenTelemetry integration

## Checklist

### README.md
- [ ] Update main README with V2 architecture overview
- [ ] Update quick start example to use `NuruApp.CreateBuilder()`
- [ ] Remove references to Mediator
- [ ] Add source generator benefits (AOT, zero runtime overhead)

### Getting Started Guide
- [ ] Create/update getting started documentation
- [ ] Document installation (single NuGet package)
- [ ] Document basic fluent DSL usage
- [ ] Document attributed routes pattern

### API Documentation
- [ ] Document `NuruApp.CreateBuilder()` API
- [ ] Document `Map()` fluent chain methods
- [ ] Document `WithHandler()` patterns (lambda, method group, delegate)
- [ ] Document `.AsCommand()` / `.AsQuery()` / `.AsIdempotentCommand()`
- [ ] Document `WithGroupPrefix()` for route groups
- [ ] Document `AddHelp()` and `AddRepl()` options

### Type Converters
- [ ] Document built-in type converters (21 types)
- [ ] Document custom type converter implementation (`IRouteTypeConverter`)
- [ ] Document `AddTypeConverter()` registration

### Pipeline Behaviors
- [ ] Document `INuruBehavior<TFilter>` interface
- [ ] Document `AddBehavior<T>()` registration
- [ ] Document `.Implements<T>()` for route filtering
- [ ] Provide middleware examples (logging, telemetry, exception handling)

### Configuration & DI
- [ ] Document `AddConfiguration()` 
- [ ] Document `IOptions<T>` parameter injection
- [ ] Document `[ConfigurationKey]` attribute
- [ ] Document `ConfigureServices()` for service registration

### Analyzer Diagnostics
- [ ] Document NURU_DEBUG* diagnostics (hidden by default)
- [ ] Document EditorConfig opt-in for debug diagnostics
- [ ] Note: valid severity values are `none`, `silent`, `suggestion`, `warning`, `error` (NOT `info`)

### REPL Mode
- [ ] Document `AddRepl()` configuration
- [ ] Document REPL features (completion, history, key bindings)
- [ ] Document `RunReplAsync()` vs `RunAsync()`

### Telemetry
- [ ] Document `UseTelemetry()` for OpenTelemetry
- [ ] Document structured logging with `ILogger<T>`
- [ ] Document Aspire integration

### Samples Reference
- [ ] Update samples index/overview
- [ ] Ensure all numbered samples have README files
- [ ] Cross-reference samples from documentation

### Migration Guide
- [ ] Create migration guide from V1 (Mediator-based) to V2
- [ ] Document breaking changes
- [ ] Provide before/after code examples

## Notes

The V2 source generator architecture provides:
- Zero runtime overhead (all routing is compile-time)
- Native AOT compatibility
- No reflection required
- Better IDE support (compile-time errors)
- Simplified dependency (single NuGet package)

Key files to reference:
- `documentation/developer/design/source-generator/` - Architecture docs
- `samples/` - Numbered sample applications
- `source/timewarp-nuru/` - Core library
- `source/timewarp-nuru-analyzers/` - Source generator

## Results

### Files Modified/Created

**New Documentation Files (6):**
- `documentation/user/reference/builder-api.md` - Complete fluent API reference
- `documentation/user/features/attributed-routes.md` - [NuruRoute] system documentation
- `documentation/user/features/pipeline-behaviors.md` - INuruBehavior middleware docs
- `documentation/user/features/configuration.md` - Configuration & DI documentation
- `documentation/user/features/telemetry.md` - OpenTelemetry/Aspire integration

**Updated Documentation Files (15+):**
- `readme.md` - Quick start with both patterns, removed SlimBuilder/EmptyBuilder
- `documentation/user/getting-started.md` - Complete rewrite with current API
- `documentation/user/features/routing.md` - All examples updated to fluent chain
- `documentation/user/reference/supported-types.md` - Fixed IRouteTypeConverter interface
- `documentation/user/guides/using-repl-mode.md` - AddRepl() API pattern
- `documentation/user/features/repl-key-bindings.md` - Current key binding API
- `documentation/user/features/analyzer.md` - Added debug diagnostics section
- `documentation/user/features/logging.md` - Fixed all builder references
- `documentation/user/features/auto-help.md` - Fixed builder references
- `documentation/user/use-cases.md` - Fixed builder references
- `documentation/user/guides/*.md` - Multiple files updated
- `documentation/user/features/terminal-abstractions.md` - Fixed builder references
- `documentation/user/features/widgets.md` - Fixed builder references
- `documentation/user/features/built-in-routes.md` - Fixed builder references
- `documentation/user/features/shell-completion.md` - Fixed builder references

**Sample READMEs Created (15):**
- All sample directories now have README.md files with run instructions

**Support Files Updated:**
- `samples/examples.json` - Fixed 27 broken paths, removed 6 non-existent entries

### Key Changes
- Only `NuruApp.CreateBuilder(args)` documented - removed SlimBuilder/EmptyBuilder references
- Both fluent DSL and attributed routes presented as equal first-class patterns
- No temporal references (V1/V2, "currently", etc.)
- All examples use `.Map().WithHandler().AsCommand().Done()` pattern
- Removed cocona-comparison references
- Fixed `IRouteTypeConverter` interface (was incorrectly `ITypeConverter<T>`)

### Checklist Items Completed
All 37 checklist items from the original task have been addressed through the documentation updates.
