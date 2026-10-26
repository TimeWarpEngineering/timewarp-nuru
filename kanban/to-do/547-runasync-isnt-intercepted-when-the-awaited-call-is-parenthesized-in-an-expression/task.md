# RunAsync isn't intercepted when the awaited call is parenthesized in an expression

## Description

The generator does not intercept `app.RunAsync(...)` when the awaited call is wrapped in parentheses and used as an expression, for example:

```csharp
(await app.RunAsync(["work"])).ShouldBe(0);
```

It compiles, then fails at runtime with "RunAsync was not intercepted". Assigning to a local first works:

```csharp
int exitCode = await app.RunAsync(["work"]);
exitCode.ShouldBe(0);
```

Both forms are ordinary C#, so the generator should intercept both. Found by the 544 implementer while writing `generator-70-per-app-logger-factory.cs`, which uses the local-variable form as a workaround.

## Checklist

- [ ] Find where the generator locates `RunAsync` call sites and why a parenthesized `await` (or other expression contexts) is missed; match by the invocation's method symbol, not by the parent syntax shape
- [ ] Check other contexts too: argument to a method, `return await app.RunAsync(...)`, ternary, `.ConfigureAwait(false)`, expression-bodied member
- [ ] If some context truly can't be intercepted, report a compile-time diagnostic instead of a runtime failure
- [ ] Regression test covering each context

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
