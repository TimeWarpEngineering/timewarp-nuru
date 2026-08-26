# Decompile and Lower Opaque AddX Into Source-Gen DI

## Parent

Epic #391: Full DI Support - Source-Gen and Runtime Options (Phase 4)

Board slug (`395-implement-execute-and-inspect-for-extension-methods`) is historical.
**Do not implement execute-and-inspect.** This kitchen supersedes that sketch.

## Description

Source-gen DI only sees `AddSingleton` / `AddScoped` / `AddTransient` in the user's
`ConfigureServices` body (plus special-cases for `AddLogging` / `AddHttpClient`).
A call like `services.AddMyLibraryServices()` is opaque: **NURU052**, and **NURU050**
if a handler needs a type that was never seen. `.UseMicrosoftDependencyInjection()`
is the ripcord — it already *runs* those methods on a real `IServiceCollection`.

Phase 4 inlines **pure, lowerable** library `AddX` methods into the existing
source-gen model so the container is not required for that graph.

This does **not** eliminate DI as a concept (handlers still take `IFoo`). It
eliminates **Microsoft's container** for graphs we can see and construct. The
ripcord stays. We will not make it obsolete.

## Decision (2026-08-27)

Two products were mixed in earlier drafts. Only **B** is this task.

| | A — Replay | B — Lower (this task) |
|---|---|---|
| Mechanism | Decompile `AddX`, emit the same `IServiceCollection` calls, `BuildServiceProvider()` | Decompile `AddX`, treat the body as a registration script, emit `new Impl(...)` / `Lazy<T>` via Phase 3 |
| Container | Still present | Absent for that graph |
| vs hatch | Worse hatch — user's `ConfigureServices` already does this under `.UseMicrosoftDependencyInjection()` | The actual capability expansion |

**Never execute** user or package code in the generator (`Assembly.LoadFrom` +
`MethodInfo.Invoke`). No compile-time `ServiceCollection` runner.

## Approach

1. Call is `AddSingleton` / `AddScoped` / `AddTransient` in user source → already handled.
2. Call is an extension **in this compilation** → follow syntax (`DeclaringSyntaxReferences`).
   No decompiler. (Method groups in-project already work; extend to in-project `AddX`.)
3. Call is in a **referenced assembly** → decompile with `ICSharpCode.Decompiler` (ILSpy as a library).
4. **Cannot decompile** → NURU052 on that invocation. That *is* the flag. User does not inspect the method.
5. Recurse **same-assembly** helpers that are also pure registration scripts.
6. **Purity (fail closed):** every effect is an `IServiceCollection` call we can **lower**.
   Anything else (including builder returns we do not lower) → NURU052 on the **whole**
   user-facing `AddX` invocation. Never partial-lower and silence NURU052.
7. Merge lowered `ServiceDefinition`s into the existing model. Phase 3 ctor resolution
   emits `new`. Do **not** emit `IServiceCollection` calls.

### Implementation vs ref assemblies

Decompile the **implementation** asset (`lib/`), not the NuGet `ref/` compile stub.
`ref/` bodies are often `throw null`; a "successful" decompile of that is garbage
and would ripcord every modern package. If a real method body cannot be found,
same flag as cannot-decompile.

Cache decompile/classify results by **(assembly MVID, method token)** — agent
`dotnet build` loops, not an IDE story.

## Purity test

If `AddX` does `services.AddSingleton<IFoo, Foo>(); Cache.Enabled = true;` and we
kept only collection calls, we would drop a side effect. So the rule is the
opposite: **if anything is not a call on `services` (or a helper that is also
only that), ripcord.**

"All side effects live in the collection" is false. The collection stores
`ServiceDescriptor`s, not:

- Locals/parameters a factory closed over (`AddFoo(string conn)` → `sp => new Foo(conn)`).
  Replay without lifting `conn` from the **call site** is a different program.
- Static mutation, file/env I/O, `GetCallingAssembly()`, `GetTypes()` beside the `Add*` calls.
- The **builder** object most serious APIs actually use.

### Builder hole (do not paper over)

```csharp
services.AddHttpClient<IGit, GitClient>(c => c.BaseAddress = ...)
        .AddHttpMessageHandler<RetryHandler>();
```

`AddHttpClient` hits `IServiceCollection` once; the rest is `IHttpClientBuilder`.
Same shape: `AddHealthChecks()`, `AddAuthentication()`, `AddOpenTelemetry()`,
`AddOptions<T>().Bind(...)`. Nuru already special-cases `AddLogging` / `AddHttpClient`
by scraping lambdas — those are not "just collection methods."

**v1:** receiver is `IServiceCollection` (plus same-assembly helpers that only
talk to it). Anything that returns a builder we do not lower → NURU052.

## Lowering catalog (whitelist primitives, not user method names)

Do not whitelist `AddMyLibraryServices`. Whitelist **IServiceCollection APIs we can lower**:

| Call | v1 lower to `new`? |
|------|--------------------|
| `Add{Lifetime}<T>()` / `<TService, TImpl>()` / `(Type, Type)` **closed, public** | Yes (existing path) |
| `TryAdd*` | Yes, if replayed **in order** against the accumulated model (user lines + inlined library calls) |
| `Configure<T>(section)` | Only as the existing `IOptions<T>` path, not as a container |
| Factory `sp => new Foo(sp.GetRequiredService<Bar>())` | **Not v1.** Today NURU053. Later: that is Phase 3 with extra steps |
| Factory that closes over args/config | **Not v1.** Only if call-site arguments are lifted into generated init |
| Open generic `IRepo<>` | **Not v1.** Use-site closing is a later pass, not `new Repo<>` |
| `AddHostedService` | **Not v1.** `new` does not `StartAsync`. Hatch or ignore only with an explicit decision |
| Builder APIs / full `AddHttpClient` stack | **Not v1.** Existing special-cases stay; no new ones in this task |
| `GetTypes()` / assembly scanning | Hatch |

