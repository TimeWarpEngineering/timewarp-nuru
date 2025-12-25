# Add NURU_H005 Analyzer for Handler Parameter Name Validation

## Description

Add compile-time validation that handler parameter names match route segment names. This ensures the generated code compiles correctly, since the V2 generator uses pattern matching variables that must align with handler parameter names.

## Parent

#273 V2 Generator Design Issue: Lambda Body Capture for Delegate Handlers

## Background

The V2 generator captures lambda bodies and emits them as local functions. Route parameters are extracted via pattern matching:

```csharp
// Route: "greet {name}"
if (args is ["greet", var name])
{
  string __handler_0(string name) => $"Hello {name}!";
  // ...
}
```

If the handler parameter is named differently (e.g., `n` instead of `name`), the generated code won't compile because `name` is the pattern variable but `n` is expected in the lambda body.

## Examples

**Valid - parameter matches segment:**
```csharp
.Map("greet {name}")
  .WithHandler((string name) => $"Hello {name}!")  // OK
```

**Invalid - parameter doesn't match:**
```csharp
.Map("greet {name}")
  .WithHandler((string n) => $"Hello {n}!")  // ERROR: 'n' doesn't match 'name'
```

## Diagnostic Definition

| Property | Value |
| -------- | ----- |
| ID | `NURU_H005` |
| Title | Handler parameter name doesn't match route segment |
| Message | Handler parameter '{0}' doesn't match any route segment. Available segments: {1} |
| Category | Handler.Validation |
| Severity | Error |
| Enabled | true |

## Checklist

- [ ] Add `NURU_H005` diagnostic descriptor to `diagnostic-descriptors.handler.cs`
- [ ] Add validation logic to `NuruHandlerAnalyzer`
  - Extract route segment names from `Map()` call
  - Extract handler parameter names from lambda/method
  - Report error for any handler parameter not in route segments
- [ ] Update `AnalyzerReleases.Unshipped.md`
- [ ] Add test cases for the analyzer

## Files to Modify

| File | Changes |
| ---- | ------- |
| `analyzers/diagnostics/diagnostic-descriptors.handler.cs` | Add `NURU_H005` descriptor |
| `analyzers/nuru-handler-analyzer.cs` | Add parameter name validation |
| `AnalyzerReleases.Unshipped.md` | Document `NURU_H005` |

## Notes

The diagnostic descriptor was already added in task #273, but the validation logic in `NuruHandlerAnalyzer` was not implemented.

### Related Diagnostics

| Diagnostic | Severity | Purpose |
| ---------- | -------- | ------- |
| `NURU_H001` | Error | Instance method handlers not supported |
| `NURU_H002` | Warning | Closure detected in handler |
| `NURU_H003` | Error | Unsupported handler expression type |
| `NURU_H004` | Warning | Private method handler not accessible |
| `NURU_H005` | Error | Handler parameter name doesn't match route segment (THIS TASK) |
