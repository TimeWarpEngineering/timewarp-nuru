# Implement AddHttpClient support in source generator

## Status: IN PROGRESS - INCOMPLETE

## Description

Extend Nuru's source generator to detect and properly handle `AddHttpClient()` extension method calls, similar to how `AddLogging()` is already handled.

**CRITICAL: This task was marked complete prematurely. The code generation portion is NOT implemented.**

## What WAS Completed (Extraction/Validation):

- [x] Add HttpClientConfiguration record to service-extraction-result.cs
- [x] Add ExtractHttpClientConfigurations() to service-extractor.cs
- [x] Add HttpClientConfigurations to IR model (iir-app-builder.cs, ir-app-builder.cs)
- [x] Update dsl-interpreter.cs to extract HttpClient configs
- [x] Add HttpClientConfigurations to AppModel
- [x] Update service-validator.cs to whitelist AddHttpClient (no NURU050/NURU052 errors)

## What's NOT Completed (Code Generation):

- [ ] **IMPLEMENT CODE GENERATION in interceptor-emitter.cs**
  - Currently the analyzer extracts AddHttpClient calls but generates NO HttpClient factory code
  - Generated code shows: `IOpenMeteoService = default! /* ERROR: not registered */`
  - Need to generate static HttpClient fields similar to ILoggerFactory pattern

- [ ] Update handler-invoker-emitter.cs to resolve HttpClient-registered services
  - Currently services from AddHttpClient are not being resolved in generated interceptor code

## Current Problem

When building sample 15-httpclient, the generated code shows:
```csharp
global::HttpClientSample.Services.IOpenMeteoService __openMeteo = default! /* ERROR: Service global::HttpClientSample.Services.IOpenMeteoService not registered */;
```

This proves the extraction works (no NURU050 error), but the code generation doesn't create the HttpClient or service.

## What Needs to Be Done

### 1. Implement EmitHttpClientFactoryFields() in interceptor-emitter.cs
Similar to EmitLoggingFactoryFields(), generate:
- Static HttpClient fields: `private static readonly HttpClient __httpClient_ServiceName`
- Factory methods: `private static HttpClient CreateHttpClient_ServiceName() { ... }`

### 2. Update handler-invoker-emitter.cs 
Resolve services registered via AddHttpClient by:
- Looking up HttpClientConfigurations
- Creating service instances using the generated HttpClient fields

### 3. Test with sample 15-httpclient

## Files Modified So Far

1. `service-extraction-result.cs` - Added HttpClientConfiguration record
2. `service-extractor.cs` - Added extraction logic
3. `iir-app-builder.cs` - Added interface
4. `ir-app-builder.cs` - Added IR builder methods + FinalizeModel
5. `dsl-interpreter.cs` - Added extraction call
6. `app-model.cs` - Added HttpClientConfigurations
7. `service-validator.cs` - Added to whitelist

## Blocked Tasks

- Task 422 (HttpClient sample) - depends on this
