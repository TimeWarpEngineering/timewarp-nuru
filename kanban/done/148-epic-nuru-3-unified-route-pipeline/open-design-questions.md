# Open Design Questions

All design questions have been resolved. See decisions below.

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
// Single [NuruRouteAlias] attribute with params (not multiple attributes)
[NuruRoute("exit|Exit the application")]
[NuruRouteAlias("quit", "q")]
public sealed class ExitCommand : IRequest<Unit> { }
```

**Help Output:**
```
exit, quit, q                           Exit the application
```

**What's Removed:**
- ❌ `MapMultiple` — no longer needed
- ❌ `MapAliases` — no longer needed
- ❌ Multiple `[NuruRouteAlias]` attributes — single attribute with `params` handles all

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

---

### 3. Hidden/Deprecated Attributes ✅

**Decision:** Use separate attributes, not properties on `[NuruRoute]`.

```csharp
[NuruRoute("secret")]
[Hidden]
public sealed class SecretCommand : IRequest<Unit> { }

[NuruRoute("old")]
[Deprecated("Use 'new' instead")]
public sealed class OldCommand : IRequest<Unit> { }
```

**Rationale:**
- **Single responsibility** — Each attribute does one thing
- **Discoverable** — Shows up separately in IntelliSense
- **Reusable** — Can potentially apply to other elements (methods, groups)
- **Explicit** — Clear what each attribute means without reading Route's documentation
- **Composable** — Easy to combine: `[NuruRoute][Hidden][Deprecated]`

**Attribute Definitions:**
```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class HiddenAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class DeprecatedAttribute : Attribute
{
    public string Message { get; }
    public DeprecatedAttribute(string message) => Message = message;
}
```

**Fluent API equivalents:**
```csharp
app.Map("secret", handler).Hidden();
app.Map("old", handler).Deprecated("Use 'new' instead");
```

---

### 5. String Return Value Semantics ✅

**Decision:** Option A — keep current auto-print behavior via `ResponseDisplay.Write()`.

**Current behavior (preserved):**
```csharp
// ResponseDisplay.Write() handles return values:
// - Unit → no output
// - string/primitives → WriteLine directly
// - Complex objects with custom ToString → use ToString
// - Complex objects without custom ToString → serialize to JSON

app.Map("whoami", () => "steve");  // prints "steve"
app.Map("config", () => new Config { Env = "prod" });  // prints {"Env":"prod"}
```

**Both explicit writes AND return values are allowed:**
```csharp
// Multi-step operation with progress output AND structured result
public ValueTask<DeployResult> Handle(DeployCommand request, CancellationToken ct)
{
  Console.WriteLine("Step 1: Validating configuration...");
  // ...
  Console.WriteLine("Step 2: Connecting to server...");
  // ...
  Console.WriteLine("Step 3: Deploying...");
  // ...
  return new ValueTask<DeployResult>(new DeployResult 
  { 
    Status = "success", 
    Version = "1.2.3", 
    Duration = "12.5s" 
  });
}
// Output:
// Step 1: Validating configuration...
// Step 2: Connecting to server...
// Step 3: Deploying...
// {"Status":"success","Version":"1.2.3","Duration":"12.5s"}
```

**Rationale:**
- **Preserves current behavior** — no breaking change
- **Multi-step operations** — progress output during execution + structured result at end is a valid pattern
- **User is in control** — they choose when to write explicitly and what to return
- **Clear mental model** — explicit writes happen immediately; return value displayed after handler completes

**Generated Command/Handler mapping:**
| Delegate Return | Generated Command | Output Behavior |
|-----------------|-------------------|-----------------|
| `void` | `IRequest<Unit>` | No auto-output |
| `T` | `IRequest<T>` | Auto-prints result via `ResponseDisplay` |
| `Task` | `IRequest<Unit>` | No auto-output |
| `Task<T>` | `IRequest<T>` | Auto-prints result via `ResponseDisplay`

---

### 4. CancellationToken Availability ✅

**Decision:** Explicit — user adds `CancellationToken` as a delegate parameter if they need it.

```csharp
// User who needs cancellation — adds parameter explicitly
app.Map("long-task", async (CancellationToken ct) => 
{
    await Task.Delay(1000, ct);
});

// User who doesn't need cancellation — no parameter needed
app.Map("quick-task", () => Console.WriteLine("Done"));
```

**Rationale:**
- **Explicit is better than implicit** — Cancellation-aware code is obvious at a glance
- **No unused parameters** — Users who don't need cancellation don't have to deal with it
- **Generator handles detection** — Source generator sees `CancellationToken` parameter and passes it through from the pipeline
- **Consistent with ASP.NET Minimal APIs** — Same pattern used there

**How it works:**
1. User includes `CancellationToken` parameter in delegate (or not)
2. Source generator detects its presence
3. Generated handler receives `CancellationToken` from mediator pipeline (always available)
4. If user requested it, generator passes it to the delegate body; otherwise ignores it

**Generated code example:**
```csharp
// User writes:
app.Map("long-task", async (string name, CancellationToken ct) => 
{
    await Task.Delay(1000, ct);
    Console.WriteLine($"Hello {name}");
});

// Generator emits:
public sealed class LongTask_CommandHandler : IRequestHandler<LongTask_Command, Unit>
{
    public async Task<Unit> Handle(LongTask_Command command, CancellationToken cancellationToken)
    {
        // CancellationToken passed through because user requested it
        await Task.Delay(1000, cancellationToken);
        Console.WriteLine($"Hello {command.Name}");
        return Unit.Value;
    }
}
```
