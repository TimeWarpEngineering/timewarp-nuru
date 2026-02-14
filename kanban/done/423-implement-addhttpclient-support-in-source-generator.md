# Implement AddHttpClient support in source generator

## Status: DONE

## Description

Extend Nuru's source generator to detect and properly handle `AddHttpClient()` extension method calls, similar to how `AddLogging()` is already handled. This allows users to use idiomatic .NET HTTP client patterns without requiring `UseMicrosoftDependencyInjection()`.

## Checklist

### Extraction/Validation:

- [x] Research how `AddLogging()` detection works in service-extractor.cs and interceptor-emitter.cs
- [x] Design generated code pattern for HttpClient management
- [x] Add HttpClientConfiguration record to service-extraction-result.cs
- [x] Add ExtractHttpClientConfigurations() to service-extractor.cs
- [x] Add HttpClientConfigurations to IR model (iir-app-builder.cs, ir-app-builder.cs)
- [x] Update dsl-interpreter.cs to extract HttpClient configs (call separate `ExtractHttpClientConfigurations()`)
- [x] Add HttpClientConfigurations to AppModel (app-model.cs) with `HasHttpClients` property
- [x] Update service-validator.cs to whitelist AddHttpClient (no NURU050/NURU052 errors)

### Code Generation:

- [x] Implement `EmitHttpClientFields()` in interceptor-emitter.cs (static HttpClient fields + factory methods)
- [x] Add `GetHttpClientFieldName()` helper in interceptor-emitter.cs
- [x] Update handler-invoker-emitter.cs `ResolveServiceForCommand()` to resolve HttpClient-registered services
- [x] Thread `httpClientConfigurations` through route-matcher-emitter.cs call chain
- [x] Handle typed clients: `AddHttpClient<IService, Implementation>()`
- [x] Handle configuration lambdas applied to static HttpClient fields
- [x] Test with sample 15-httpclient — builds and runs successfully
- [x] 1067/1067 CI tests pass, 0 regressions

### Not Implemented (future work):

- [ ] Named clients: `AddHttpClient("name", c => ...)`
- [ ] `ServiceResolverEmitter` support (delegate handler path)
- [ ] Dedicated unit tests for AddHttpClient code generation scenarios

## Implementation Summary

Followed the exact same pattern as AddLogging:

1. **Data pipeline**: `ServiceExtractor.ExtractHttpClientConfigurations()` → `IrAppBuilder.AddHttpClientConfiguration()` → `AppModel.HttpClientConfigurations`
2. **Static field emission**: `InterceptorEmitter.EmitHttpClientFields()` generates `private static readonly HttpClient __httpClient_X = Create___httpClient_X();` with factory methods applying config lambdas
3. **Service resolution**: `HandlerInvokerEmitter.ResolveServiceForCommand()` resolves `new TImplementation(httpClientField)` for services registered via `AddHttpClient<TService, TImpl>()`

### Key Insight

`ServiceExtractor.Extract()` always returns empty `HttpClientConfigurations` — the separate `ExtractHttpClientConfigurations()` method must be called independently in `dsl-interpreter.cs`, matching the pattern of `ExtractLoggingConfiguration()`.

### Generated Code Pattern

```csharp
// Static field + factory
private static readonly global::System.Net.Http.HttpClient __httpClient_IOpenMeteoService = Create___httpClient_IOpenMeteoService();
private static global::System.Net.Http.HttpClient Create___httpClient_IOpenMeteoService()
{
  var client = new global::System.Net.Http.HttpClient();
  client.Timeout = TimeSpan.FromSeconds(30);
  return client;
}

// Handler resolution
global::HttpClientSample.Services.IOpenMeteoService __openMeteo =
  new global::HttpClientSample.Services.OpenMeteoService(__httpClient_IOpenMeteoService);
```

## Files Modified

| File | Change |
|------|--------|
| `app-model.cs` | Added `HttpClientConfigurations` parameter + `HasHttpClients` property |
| `ir-app-builder.cs` | Pass `HttpClientConfigurations` to `FinalizeModel()` |
| `dsl-interpreter.cs` | Call `ExtractHttpClientConfigurations()` and push into IR builder |
| `service-validator.cs` | Whitelist "AddHttpClient" + add HttpClient services to registered set |
| `interceptor-emitter.cs` | `EmitHttpClientFields()` + `GetHttpClientFieldName()` + call in `Emit()` |
| `handler-invoker-emitter.cs` | HttpClient resolution in `ResolveServiceForCommand()` |
| `route-matcher-emitter.cs` | Thread `httpClientConfigurations` through call chain |

## Related

- Task 422 (HttpClient sample) - unblocked by this task
