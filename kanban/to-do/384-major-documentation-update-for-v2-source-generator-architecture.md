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
