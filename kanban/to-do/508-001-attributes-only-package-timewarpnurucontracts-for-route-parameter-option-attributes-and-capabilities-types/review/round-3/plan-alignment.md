# Round 3 — plan_alignment
**Date:** 2026-10-18
**Scope reviewed:** fix delta `git diff 572ecdc1..bb2d2b9f -- . ':!kanban'` (17 product files) and the `task.md` changes in that delta. Read in full: `task.md` (Description, Requirements, Goal restatement with "Fix-pass acceptance", Learnings, Results), `review/round-2/merged.md`, and `documentation/user/features/neutral-contracts.md`. Every claim below was checked against the code at `bb2d2b9f`. I did not run builds or tests; the tests reviewer covers that.

## Summary

Eight of the nine fix-pass acceptance bullets are delivered, and tests cover them. M32 is fixed. M29 is fixed in the docs and in the plain source-gen path. However, the new exit-1 branch for an external handler with no generated mediator is emitted as `return 1;` inside the `INuruBehavior` lambda (`Func<ValueTask>`). Any app in that configuration that has an applicable behavior gets generated code that does not compile (Issue 1). Before this delta, that configuration compiled and threw at runtime.

A second, narrower regression: a `required` member whose initializer the generator cannot copy is now left out of the object initializer, which is CS9035 (Issue 2).

The docs and changelog match the main behaviors. They leave out NURU_H009 and the positional-record / constructor rule, and a few display statements are too broad. The restatement now matches the Goal paragraph on mediator dispatch, so the round-2 "every host" wording is fixed. One acceptance bullet still says "startup reports an error" (nit).

NURU_H009 is a reasonable Error with an `AnalyzerReleases.Unshipped.md` entry (line 37). Its message has small inaccuracies (folded into Issue 4).

## Fix-pass acceptance walk

| # | Bullet (task.md:25-33) | Verdict | Evidence |
|---|---|---|---|
| 1 | Omitted uncopyable initializer stays on argv and `--json-args`; capabilities omit it | Delivered, except `required` members (Issue 2) | `command-construction-emitter.cs:63-73` (conditional / discard), `:116-118` (assign only when supplied); `json-args-emitter.cs:347-351,564-567` (`__supplied_{id}`); `capabilities-emitter.cs:331-340` filters the sentinel. Test `operation-endpoint-derivation.cs` `t508defaults`: argv and `--json-args {}` both expect `items:set|delay:30|token:set`, and `Items`/`Delay`/`Token` have no `defaultValue`. The init-only conditional path (`command-construction-emitter.cs:85-113`, combinatorial branches) has no test. |
| 2 | NaN, infinities, const enum field, flags/cast enum compile; capabilities show a plain number and the enum member name | Delivered | `endpoint-extractor.cs:1118` (enum member → name), `:1121-1130` (enum-typed constant → symbol or `FormatEnumCast`, display = member name or integral), `:1194-1218` (named NaN/∞ constants, `TrimRealDisplay`). Test `t508special` covers runtime values and capabilities `NaN`, `Infinity`, `-Infinity`, `Fast`, `3`, `3`, plus the `[Option("quiet")]` attribute path. A flags value that matches no member shows `3`, not member names. Results says this; the docs do not (Issue 5). |
| 3 | `init` members and positional records derived; primary-constructor construction; `required init` set in the initializer | Delivered, except `required` + uncopyable initializer (Issue 2) | `operation-endpoint-extractor.cs:687` (drops `IsInitOnly: false`), `:696-745` (`TrySelectConstructorArguments`), `command-construction-emitter.cs:36-50,134-150` (`new(args)`). Tests: `t508user --Id 7` prints `id:7`; `t508init --Name ada` prints `name:ada|extra:3`. |
| 4 | Keyword-named JSON member gets a valid rejection flag | Delivered | `route-matcher-emitter.cs:1666-1672` strips `@`. Test `t508params --Params nope` exits 1 with the JSON message. |
| 5 | Redeclared base property is not derived twice | Delivered | `endpoint-extractor.cs:848,853,875` (`seenPropertyNames` by declared name). Test `t508hide`: `--title ada` gives `name:ada`, omission gives `name:child`. |
| 6 | Direct invoke with only an external handler emits `__GetMediator`; no generated mediator → nested handler called directly | Delivered | generator-55 `Should_keep_get_mediator_when_direct_invoke_has_only_an_external_handler` (updated compilation, no CS0103) and `Should_call_a_nested_handler_directly_when_no_generated_mediator_exists`. Logic at `handler-invoker-emitter.cs:301-302`. |
| 7 | Overhead script: µs, separate first call, several alternating AOT cold starts | Delivered | `operation-dispatch-overhead.cs:19-31` (µs, computed decision), `:57-63,96-102` (first call timed before warm-up), `:148-165` (3 alternating samples, ms). Order nit: Issue 7. |
| 8 | Docs name the no-mediator condition; an external handler in that build exits 1 with a message | Docs delivered. Code delivered only without behaviors and without runtime DI (Issues 1, 3) | `neutral-contracts.md:60` matches `generated-mediator-detector.cs:26-39`. `handler-invoker-emitter.cs:346-353` emits the message and `return 1;`. generator-55 `Should_exit_1_for_an_external_handler_when_no_generated_mediator_exists` checks only the generated text, with no behavior and no compile. |
| 9 | JSON type names omit `global::` in type arguments; mediator-kind check before pattern validation; NURU_H008 wording | Delivered | `operation-endpoint-extractor.cs:690-694` (`OmittedGlobalFormat`), test `Bag` (`List<`, no `global::`); `endpoint-extractor.cs:93-98` runs before `ValidateRoutePattern`, generator-55 `Should_skip_pattern_validation_...`; `diagnostic-descriptors.handler.cs:108`. |

