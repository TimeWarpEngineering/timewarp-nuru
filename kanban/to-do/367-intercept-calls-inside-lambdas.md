# Interceptor Cannot Intercept Calls Inside Lambdas/Delegates

## Description

The source generator cannot intercept method calls that occur inside lambda expressions or delegates. The `[InterceptsLocation]` attribute requires direct source code locations, but lambda bodies are compiled to separate closure methods.

## Example

```csharp
// This works - direct call, intercepted
await app.RunAsync(["test"]);

// This does NOT work - call inside lambda, NOT intercepted
await Should.ThrowAsync<Exception>(
  async () => await app.RunAsync(["test"]));  // Generator sees 0 intercept sites
```

## Root Cause

Source generators with interceptors analyze syntax at compile-time. When a call like `app.RunAsync()` is inside a lambda:
1. The lambda is compiled to a separate closure class/method
2. The `[InterceptsLocation]` points to source locations in the original method
3. The generator's syntax analysis doesn't descend into lambda bodies when counting intercept sites
4. Even if it did, intercepting inside closures is architecturally problematic

## Failing Tests

From `generator-14-options-validation.cs`:
- `Should_throw_when_required_field_empty` - Uses `Should.ThrowAsync(async () => await app.RunAsync(testArgs))`
- `Should_report_multiple_validation_failures` - Uses `Should.ThrowAsync(async () => await app.RunAsync(testArgs))`

From `repl-32-multiline-editing.cs` (9 skipped tests):
- All tests use handler closures to capture state

## Workaround

Use try-catch instead of `Should.ThrowAsync` with lambda:

```csharp
// Instead of:
await Should.ThrowAsync<OptionsValidationException>(
  async () => await app.RunAsync(testArgs));

// Use:
try
{
  await app.RunAsync(testArgs);
  throw new ShouldAssertException("Expected OptionsValidationException but none was thrown");
}
catch (OptionsValidationException ex)
{
  ex.Message.ShouldContain("...");
}
```

## Potential Solutions

1. **Documentation/Test patterns**: Document that `app.RunAsync()` must be called directly, not inside lambdas
2. **Runtime fallback**: If no intercept happens, fall back to runtime route matching (would defeat AOT benefits)
3. **Deeper syntax analysis**: Walk into lambda bodies to find intercept sites (complex, may not solve the interception problem itself)

## Related

- #362 skipped tests mention closures as incompatible with source generator
- This is a fundamental limitation of C# interceptors, not a bug per se

## Priority

Low - Workarounds exist, and this is a language/compiler limitation
