# Open Design Questions

These are unresolved API design questions that need decisions before implementation.

---

## 1. MapMultiple Naming

**Should `MapMultiple` be renamed?**

Alternatives:
- `MapAliases`
- `MapWithAliases`
- Or rely solely on `[RouteAlias]` attributes (no delegate-based API)?

```csharp
// Current
builder.MapMultiple(["exit", "quit", "q"], () => Environment.Exit(0));

// Alternative
builder.MapAliases(["exit", "quit", "q"], () => Environment.Exit(0));
```

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

## 6. MapMultiple with Different Parameters

**What happens if patterns in `MapMultiple` have different parameters?**

```csharp
// Same parameters — works fine
builder.MapMultiple(["greet {name}", "hello {name}"], (string name) => ...);

// Different parameters — what happens?
builder.MapMultiple(["list", "list {filter?}"], (string? filter) => ...);
```

**Options:**
- Error: All patterns must have identical parameters
- Union: Command has union of all parameters (some optional)
- Subset: Shorter patterns are valid if parameters are a subset

---

## Decisions Made

*(Move questions here once decided)*