## Prior findings

| ID | Status | Evidence |
|----|--------|----------|
| M29 | Partially fixed | Docs: `neutral-contracts.md:60` names the condition (`TimeWarpMediatorAssembly` not true, or profile `Aot`/`Link`), which matches `generated-mediator-detector.cs:26-37`. It also says FluentValidation does not run and an external handler exits 1. Code: `handler-invoker-emitter.cs:346-353` replaces the `__GetMediator()` throw with `WriteErrorLine` + `return 1;` when `!useRuntimeDI && !hasGeneratedMediator`. With an applicable `INuruBehavior`, that `return 1;` lands inside `async () => { ... }` passed as `Func<ValueTask>` (`behavior-emitter.cs:282-292`, `route-matcher-emitter.cs:252-254`, `json-args-emitter.cs:1270-1290`), which does not compile (Issue 1). Under `UseMicrosoftDependencyInjection` it still throws from `GetRequiredService<IMediator>` (Issue 3). |
| M32 | Fixed | `diagnostic-descriptors.handler.cs:108`: "Register one, or invoking this route exits 1." generator-55 asserts the message contains `exits 1`. |
| NURU_H009 (new) | Present | `diagnostic-descriptors.handler.cs:117-124`, Error, Handler category; `AnalyzerReleases.Unshipped.md:37` matches the id, category, severity, and title. Raised at `endpoint-extractor.cs:115-122`, which skips the endpoint. generator-55 asserts Error and no `__command`. Message nits are in Issue 4. |

## Restatement check