v1 ignore factories inside decompiled `AddX` (ripcord the invocation). Do not
splice arbitrary decompiled C# into `NuruGenerated.g.cs`.

## Hard ceiling: `internal` (NURU054)

Library `AddX` runs **inside the library assembly**. It can `new InternalImpl()`.
Generated code in the app cannot. Emitting `services.AddScoped<IFoo, InternalImpl>()`
in `NuruGenerated.g.cs` fails the same way.

Decompile lets us *see* `InternalImpl`. It does not let us construct it.
EF, FreeSql, `IHttpClientFactory`, much of Microsoft.Extensions: the **only**
working path is invoking the original extension method at runtime (the hatch).

Public `AddSingleton<IFoo, Foo>` wrappers (app-level and small libraries): in scope.
Framework `AddX` that hides impl types: hatch, no shame.

## Diagnostics (ripcord must stay loud)

- **NURU052** — cannot decompile, purity fail, unlowerable call, builder, scanning.
  The whole user-facing `AddX` is opaque. Do not silence 052 after a partial lower.
- **NURU050** — handler needs a type we never registered (unchanged).
- **NURU053** — factory (unchanged in v1).
- **NURU054** — internal impl (unchanged).

Tighten 052 message if needed: Nuru did not instantiate anything this method
registered. Call `.UseMicrosoftDependencyInjection()` or register the injected
types with `AddSingleton<,>` yourself.

## Success criteria

- Opaque `AddX` is decompiled (implementation DLL).
- Pure lifetime-`Add*` graphs of **public closed types** are inlined into source-gen DI.
- Everything else, including failed decompile, is NURU052 on that invocation.
- Factories, `Configure`/HttpClient/hosted/builders stay on the ripcord (except
  existing `AddLogging` / `AddHttpClient` special-cases).
- No `ServiceProvider` is introduced for lowered graphs.
- `.UseMicrosoftDependencyInjection()` remains the hatch; this task does not remove it.
- AOT path for source-gen DI is unchanged.

**Not a success criterion:** "extension methods work" in general, "eliminate DI",
or CLI startup microseconds (hatch is +2–10ms; the reason to maximize lowering is
AOT / trim / no container in the binary).

Plausible outcome: TimeWarp console apps we write almost never need the hatch.
Not plausible: Nuru apps never ship a container (`AddDbContext`, a real
`AddHttpClient` pipeline, vendor SDKs).

## Requirements

### Same-compilation

- [ ] Follow in-project `AddX` via syntax (no decompiler)

### Decompile

- [ ] `ICSharpCode.Decompiler` (or equivalent ILSpy library) on referenced methods
- [ ] Resolve **lib/** implementation assembly, not `ref/`
- [ ] Cannot-decompile / no real body → NURU052
- [ ] Cache by (MVID, method token)

### Purity + lower

- [ ] Recurse same-assembly helpers
- [ ] Fail closed: any non-lowerable effect aborts the whole user-facing `AddX`
- [ ] Lower closed public `Add{Lifetime}` type mappings into `ServiceDefinition`
- [ ] `TryAdd*` in order against the accumulated model
- [ ] Merge into existing Phase 3 emit (`new` / `Lazy<T>`)
- [ ] Do not emit `IServiceCollection` calls; do not `Invoke` at compile time

### Diagnostics + tests

- [ ] NURU052 on failed decompile, purity fail, builder, unlowerable API
- [ ] NURU054 still fires for internal impls (do not attempt to emit them)
- [ ] Simple public `AddX` wrapping `AddSingleton<IFoo, Foo>`
- [ ] Nested same-assembly helpers (A → B → `AddScoped`)
- [ ] Mix of inline `AddSingleton` and lowerable `AddX` (order / `TryAdd`)
- [ ] Generic `AddX<T>()` that registers `T` as public closed type
- [ ] Cannot-decompile / `ref/`-only body → 052
- [ ] Non-collection side effect in `AddX` → 052 (not a partial lower)
- [ ] Builder-returning `AddX` → 052
- [ ] Internal impl inside `AddX` → 054 or 052 (hatch), never `new Internal`
- [ ] Factory inside `AddX` → 052/053, not lowered in v1

## Out of scope (v1)

- Execute-and-inspect / compile-time `ServiceCollection`
- Replaying `IServiceCollection` method calls into generated startup
- Factory lowering (including GetRequiredService shape)
- New builder special-cases beyond existing logging/HttpClient
- Open generics, hosted services, `GetTypes()` scanning
- `[NuruServiceRegistration]` library-author tax
- Removing `.UseMicrosoftDependencyInjection()`
- Epic #391 Phase 5 (per-app `ServiceProvider` isolation)
- ServiceGen extraction (task 444)

## Notes

- Combined with Phase 3, this covers most **app-authored** `AddX` sugar.
- GitHub public search (2026-08-27, overlapping file counts): `this IServiceCollection`
  plus `AddScoped` ~194k, `AddSingleton` ~125k, `AddTransient` ~62k; vs `AddHttpClient`
  ~27k, `AddHostedService` ~18k, `GetTypes` ~9k. Lifetime `Add*` is the mass;
  builders/scanning are the tail — hatch.
- Epic #391 still says "Execute ServiceCollection at compile time" on Phase 4;
  that line is stale after this rewrite (fold in when 395 lands or in the epic
  close-out, do not sneak a 391 edit through this branch unless asked).

## Session

- Created: 2026-03-23 — execute-and-inspect sketch (never implemented)
- Rewrite: grok (2026-08-27) — decompile + purity + lower; execute/replay rejected
