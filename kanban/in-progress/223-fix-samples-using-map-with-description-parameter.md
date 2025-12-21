# Fix samples using Map with description parameter

## Description

Several samples use `Map<T>(pattern: "...", description: "...")` syntax but the `Map<T>` overload no longer has a `description` named parameter. Update these samples to use the current API (likely `.WithDescription()` fluent method or correct overload).

Error: `CS1739: The best overload for 'Map' does not have a parameter named 'description'`

## Checklist

- [ ] samples/aspire-host-otel/nuru-client.cs
- [ ] samples/aspire-telemetry/aspire-telemetry.cs
- [ ] samples/calculator/calc-mediator.cs
- [ ] samples/calculator/calc-mixed.cs (description errors)

## Notes

- Discovered by `runfiles/verify-samples.cs` (task 221)
- These samples use the Mediator pattern with `Map<TRequest>()` generic method
