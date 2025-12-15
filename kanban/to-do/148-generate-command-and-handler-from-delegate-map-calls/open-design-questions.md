# Open Design Questions

These are unresolved API design questions that need decisions before implementation.

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

---

### 2. Group Options Syntax ✅

**Decision:** Use fluent builder API, not string parsing.

```csharp
.WithGroupOptions(o => o
    .Flag("debug", "D", "Enable debug mode")
    .Option("log-level", "level", optional: true, description: "Set logging level"))
```

**Rationale:**
- **No magic** — What you see is what you get
- **Full IDE support** — Autocomplete, parameter hints, compile-time checking
- **Self-documenting** — Method names (`Flag`, `Option`) tell you what you're creating
- **Extensible** — Easy to add capabilities later without breaking existing code
- **Descriptions work cleanly** — String parsing gets ugly with descriptions (`"--debug,-D|Enable debug mode --verbose,-v|Verbose"`)
- **Few group options in practice** — Realistically 2-5 global options, so verbosity is minimal
- **No new parsing logic** — Existing parser is for route patterns, not option lists

**What's Removed:**
- ❌ `WithGroupOptions(string optionsPattern)` — No string-based option parsing
- ❌ Reusing route parser for option lists — Different contexts, different APIs

**Fluent Builder API:**
```csharp
public interface IGroupOptionsBuilder
{
    /// <summary>
    /// Add a boolean flag option.
    /// </summary>
    IGroupOptionsBuilder Flag(string longForm, string? shortForm = null, string? description = null);
    
    /// <summary>
    /// Add an option that expects a value.
    /// </summary>
    IGroupOptionsBuilder Option(string longForm, string parameterName, string? shortForm = null, 
        bool optional = false, string? description = null);
}

// Usage:
var docker = builder.MapGroup("docker")
    .WithGroupOptions(o => o
        .Flag("debug", "D", "Enable debug mode")
        .Option("log-level", "level", "l", optional: true, description: "Set logging level"));
```

**Future:** If syntactic sugar is truly needed, a string-based overload could parse into the same builder calls internally. But start explicit.
