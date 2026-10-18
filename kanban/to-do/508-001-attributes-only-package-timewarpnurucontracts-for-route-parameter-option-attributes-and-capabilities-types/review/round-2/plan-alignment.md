# Round 2 — plan_alignment
**Date:** 2026-10-18
**Scope reviewed:** `task.md` (Description, Requirements, updated Goal restatement, Learnings, Results, How to validate), `review/round-1/merged.md`, `review/round-1/plan-alignment.md`, and the fix delta `git diff d769f8b1..e316fd32 -- . ':!kanban'`. I checked the docs page, changelog, `AnalyzerReleases.Unshipped.md`, the NURU_H008 descriptor, the benchmark, and the dispatch, renderer, default, and JSON-member emitters against each other. Read-only. No build or test run.

## Summary

M8 and M17 are fixed. The docs page and changelog now match the code on these points: the dispatch default, the nested-handler fallback, `UseDirectInvoke()`, external handlers staying on the mediator, NURU_H008 as a Warning, the required-JSON error text, renderer precedence, record support, and inherited members. The test counts in Results match the files (24, 7, 3), and every listed path exists.

I found one new bug. The `default!` sentinel replaces `= new()` and invocation initializers with `default` when the option is omitted. The docs say "Omission keeps the initializer". The startup half of "Measure startup and per-call overhead" is still not measured. Task.md discloses this, so it is a suggestion. The "no generated mediator" condition is neither documented nor diagnosed. There are also two nits.

## Prior findings

| ID | Status | Evidence |
|----|--------|----------|
| M8 | fixed | `diagnostic-descriptors.handler.cs:110` is `DiagnosticSeverity.Warning`. `AnalyzerReleases.Unshipped.md:36` says `Warning`. `neutral-contracts.md:54` says "reports NURU_H008 as a warning. The build is accepted", which matches. `generator-55-missing-operation-handler.cs:81` asserts Warning. `operation-endpoint-derivation.cs:420-435` asserts that `--help` and `--capabilities` exit 0 and that invoking the handlerless route exits 1 with "No handler is registered for". One wording leftover is Issue 4. |
| M17 | fixed | `changelog.md:10-16` has `### Added` covering `UseDirectInvoke()`, `[NuruCliBinding(Group)]`, NURU_H008, the renderer, and problem rendering. `### Changed` covers the dispatch default, the nested-handler fallback, external handlers on the mediator, the `Environment.ExitCode` reset (`interceptor-emitter.cs:173`), external-handler `[NuruRoute]` endpoints, and the measured ticks. Minor: "A visible or registered `IResponseRenderer<T>`" does not state the precedence, but the docs do. |

Doc and changelog spot-checks against source, all consistent:
- **Dispatch default and fallback.** `handler-invoker-emitter.cs:301-302` is `directInvoke = nested && (useDirectInvoke || !hasGeneratedMediator)`. `generator-model.cs:99-112` uses the same rule to decide whether `__GetMediator()` is emitted. This matches `neutral-contracts.md:58-60` and `changelog.md:15`.
- **External handler stays on the mediator.** With no nested handler, `directInvoke` is false whatever `UseDirectInvoke()` says. Tested at `operation-endpoint-dispatch.cs:80`.
- **Required-JSON error text.** `route-matcher-emitter.cs:1654,1660` emits `Error: --{x} is JSON. Pass it with --json-args.` and `Error: pass --{x} via --json-args.`. These match Results, the docs, and `operation-endpoint-derivation.cs:409,413`.
- **Renderer precedence.** Runtime DI tries `GetService<IResponseRenderer<T>>` first, then the visible renderer, then the output strategy. Source-gen tries `TryResolveRegisteredService` first, then the visible renderer, then the output strategy (`handler-invoker-emitter.cs:439-481`). This matches `neutral-contracts.md:64-66`.
- **Records.** `nuru-generator.cs:305-308` accepts classes and record classes and excludes record structs, as the docs say.
- **`default!` omitted from capabilities.** `capabilities-emitter.cs:331` does this. The runtime effect contradicts the docs; see Issue 1.

## Restatement check

There is no misread of substance. The restatement keeps every requirement and folds in the 506 findings and the group decision.

**Nested-handler fallback.** I judge the carve-out reasonable and documented. The requirement holds two constraints: "dispatch through TimeWarp.Mediator so mediator behaviors run identically in every host" and "keep working unchanged for existing apps". The fallback fires only when the compilation has no generated mediator (`generated-mediator-detector.cs:23-40`: `TimeWarpMediatorAssembly` is not true, or the profile is `Aot`/`Link`). In that case no mediator pipeline exists, so behaviors could not run anyway. The only alternative is a runtime `InvalidOperationException`, which is what round-1 M2 reported. The carve-out is in the Goal paragraph, `neutral-contracts.md:60`, and `changelog.md:15`.

