# Implement AddHttpClient support in source generator

## Description

Extend Nuru's source generator to detect and properly handle `AddHttpClient()` extension method calls, similar to how `AddLogging()` is already handled. This will allow users to use idiomatic .NET HTTP client patterns without requiring `UseMicrosoftDependencyInjection()`.

Currently, `AddHttpClient()` calls are detected but emit a NURU052 warning telling users to use runtime DI. We should support this extension method natively with AOT-optimized code generation.

## Checklist

- [ ] Research how `AddLogging()` detection works in service-extractor.cs and interceptor-emitter.cs
- [ ] Design generated code pattern for HttpClient management (IHttpClientFactory or static factory)
- [ ] Add AddHttpClient detection in service-extractor.cs
- [ ] Update NURU052 diagnostic handling to not warn for supported extension methods
- [ ] Implement HttpClientFactory generation in interceptor-emitter.cs
- [ ] Handle basic AddHttpClient() registration
- [ ] Handle named clients: AddHttpClient("name", c => ...)
- [ ] Handle typed clients: AddHttpClient<IService, Implementation>()
- [ ] Add unit tests for AddHttpClient scenarios
- [ ] Add integration test verifying HTTP calls work without MS DI
- [ ] Update documentation (glossary.md) to show AddHttpClient works with source-gen DI

## Notes

**Current State:**
- `AddLogging()` → detected → generates static `ILoggerFactory` field at compile time (AOT-optimized)
- `AddHttpClient()` → detected → emits NURU052 warning (tells users to use `UseMicrosoftDependencyInjection()`)

**Key Files:**
- `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs` - Detects extension method calls
- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - Emits generated DI code
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.service.cs` - NURU052 definition

**Design Considerations:**
- Should generate something equivalent to IHttpClientFactory but AOT-compatible
- Need to handle HttpClient lifetime (connection pooling) properly
- Support configuration lambdas: `AddHttpClient(c => { c.BaseAddress = ...; })`
- May need to generate typed client classes for injection

**Related:**
- Task 422 is blocked on this implementation
- See fluent-runtime-di.cs sample for AddLogging() usage pattern