- Round-2 Learnings asked that the acceptance bullet stop saying mediator behaviors "run in every host". This is fixed. `task.md:40` now says "when this compilation has a generated mediator, so mediator behaviors run in that host", names both direct-invoke cases, and states the exit-1 case. That matches the Goal paragraph (`task.md:21`) and the code.
- Narrowing "run identically in every host" (Requirements) to builds with a generated mediator is the documented compatibility carve-out from round 2. It is explicit and not a misread.
- The fix-pass acceptance bullets (`task.md:25-33`) restate round-2 M20 and M22–M32 faithfully. M27 maps to bullet 6, M28 to bullet 7, and M29 to bullet 8.
- Stale wording: `task.md:39` still says "otherwise startup reports an error". The implemented and documented behavior is per-route exit 1 on invocation (Learnings `task.md:100`, NURU_H008 message, `neutral-contracts.md:54`). The Requirements line says "startup error", and the M18 deviation from it is recorded in Learnings but not in the restatement (Issue 6).
- **Verdict: no misread of substance.** One stale acceptance bullet (nit).

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs:348
- Description: The new no-generated-mediator branch for a visible, non-nested handler emits `app.Terminal.WriteErrorLine(...); return 1;` from inside `EmitCommandInvocation`. When the app has any applicable `INuruBehavior`, `BehaviorEmitter.EmitBehaviorCall` emits this handler body inside `await behavior.HandleAsync(context, async () => { ... })`, where the lambda must be `Func<ValueTask>` (`behavior-interfaces.cs:124`, `behavior-emitter.cs:282-292`). An async `ValueTask` lambda cannot return a value, so the generated code does not compile. `EmitCommandResult` in the same file already documents this constraint ("The branch must not `return` a value"). This affects both the argv path (`route-matcher-emitter.cs:252-254`) and the `--json-args` path (`json-args-emitter.cs:1270-1290`).

  The trigger is `TimeWarpMediatorAssembly` off or mediator profile `Aot`/`Link`, plus a behavior, plus an `[Operation]` or `[NuruRoute]` endpoint whose handler is visible but not nested. Before this delta, that app compiled and threw at runtime. Now the build fails inside generated code. This also contradicts `neutral-contracts.md:60` and the Results line "A visible handler that is not nested writes an error and exits 1 in that build". The generator-55 test checks only the generated text, with no behavior, and does not compile the output.
- Suggestion: Emit this check before the behavior pipeline, next to `InterceptorEmitter.EmitMissingHandlerOnDispatch` (`route-matcher-emitter.cs:244`, and the matching JSON dispatch site), where `return 1;` is legal. Or, inside the lambda, set `Environment.ExitCode = 1` and `return;`, so the caller's `return global::System.Environment.ExitCode;` reports it. Add a generator-55 case with `.AddBehavior(...)` and `TimeWarpMediatorAssembly=false` that compiles the updated compilation and asserts no errors.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs:63
- Description: Every option whose default is the `default!` sentinel goes to `conditional` or `discards`, whether or not the property is `required`:
  - A settable member is assigned after construction (`:116-118`).
  - An init member appears only in some branches of the combinatorial `if`, and the final `else` omits it (`:85-113`).
  - An argv JSON member is never assigned (`Supplied` returns `"false"`, `:160-161`).

  C# requires a `required` member to be set in the object initializer of every `new`, so `public required Payload Body { get; set; } = new();` or `public required List<string> Tags { get; init; } = new();` now fails with CS9035 in generated code. Round 2 assigned the sentinel unconditionally, so this shape compiled (with the data loss M22 described). This contradicts fix-pass bullet 3, "A `required init` member is set in the object initializer". The shape is uncommon (`required` plus an initializer), but it is legal C#.
- Suggestion: Treat a `required` property as always supplied. Put it in `always` and keep it in the object initializer even when its initializer is uncopyable, because the route already rejects omission of a required option. This needs a `required` flag on `OptionDefinition` (or a check of `CapabilityRequired`/`IsRequired`). Add a derivation test with `required` plus `= new()`.
- Status: open

### Issue 3 — Severity: nit
- File: documentation/user/features/neutral-contracts.md:60
- Description: "A visible handler that is not nested writes an error and exits 1 instead of throwing" is true only for source-gen DI. The guard is `!useRuntimeDI && !hasGeneratedMediator` (`handler-invoker-emitter.cs:348`). Under `UseMicrosoftDependencyInjection` the code resolves `GetRequiredService<IMediator>`, and `AddGeneratedMediator` is registered only when a generated mediator exists (`interceptor-emitter.cs:853-858`). So such an app still throws `InvalidOperationException` unless the user registers an `IMediator` in `ConfigureServices`.
- Suggestion: Qualify the sentence. For example: "With runtime DI, Nuru resolves `IMediator` from your container, so register one or the call throws." Deferring to the container under runtime DI is reasonable; only the docs need to change.
- Status: open

