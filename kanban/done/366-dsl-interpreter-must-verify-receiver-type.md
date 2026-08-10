# DSL Interpreter Must Verify Receiver Type for All Method Calls

## Description

The fluent DSL interpreter incorrectly interprets method calls on unrelated types as if they were builder methods. For example, `CustomKeyBindingProfile.WithName()` is being interpreted as `NuruCoreAppBuilder.WithName()`, causing the generator to fail with:

```
WithName() must be called on an app builder
```

This is a semantic analysis bug - the interpreter pattern-matches on method names without verifying the receiver type.

## Root Cause

The interpreter uses syntactic pattern matching to find DSL method calls but doesn't use the Roslyn semantic model to verify that the method is actually being called on `NuruCoreAppBuilder` or its derivatives.

## Affected Methods

All DSL methods need receiver type verification:
- `WithName()`
- `WithVersion()`
- `WithDescription()`
- `AddRoute()`
- `AddRepl()`
- `UseTerminal()`
- `Build()`
- And all other fluent API methods

## Failing Tests

From `repl-24-custom-key-bindings.cs`:
- `Should_use_custom_profile_from_ReplOptions` - Line 371: `CustomKeyBindingProfile.WithName("TestCustomProfile")`
- `Custom_profile_should_take_precedence_over_profile_name` - Line 397: `CustomKeyBindingProfile.WithName("CustomTakesPrecedence")`
- `Custom_profile_based_on_emacs_should_work` - Line 451: `CustomKeyBindingProfile.WithName("EmacsCustom")`

## Expected Behavior

The interpreter should:
1. Use semantic model to get the type of the receiver expression
2. Only interpret method calls where receiver type is `NuruCoreAppBuilder` or derives from it
3. Ignore method calls on unrelated types even if they have the same name

## Example Fix Pattern

```csharp
// In interpreter, before interpreting a method call:
ITypeSymbol? receiverType = semanticModel.GetTypeInfo(invocation.Expression).Type;
if (receiverType?.Name != "NuruCoreAppBuilder" &&
    !receiverType?.AllInterfaces.Any(i => i.Name == "INuruAppBuilder") == true)
{
    // Not a builder method call - skip
    return;
}
```

## Related

- Similar to #364 (cross-method field tracking) which also improved semantic analysis
- The semantic model is already available in the generator context

## Priority

Medium - Tests are failing but functionality works when patterns aren't ambiguous

## Results

Closed 2026-08-10 board audit: IsDslBuilderMethod() receiver-type verification added (commit 075692c8) and used at every dispatch site in dsl-interpreter.cs.

### How to validate
Smoke: grep -n "IsDslBuilderMethod" source/timewarp-nuru-analyzers/**/dsl-interpreter.cs — Expect: guard at dispatch sites; dotnet run tests/timewarp-nuru-tests/repl/repl-24-custom-key-bindings.cs — Expect: 26/26 pass.
