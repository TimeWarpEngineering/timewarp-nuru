# Round 3 — general
**Date:** 2026-10-18
**Scope reviewed:** Fix delta `git diff 572ecdc1..bb2d2b9f -- . ':!kanban'`, focused on generator correctness: `command-construction-emitter.cs` (new), `json-args-emitter.cs`, `behavior-emitter.cs`, `handler-invoker-emitter.cs`, `route-matcher-emitter.cs`, `capabilities-emitter.cs`, `endpoint-extractor.cs`, `operation-endpoint-extractor.cs`, `handler-definition.cs`, `segment-definition.cs`. I checked the claims by compiling and running throwaway apps under `/tmp/r3` (repo `global.json`, project references to the worktree's `timewarp-nuru` and `timewarp-nuru-analyzers` at bb2d2b9f) and by reading the generated `NuruGenerated.g.cs`.

## Summary
All eight round-2 findings in scope are fixed. Every listed case compiles and behaves correctly on argv and on `--json-args`. The delta adds new defects:
- A conditional init-only construction that grows exponentially. It is easy to hit with `{ get; init; } = null!`: 12 such members produce 84k generated lines and a 117 s build.
- A CS9035 regression for `required` members that have a non-copyable initializer.
- CS8031 when an `INuruBehavior` wraps the new "no generated mediator" exit-1 path.
- Option matching in the construction emitter that ignores case, so `--Quiet` and `--quiet` are mixed up again.
- A nullable-enum `= null` initializer that becomes member 0.
- Three positional-record gaps: an initializer that reads a constructor parameter (`= Name`), constructor-parameter defaults ignored, and an unbound constructor parameter on `[NuruRoute]`.

## Prior findings
| ID | Status | Evidence |
|----|--------|----------|
| M22 | fixed | `command-construction-emitter.cs:62-73,116-120` leaves a `default!` member out of the object initializer and assigns it only when supplied. `json-args-emitter.cs:347-351,564-567` declares and sets `__supplied_{id}`. Run: `TimeSpan Wait { get; init; } = TimeSpan.FromSeconds(30)` and `Guid Id { get; set; } = Guid.NewGuid()`. Omitted on argv and on `--json-args {}`, they give `00:00:30` and a non-empty Guid. `--Wait 00:00:05` and `{"Wait":"00:00:07"}` override. `TimeSpan? = FromSeconds(3)` is also correct on both paths. The `required` regression is Issue 2 and the case mix-up is Issue 4. |
| M23 | fixed | `endpoint-extractor.cs:1184-1222` (named NaN/infinity constants), `:1122-1130` (enum const field kept as a symbol; flags and cast become `(global::E)n`). Run: `Perm.Read \| Perm.Write`, `Defaults.Color` (a const enum field), `double.NaN`, `float.PositiveInfinity`, `(Tint)1`, `(Level)(-1)` all compile and print correct values. New regression for `Shade? = null`: Issue 5. |
| M24 | fixed | `operation-endpoint-extractor.cs:686-687` drops `IsInitOnly`. `:696-741` selects the constructor. `command-construction-emitter.cs:38-50` emits `new(args)`. `diagnostic-descriptors.handler.cs:117-124` adds NURU_H009. Run: `record R3PosQuery(string Name, int Count = 5)` with `required string Extra { get; init; }` builds and binds. A keyword-named constructor (`string @event, string @class`) works on argv and JSON. Residual gaps: Issues 6, 7, 8. |
| M25 | fixed | `route-matcher-emitter.cs:1666-1672` strips the leading `@`. `T508ParamsQuery` (`Params` JSON member) compiles and rejects `--Params` on argv. |
| M26 | fixed | `endpoint-extractor.cs:848,853,875` skips a base property by declared name. Run: base `Name` plus derived `[Parameter] new string Name` produces `name` and `Keep` only, with no CS0128. |
| M20 | fixed | `operation-endpoint-extractor.cs:690-694` uses `FullyQualifiedFormat.WithGlobalNamespaceStyle(Omitted)` for the whole type, including type arguments. |
| M30 | fixed | `endpoint-extractor.cs:93-98`: the mediator-kind check now runs before `ValidateRoutePattern` (`:101`). |
| M31 | fixed | `capabilities-emitter.cs:331-339` prefers `CapabilityDefaultValue`. Run output: `Price` is `"1.5"`, `Color` is `"Blue"`, `Ratio` is `"NaN"`, `Top` is `"Infinity"`. A flags combination shows `"3"` (nit, Issue 11). Nullable `= null` shows `"0"` (Issue 5). |

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs:79-113 (with endpoint-extractor.cs:1186-1189)
- Description: Every init-only member whose default is `default!` doubles the number of emitted object initializers (`1 << initConditional.Count` branches), and the emitter does this once for argv and once for `--json-args`. `= null!` on a non-nullable reference is classified as `default!` (`case null:`), so the common DTO shape `public string X { get; init; } = null!;` triggers it. Measured: 12 such members produce 8192 `__command = new` sites, an 84,270-line `NuruGenerated.g.cs`, and a 117 s `dotnet build`. The same type with `{ get; set; }` produces 2,402 lines and builds in 4.9 s. Each further member doubles the cost. At 31 or more members, `1 << n` overflows and the loop runs about 2^31 times. The same applies to `[NuruRoute]` `[Option]` init members, so existing 3.0 apps are affected.
- Suggestion: Emit `null!` as a real literal. Assigning null gives the same value as the initializer, so it needs no conditional. For initializers the generator really cannot copy, make the cost linear. For example, build `var __defaults = new T(args) { /* required members */ };` once and emit `Prop = __supplied ? local : __defaults.Prop`, or fall back to a single initializer with a diagnostic above a small limit. Add a test with several init-only members that have non-copyable defaults. No current test exercises the `initConditional` branch.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs:62-73,116-120
- Description: A `required` member whose initializer the generator cannot copy is now assigned conditionally. A settable one is assigned after `new()`, and an init-only one is left out of some branches. Either way the build fails with CS9035. Repro: `public required TimeSpan Wait { get; set; } = TimeSpan.FromSeconds(1);` produces `CS9035: Required member 'R3ReqQuery.Wait' must be set in the object initializer` at two generated sites. `required ... = null!` hits the same path. This compiled before the delta, which always assigned the member, and the same shape on a 3.0 `[NuruRoute]` `[Option]` property also compiled.
- Suggestion: Always place `required` members in the object initializer. `required` already makes the option mandatory, so the conditional is never needed. Carry `IsRequired` on `OptionDefinition` (or reuse `CapabilityRequired`) and exclude those members from `conditional`.
- Status: open

### Issue 3 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs:348-352
- Description: The new exit-1 path for a visible handler that is not nested, in a compilation with no generated mediator, emits `return 1;` inside `HandlerInvokerEmitter.Emit`. When an `INuruBehavior` applies, that code sits inside the `async () => { ... }` lambda passed to `HandleAsync` (`behavior-emitter.cs:282-295`), and the build fails. Repro with `#:property TimeWarpMediatorAssembly=false`, `.AddBehavior(typeof(R3Behavior))`, and an `[Operation]` query with a top-level handler: `CS8031: Async lambda expression converted to a 'ValueTask' returning delegate cannot return a value` on both the argv and `--json-args` paths. Without the behavior it prints the message and exits 1, which is correct.
- Suggestion: Emit this guard before the behavior pipeline, as `InterceptorEmitter.EmitMissingHandlerOnDispatch` does, or emit the write plus a non-value return / exit-code assignment when inside the pipeline. Add the behavior case to the generator-55 no-mediator test.
- Status: open

### Issue 4 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs:173-188 (`MatchOption`)
- Description: `MatchOption` returns the first option whose `LongForm` matches ignoring case. It drives both the `default!` decision and the slot id in `__supplied_{id}`. Round 2 fixed exactly this for `AssignVariables` (`json-args-emitter.cs:1578-1582`, "--Quiet and --quiet are different options"), and the new emitter brings it back. Repro: `[Option("quiet")] public string Level { get; set; } = Init();` plus derived `public bool Quiet { get; set; } = false;`. The generated code has `if (__level_flagFound_0) __command.Quiet = quiet;` and `if (__supplied_quiet) __command.Quiet = quiet;`. So `r3case --Quiet` and `--json-args {"Quiet":true}` both print `quiet:False`. When the options are swapped, the wrong match also drops a supplied value or overwrites a kept initializer. `NeedsJsonSuppliedFlag` (`json-args-emitter.cs:1647-1685`) matches exact-first, so the two can also disagree about which `__supplied_` local exists, which can leave an undeclared identifier.
- Suggestion: Match exact first (`SourceName` against `LongForm`/`ShortForm`), then fall back to ignore-case only for options not already claimed. Share one matcher between `CommandConstructionEmitter`, `NeedsJsonSuppliedFlag`, and `AssignVariables`. Add a mixed-case test with a `default!` option.
- Status: open

### Issue 5 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1122-1130
- Description: The new enum branch runs for any constant, including null. For `public Shade? Color { get; set; } = null;`, `MatchingEnumMemberName(null)` returns null and the branch emits `(global::Shade)0` with display `"0"`. Generated: `global::Shade? color = (global::Shade)0;` on the `--json-args` path. Run: `--json-args {}` prints `color:Red` (argv prints `color:null`), and `--capabilities` shows `"defaultValue": "0"`. Before the delta, `FormatConstantLiteral(null, ...)` produced `null`. `= default` on the same type is unaffected.
- Suggestion: Guard the branch with `constantValue.Value is not null`, so null falls through to `FormatConstantLiteral` (`"null"`). Add a nullable-enum `= null` test.
- Status: open

### Issue 6 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1160; operation-endpoint-extractor.cs:289
- Description: A positional record that redeclares its property from the parameter is the standard C# way to customize a positional member:

  ```csharp
  public sealed record R3SameQuery(string Name) : IQuery<string>
  {
    public string Name { get; init; } = Name;
  }
  ```

  The initializer resolves to an `IParameterSymbol`, so it falls through to `initializerValue.ToString()`. That copies `Name` into generated code, which fails with `CS0103: The name 'Name' does not exist in the current context` at two sites. A parameter-based call such as `= Name.Trim()` becomes `default!` instead. That makes the option optional (`contractRequired` is false because the literal is not null), so omitting `--Name` passes null into the non-nullable constructor parameter, and the initializer then throws NullReferenceException.
- Suggestion: When a property is a constructor argument (or its initializer references an `IParameterSymbol`), ignore the initializer as a default. Its value comes through the constructor, so derive required or optional from nullability (or from the parameter default, Issue 7). Add a test.
- Status: open

### Issue 7 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1099-1101; operation-endpoint-extractor.cs:289
- Description: A primary-constructor parameter default is ignored, because the property's syntax is a `ParameterSyntax` and the extractor returns no default. `record R3PosQuery(string Name, int Count = 5)` derives `--Count` as required:
  - `r3pos --Name x --Extra e` exits 1 with "Unknown command".
  - `--json-args {"Name":"j","Extra":"e"}` exits 1 with "Missing required value 'Count'".

  The contract says `Count` is optional with default 5, and positional records are the shape M24 targets. Capabilities also report `required: true` with no default.
- Suggestion: For a positional property, read the matching `IParameterSymbol.HasExplicitDefaultValue` / `ExplicitDefaultValue` through `FormatConstantLiteral`, so the option is optional and shows the default. The constructor argument is always passed, so the local's initializer must hold that value. Add a test.
- Status: open

### Issue 8 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs:40-50; operation-endpoint-extractor.cs:755-773
- Description: Constructor selection only requires each parameter to match a derivable property. The emitter silently skips any parameter that has no binding (`if (binding is null) continue;`). On a `[NuruRoute]` type, unmarked properties are not bound, so `[NuruRoute("r3routepos")] record R3RoutePos([property: Option("name")] string Name, int Hidden)` emits `new(name)` and fails with CS7036 instead of NURU_H009. This is not a regression, because 3.0 emitted `new()` and also failed. But H009 promises the constructor is callable.
- Suggestion: In `TrySelectConstructorArguments`, also require that each matched property will be bound (derived or attribute-marked, depending on the endpoint kind), or report H009 when the emitter would drop an argument.
- Status: open

### Issue 9 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1572-1575 (outside the fix delta, from an earlier round of this task)
- Description: A nullable enum with a non-null initializer behaves differently on each path. With `Shade? Third { get; set; } = Shade.Green` omitted:
  - argv gives null, because the comment says "Nullable enum: omission stays null".
  - `--json-args {}` gives `Green`.
  - `--capabilities` advertises `Green`.
  - `(Tint)1` on `Tint?` behaves the same way.

  This contradicts "omission keeps the initializer".
- Suggestion: Initialize the nullable-enum local with `DefaultValueLiteral` when present, as the non-nullable branch at `:1587-1590` does.
- Status: open

### Issue 10 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:746-749
- Description: `HasCallableParameterlessConstructor` requires zero parameters. A `[NuruRoute]` type whose only constructor has all-optional parameters (`public Foo(int x = 0)`) compiled with `new()` on 3.0. It now gets NURU_H009 (an error) unless `x` happens to match a property. This is rare, but it is an error-level change for existing attribute endpoints.
- Suggestion: Treat a constructor whose parameters are all optional (or `params`) as parameterless.
- Status: open

### Issue 11 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1125
- Description: A flags-combination default (`Perm.Read | Perm.Write`) shows `"defaultValue": "3"` in `--capabilities`, while `allowedValues` lists member names. M31 asked for the member name, and the handler's own `ToString()` prints `Read, Write`.
- Suggestion: For a `[Flags]` enum with no exact member, show the comma-joined member names when the bits decompose exactly. Otherwise keep the number.
- Status: open
