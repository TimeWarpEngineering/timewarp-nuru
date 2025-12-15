# Open Design Questions

These are unresolved API design questions that need decisions before implementation.

---

## 2. Group Options Syntax

**Is `WithGroupOptions("--debug,-D --log-level {level?}")` the right API?**

Or should it be multiple calls:

```csharp
// Single string (current)
.WithGroupOptions("--debug,-D --log-level {level?}")

// Multiple calls
.WithGroupOption("--debug,-D")
.WithGroupOption("--log-level {level?}")

// Fluent builder
.WithGroupOptions(o => o
    .WithOption("debug", shortForm: "D")
    .WithOption("log-level", parameterName: "level", expectsValue: true, isOptional: true))
```

**Considerations:**
- Single string is concise but requires parsing
- Multiple calls are verbose but explicit
- Fluent builder matches other APIs but is most verbose

---

## 3. Hidden/Deprecated Attributes

**Should `[Hidden]` and `[Deprecated]` be separate attributes or properties on `[Route]`?**

```csharp
// Option A: Separate attributes
[Route("secret")]
[Hidden]
public sealed class SecretCommand : IRequest<Unit> { }

[Route("old")]
[Deprecated("Use 'new' instead")]
public sealed class OldCommand : IRequest<Unit> { }

// Option B: Properties on Route
[Route("secret", Hidden = true)]
public sealed class SecretCommand : IRequest<Unit> { }

[Route("old", DeprecatedMessage = "Use 'new' instead")]
public sealed class OldCommand : IRequest<Unit> { }
```

**Considerations:**
- Separate attributes: More discoverable, can be reused on other elements
- Properties on Route: Fewer attributes to learn, all route metadata in one place

---

## 4. CancellationToken Availability

**Should CancellationToken be implicitly available to all handlers, or explicitly requested as a delegate parameter?**

```csharp
// Option A: Implicit (always available in generated handler)
app.Map("long-task", async () => 
{
    // CancellationToken not accessible in delegate
    await Task.Delay(1000);
});

// Option B: Explicit (user requests it)
app.Map("long-task", async (CancellationToken ct) => 
{
    await Task.Delay(1000, ct);
});
```

**Considerations:**
- Implicit: Simpler for users who don't need it
- Explicit: User controls whether to use it, makes cancellation-aware code obvious
- Generated handler always has access to CancellationToken regardless

**Recommendation:** Explicit — if user wants CancellationToken, they add it as a parameter. Generator detects it and passes it through.

---

## 5. String Return Value Semantics

**Should we support `string` returns for direct output?**

```csharp
// Returns string — what happens?
app.Map("whoami", () => "steve");

// Option A: Print to stdout
// Executes: Console.WriteLine("steve")

// Option B: Return as IRequest<string> result
// Caller handles the result

// Option C: Not supported — must use Console.WriteLine explicitly
app.Map("whoami", () => { Console.WriteLine("steve"); });
```

**Considerations:**
- Option A is convenient for simple cases
- Option B is more flexible but requires handling
- Option C is explicit and consistent with side-effect model

**Current convention:** `IRequest<T>` returns a result, exit code is separate. So `string` return would be a result, not automatic output.

---

## Decisions Made

### 1. Aliases API ✅

**Decision:** Use `Map()` with fluent `.WithAliases()` chain instead of `MapMultiple` or `MapAliases`.

**Rationale:**
- Primary route is explicit in `Map()` call
- Aliases are clearly secondary via `.WithAliases()`
- Consistent with existing fluent pattern (`.WithDescription()`, `.Hidden()`, etc.)
- Aliases share the primary route's description — no duplicate descriptions

**Fluent API:**
```csharp
// Primary route with aliases — all share same description
app.Map("exit|Exit the application", () => Environment.Exit(0))
   .WithAliases("quit", "q");
```

**Attribute API:**
```csharp
// Single [RouteAlias] attribute with params (not multiple attributes)
[Route("exit|Exit the application")]
[RouteAlias("quit", "q")]
public sealed class ExitCommand : IRequest<Unit> { }
```

**Help Output:**
```
exit, quit, q                           Exit the application
```

**What's Removed:**
- ❌ `MapMultiple` — no longer needed
- ❌ `MapAliases` — no longer needed
- ❌ Multiple `[RouteAlias]` attributes — single attribute with `params` handles all

**Attribute Definition:**
```csharp
// Single attribute accepts multiple aliases via params
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RouteAliasAttribute : Attribute
{
    public string[] Aliases { get; }
    public RouteAliasAttribute(params string[] aliases)
    {
        Aliases = aliases;
    }
}
```
