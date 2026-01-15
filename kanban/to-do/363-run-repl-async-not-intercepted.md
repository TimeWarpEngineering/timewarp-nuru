# RunReplAsync is not intercepted by source generator

## Description

`RunReplAsync()` is a public API method on `NuruCoreApp` that is supposed to be intercepted by the source generator, but it is **never intercepted** - not even when called inline.

### Fails even inline:
```csharp
NuruCoreApp app = NuruApp.CreateBuilder([])
  .Map("hello")
    .WithHandler(() => "Hello!")
    .AsQuery()
    .Done()
  .AddRepl()
  .Build();

await app.RunReplAsync(); // NOT intercepted - throws InvalidOperationException
```

### Works (using --interactive flag):
```csharp
await app.RunAsync(["--interactive"]); // Works - intercepted
```

## Root Cause

The source generator emits an interceptor for `RunAsync()` that handles `["--interactive"]` args, but it does NOT emit an interceptor for `RunReplAsync()` calls.

## Fix Required

The generator must emit `[InterceptsLocation]` for ALL `RunReplAsync()` calls on `NuruCoreApp`, just like it does for `RunAsync()`.

## Checklist

- [ ] Add `RunReplAsync` invocation tracking to generator (like `RunAsync`)
- [ ] Emit interceptor for `RunReplAsync` calls
- [ ] Verify `repl-36-run-repl-async-inline.cs` test passes
- [ ] Verify tests with Setup pattern work after fix

## Impact

- Blocks Task #362 - 122 REPL tests fail
- Public API method doesn't work as documented