### Issue 4 — Severity: suggestion
- File: documentation/user/features/neutral-contracts.md:13; changelog.md:11
- Description: This delta adds behavior that the user docs do not cover:
  - `init` members are options.
  - A type with no callable parameterless constructor is built through the constructor whose parameters match public settable properties. This is the positional-record case.
  - A type with neither constructor is NURU_H009, an Error, and the endpoint is skipped.

  `neutral-contracts.md` says only "Each public settable property" and "Classes and record classes both qualify". It never mentions positional records, constructor selection, or NURU_H009. The changelog `### Fixed` mentions init members and positional records, but `### Added` lists NURU_H008 and not the new NURU_H009 Error.

  The NURU_H009 message has small inaccuracies:
  - It says "Add a public parameterless constructor", but internal constructors are accepted too (`operation-endpoint-extractor.cs:717,749`).
  - It says "parameters match property names", but the match is case-insensitive and only against public settable properties (`:766-768,787`).
  - It also fires for abstract and static types (`:704-707`), where "has no constructor Nuru can call" is a misleading reason.
- Suggestion: Add a sentence to the "What the generator derives" section covering init members, primary-constructor binding, and NURU_H009. Add NURU_H009 to the changelog `### Added`. Consider rewording the message to "... has no accessible parameterless constructor and no constructor whose parameters all match public settable properties" and giving abstract or static types their own reason (or not treating them as endpoints at all).
- Status: open

### Issue 5 — Severity: nit
- File: documentation/user/features/neutral-contracts.md:17
- Description: "Numeric defaults are plain numbers. An enum default is the member name." is broader than the code:
  - A flags or cast value that matches no member shows the integral value (`3`). The test asserts this, and Results says so (`endpoint-extractor.cs:1125`).
  - A non-const `static readonly` field initializer of enum or numeric type falls through to `symbol.ToDisplayString(FullyQualifiedMemberFormat)` for both code and display (`endpoint-extractor.cs:1135-1138`). `--capabilities` then shows `global::Ns.Defaults.Color`, not a member name or number. That is the M31 symptom for this shape.
- Suggestion: Say "A constant numeric default is a plain number. An enum default is the member name, or the number when it matches no single member." Optionally omit the display for a non-const symbol, as is done for uncopyable initializers, instead of showing the `global::` path.
- Status: open

### Issue 6 — Severity: nit
- File: kanban/to-do/508-001-attributes-only-package-timewarpnurucontracts-for-route-parameter-option-attributes-and-capabilities-types/task.md:39
- Description: The acceptance bullet says "otherwise startup reports an error". Since M18, the behavior is per route: invoking the route exits 1, and `--help`/`--capabilities` still run. Learnings line 100, the NURU_H008 message, and `neutral-contracts.md:54` all say so. The restatement is the one place left with the old wording, and it does not record that the original "startup error" requirement was deliberately changed to per-invocation exit 1.
- Suggestion: Reword it to "otherwise invoking that route exits 1 (a deliberate change from a startup error; see Learnings)".
- Status: open

### Issue 7 — Severity: nit
- File: benchmarks/operation-dispatch/operation-dispatch-overhead.cs:19
- Description: The mediator path is always measured first in the same process. `mediator-first-us` therefore also pays the one-time JIT of Nuru's shared runtime paths (`RunAsync`, the matcher, terminal), not only the container build, and `direct-first-us` runs warm. The changelog and Results compare `28636` and `2665` µs side by side. That comparison overstates mediator startup cost. The AOT cold starts (`4,3,3` vs `3,3,3` ms) are the clean startup figures.
- Suggestion: Alternate the order across runs, or prime shared code with an unrelated route first. Alternatively, note in the changelog and Results that the in-process first-call figure includes shared JIT, and that the AOT cold start is the startup comparison.
- Status: open
