# Implement AddHttpClient support in source generator

## Status: IN PROGRESS - INCOMPLETE

## Description

Extend Nuru's source generator to detect and properly handle `AddHttpClient()` extension method calls, similar to how `AddLogging()` is already handled. This will allow users to use idiomatic .NET HTTP client patterns without requiring `UseMicrosoftDependencyInjection()`.

**CRITICAL: This task was marked complete prematurely. The code generation portion is NOT implemented.**

## Checklist

### Completed (Extraction/Validation):

- [x] Research how `AddLogging()` detection works in service-extractor.cs and interceptor-emitter.cs
- [x] Design generated code pattern for HttpClient management
- [x] Add HttpClientConfiguration record to service-extraction-result.cs
- [x] Add ExtractHttpClientConfigurations() to service-extractor.cs
- [x] Add HttpClientConfigurations to IR model (iir-app-builder.cs, ir-app-builder.cs)
- [x] Update dsl-interpreter.cs to extract HttpClient configs
- [x] Add HttpClientConfigurations to AppModel (app-model.cs)
- [x] Update service-validator.cs to whitelist AddHttpClient (no NURU050/NURU052 errors)

### NOT Completed (Code Generation):

- [ ] **Implement EmitHttpClientFactoryFields() in interceptor-emitter.cs**
  - Currently the analyzer extracts AddHttpClient calls but generates NO HttpClient factory code
  - Generated code shows: `IOpenMeteoService = default! /* ERROR: not registered */`
  - Need to generate static HttpClient fields similar to ILoggerFactory pattern
- [ ] **Update handler-invoker-emitter.cs to resolve HttpClient-registered services**
  - Currently services from AddHttpClient are not being resolved in generated interceptor code
- [ ] Handle basic AddHttpClient() registration
- [ ] Handle named clients: AddHttpClient("name", c => ...)
- [ ] Handle typed clients: AddHttpClient<IService, Implementation>()
- [ ] Add unit tests for AddHttpClient scenarios
- [ ] Add integration test verifying HTTP calls work without MS DI
- [ ] Test with sample 15-httpclient
- [ ] Update documentation

## Current Problem

When building sample 15-httpclient, the generated code shows:
```csharp
global::HttpClientSample.Services.IOpenMeteoService __openMeteo = default! /* ERROR: Service global::HttpClientSample.Services.IOpenMeteoService not registered */;
```

This proves the extraction works (no NURU050 error), but the code generation doesn't create the HttpClient or service.

## Design Considerations

- Should generate something equivalent to IHttpClientFactory but AOT-compatible
- Need to handle HttpClient lifetime (connection pooling) properly
- Support configuration lambdas: `AddHttpClient(c => { c.BaseAddress = ...; })`
- May need to generate typed client classes for injection

### Target Generated Code Pattern

For `AddHttpClient<IService, TImplementation>(client => { ... })`, the generator should produce:

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

## Key Files

- `source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs` - **Needs code generation implementation**
- `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs` - **Needs service resolution update**
- `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs` - Extraction (done)
- `source/timewarp-nuru-analyzers/generators/extractors/service-extraction-result.cs` - Record (done)
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.service.cs` - NURU052 definition

## Files Modified So Far

1. `service-extraction-result.cs` - Added HttpClientConfiguration record
2. `service-extractor.cs` - Added extraction logic
3. `iir-app-builder.cs` - Added interface
4. `ir-app-builder.cs` - Added IR builder methods + FinalizeModel
5. `dsl-interpreter.cs` - Added extraction call
6. `app-model.cs` - Added HttpClientConfigurations
7. `service-validator.cs` - Added to whitelist

## Related

- Task 422 (HttpClient sample) - blocked on this
- See fluent-runtime-di.cs sample for AddLogging() usage pattern
