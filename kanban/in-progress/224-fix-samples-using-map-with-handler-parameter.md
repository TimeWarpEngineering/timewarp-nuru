# Fix samples using Map with handler parameter

## Description

The `calc-mixed.cs` sample uses `Map<T>(pattern: "...", handler: ...)` syntax but the `Map<T>` overload no longer has a `handler` named parameter. Update to use the current API.

Error: `CS1739: The best overload for 'Map' does not have a parameter named 'handler'`

## Checklist

- [ ] samples/calculator/calc-mixed.cs (handler errors)

## Notes

- Discovered by `runfiles/verify-samples.cs` (task 221)
- This sample mixes delegate handlers with Mediator commands
