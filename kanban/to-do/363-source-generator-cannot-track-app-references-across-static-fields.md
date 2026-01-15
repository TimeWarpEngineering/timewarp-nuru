# Source generator cannot track app references across static fields

## Description

The source generator fails to intercept `RunAsync()` calls when the app is created in a `Setup()` method and stored in a static field. This is a standard test pattern that should work.

### This pattern SHOULD work but doesn't:
```csharp
private static NuruCoreApp? App;

public static async Task Setup()
{
  App = NuruApp.CreateBuilder([])
    .Map("greet {name}")
      .WithHandler((string name) => $"Hello, {name}!")
      .AsCommand()
      .Done()
    .AddRepl()
    .Build();
}

public static async Task Should_test_something()
{
  await App!.RunAsync(["--interactive"]); // BUG: NOT intercepted
}
```

There is nothing in this code that indicates to a developer it shouldn't work. It's a completely valid pattern.

## Root Cause

The source generator only tracks direct fluent chains where `RunAsync()` is called on the same local variable that received the `.Build()` result. It doesn't track assignments to static fields or instance fields.

## Fix Required

The generator must be enhanced to intercept `RunAsync()`/`RunReplAsync()` calls on `NuruCoreApp` regardless of how the instance was obtained. The routes are already compiled at build time - the interception should work for any call site.

## Checklist

- [ ] Modify generator to emit interceptors for ALL `RunAsync` calls on `NuruCoreApp` type
- [ ] Modify generator to emit interceptors for ALL `RunReplAsync` calls on `NuruCoreApp` type
- [ ] Verify tests with Setup/CleanUp pattern work after fix

## Impact

- Blocks Task #362 - 16 REPL test files cannot be added to CI
- Forces unnatural inline-everything test style
