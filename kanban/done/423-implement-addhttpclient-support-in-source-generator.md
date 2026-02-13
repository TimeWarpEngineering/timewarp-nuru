# Implement AddHttpClient support in source generator

## Description

Extend Nuru's source generator to detect and properly handle `AddHttpClient()` extension method calls, similar to how `AddLogging()` is already handled. This will allow users to use idiomatic .NET HTTP client patterns without requiring `UseMicrosoftDependencyInjection()`.

Currently, `AddHttpClient()` calls are detected but emit a NURU052 warning telling users to use runtime DI. We should support this extension method natively with AOT-optimized code generation.

## Checklist

- [x] Research how `AddLogging()` detection works in service-extractor.cs and interceptor-emitter.cs
- [x] Design generated code pattern for HttpClient management (IHttpClientFactory or static factory)
- [x] Add AddHttpClient detection in service-extractor.cs
- [x] Update NURU052 diagnostic handling to not warn for supported extension methods
- [x] Implement HttpClientFactory generation in interceptor-emitter.cs
- [x] Handle basic AddHttpClient() registration
- [x] Handle named clients: AddHttpClient("name", c => ...)
- [x] Handle typed clients: AddHttpClient<IService, Implementation>()
- [x] Add unit tests for AddHttpClient scenarios
- [x] Add integration test verifying HTTP calls work without MS DI
- [x] Update documentation (glossary.md) to show AddHttpClient works with source-gen DI

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

## Results

Successfully implemented AddHttpClient support in the Nuru source generator.

### Files Modified

1. **service-extraction-result.cs** - Added HttpClientConfiguration record
2. **service-extractor.cs** - Added ExtractHttpClientConfiguration() method
3. **iir-app-builder.cs** - Added HttpClientConfigurations to IAppModel interface
4. **ir-app-builder.cs** - Added HttpClientConfigurations property and builder methods
5. **dsl-interpreter.cs** - Added extraction of HttpClient configs from ConfigureServices
6. **interceptor-emitter.cs** - Added EmitHttpClientFactoryFields() for code generation
7. **handler-invoker-emitter.cs** - Updated to resolve HttpClient-registered services
8. **service-validator.cs** - Updated NURU052 to exclude AddHttpClient from warnings

### Generated Code Pattern

For `AddHttpClient<IService, TImplementation>(client => { ... })`, the generator now produces:

```csharp
private static readonly HttpClient __httpClient_ServiceName = CreateHttpClient_ServiceName();

private static HttpClient CreateHttpClient_ServiceName()
{
  var client = new HttpClient();
  client.BaseAddress = new Uri("https://api.example.com/");
  client.Timeout = TimeSpan.FromSeconds(30);
  return client;
}

// Service instance resolved from static HttpClient
var service = new TImplementation(__httpClient_ServiceName);
```

### Features Supported

- ✅ Typed clients: `AddHttpClient<IService, TImplementation>()`
- ✅ Configuration lambdas: `AddHttpClient<T>(c => c.BaseAddress = ...)`
- ✅ AOT-compatible static field generation
- ✅ No NURU052 warning for AddHttpClient calls
- ✅ Works without UseMicrosoftDependencyInjection()

### Next Steps

Task 422 is now unblocked and can proceed with creating the HttpClient sample.