Two caveats:
- **Scope.** The fallback covers new contracts-first `[Operation]` endpoints with a nested `Handler`, not only existing `[NuruRoute]` apps. The compatibility argument only requires the latter. In a project with no generated mediator, such an endpoint silently skips mediator behaviors such as FluentValidation. The docs never say when "no generated mediator exists". See Issue 3.
- **Internal inconsistency.** The acceptance bullet (task.md:28) still says "so mediator behaviors run in every host", and it drops "identically". The Goal paragraph above it describes the exception. The bullet should say "when a generated mediator exists" so the two agree.

**Startup measurement.** The restatement keeps "Startup and per-call cost ... is measured" (task.md:28), and the checklist marks "overhead measured" done. Only per-call cost was measured. See Issue 2.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1101-1107. Related: route-matcher-emitter.cs:1622, documentation/user/features/neutral-contracts.md:17.
- Description: When an initializer is an object creation (`= new()`, `= new Foo()`) or an invocation (`= Guid.NewGuid()`), the extractor returns `default!` as the default literal.
  - That literal does not count as "no default", so `contractRequired` is false (operation-endpoint-extractor.cs:281). The member becomes an optional option.
  - When the caller omits the option, the generated local is `default!` (route-matcher-emitter.cs:1622 for JSON members, and the same literal on the scalar paths). The object initializer then assigns that value to the property and overwrites the initializer.
  - Results: `public List<Item> Items { get; set; } = new();` is null in the handler, even though its type is non-nullable. `public Guid Id { get; set; } = Guid.NewGuid();` becomes `Guid.Empty`.
  - The test pins this behavior: `operation-endpoint-derivation.cs:372` expects `items:null` for `T508Fresh? Items = new()`.
  - The docs table row "a non-nullable member with an initializer → Optional option. Omission keeps the initializer" (neutral-contracts.md:17) is false for these initializers.
  - The requirement says "defaults from initializers". Results (task.md:104) says only that such a default "is omitted from capabilities". It does not mention that the runtime value is lost.
  - `= new()` on a collection or object property is a common DTO idiom, so a handler can hit a NullReferenceException.
- Suggestion: When the option is omitted, do not assign the property, so the type's own initializer stands. One way is a conditional assignment, or a `with`/post-construction set only when the value was supplied. The alternative is to treat these members as required and document that. Then update the docs row and the test expectation.
- Status: open

### Issue 2 — Severity: suggestion
- File: benchmarks/operation-dispatch/operation-dispatch-overhead.cs:20-32. Related: task.md:28,102,120.
- Description: The requirement says "Measure startup and per-call overhead". Only per-call cost was measured.
  - The first mediator call builds the container (`interceptor-emitter.cs:464`), and that call falls inside the 50 warm-up iterations, so the one-time cost is deliberately excluded.
  - The `--aot` cold-start path exists (lines 102-127) but was not run, and the task says so. Startup is therefore not measured at all, in JIT or AOT.
  - For a one-shot CLI, startup is the cost that matters most.
  - The decision is unaffected because `UseDirectInvoke()` was kept anyway. That and the honest disclosure keep this at suggestion rather than bug. Even so, the checklist item "overhead measured" and acceptance bullet task.md:28 are marked done while half of the measurement is missing.
  - Smaller points: the script prints raw Stopwatch ticks without `Stopwatch.Frequency`, so the numbers cannot be read as time. The `decision=keep-UseDirectInvoke` line is hard-coded, not computed.
- Suggestion: Run `--aot` once, or add a JIT first-call measurement that does not discard the first call. Record the startup figure in Results and the changelog. Print `Stopwatch.Frequency` or per-call nanoseconds. Otherwise, mark the startup half as not done in the checklist and acceptance.
- Status: open

### Issue 3 — Severity: suggestion
- File: documentation/user/features/neutral-contracts.md:58-60. Related: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:456-458.
- Description: The docs say behavior changes "when no generated mediator exists", but never say when that happens. It happens when `TimeWarpMediatorAssembly` is not true or the TimeWarp.Mediator profile is `Aot` or `Link`. In that configuration:
  - A contracts-first `[Operation]` endpoint with a nested `Handler` silently skips mediator behaviors. No diagnostic fires.
  - An endpoint with a visible external handler compiles without a diagnostic, because NURU_H008 only fires when no handler is visible. Invoking it then throws `InvalidOperationException("The TimeWarp.Mediator generated mediator is not available in this compilation.")` from `__GetMediator()` instead of a clean exit 1.
  - The generator can tell in both cases (`hasGeneratedMediator` is a compile-time value).
- Suggestion: Document the condition (the property and profiles) in the Dispatch section. Report a warning when an endpoint without a nested handler is built with no generated mediator. Optionally, also warn when `[Operation]` endpoints fall back to direct invoke, because mediator behaviors will not run.
- Status: open

### Issue 4 — Severity: nit
- File: source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.handler.cs:108
- Description: The NURU_H008 `messageFormat` still says "Register one, or the app fails at startup." After M18, the app fails only when that route is invoked, and `--help` and `--capabilities` still work. The `description` on line 112 and the docs say this correctly. The message that users see in the build output does not.
- Suggestion: Change it to "Register one, or invoking this route fails with exit code 1."
- Status: open
