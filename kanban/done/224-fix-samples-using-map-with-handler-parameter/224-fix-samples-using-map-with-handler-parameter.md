# Fix samples using Map with handler parameter

## Description

The `calc-mixed.cs` sample uses `Map<T>(pattern: "...", handler: ...)` syntax but the `Map<T>` overload no longer has a `handler` named parameter. Update to use the current API.

Error: `CS1739: The best overload for 'Map' does not have a parameter named 'handler'`

## Checklist

- [x] samples/calculator/calc-mixed.cs (handler errors) - resolved as part of task 223

## Results

This task was already resolved when task 223 was completed. The `calc-mixed.cs` sample now uses the fluent API (`.Map(...).WithHandler(...).WithDescription(...)`) and compiles successfully.

Verified by running `runfiles/verify-samples.cs` which shows `calc-mixed.cs` passing compilation.

## Notes

- Discovered by `runfiles/verify-samples.cs` (task 221)
- This sample mixes delegate handlers with Mediator commands
