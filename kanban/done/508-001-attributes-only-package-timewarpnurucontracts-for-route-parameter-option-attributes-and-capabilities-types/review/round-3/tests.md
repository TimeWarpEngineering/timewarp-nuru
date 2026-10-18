# Round 3 — tests
**Date:** 2026-10-18
**Scope reviewed:** Fix delta `git diff 572ecdc1..bb2d2b9f -- . ':!kanban'` at HEAD `bb2d2b9f`. I focused on `tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs`, `tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs`, `benchmarks/operation-dispatch/operation-dispatch-overhead.cs`, and the product code those tests guard: `command-construction-emitter.cs`, `endpoint-extractor.cs`, `generator-model.cs`, and `handler-invoker-emitter.cs`. I checked these against `review/round-2/merged.md` and the `## Results` section of `task.md`.

## Summary

Every run matches the Results claims. The four focused tests pass at 28, 7, 8, and 19. The full CI suite has 1920 tests: 1914 passed, 6 skipped, 0 failed, and 0 standalone failures. All 6 skips already exist on the master merge base. The sample prints the expected values. The benchmark runs with `--aot`.

**M27 is fixed.** Each new generator-55 test would fail if its fix were reverted. I traced this through the pre-fix code.

**M28 is fixed.** The benchmark reports µs, times the first call on its own, and runs three alternating AOT cold starts, and the figures are in Results. I confirmed the AOT "mediator" binary really contains `AddGeneratedMediator` and the direct binary does not.

The derivation tests assert the fixed behavior for M22 through M26 and NURU_H009. If any of those fixes were reverted, the build or an assertion would fail.

There are four new issues, all coverage or measurement gaps and none of them bugs:
- The M22 path that assigns a supplied value is never run with a value supplied.
- The `init`-only conditional construction branch has no test.
- The `float` NaN and infinity defaults have no test.
- In the benchmark's in-process first-call figure, the mediator path always runs first.

## Runs (commands and actual results)

All commands ran from the task worktree.

1. `ganda runfile cache --clear` cleared 36 entries. Then:
   - `dotnet run --file tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs`: exit 0, Total 28, Passed 28. Matches the claim of 28.
   - `dotnet run --file tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs`: exit 0, Total 7, Passed 7. Matches the claim of 7.
   - `dotnet run --file tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs`: exit 0, Total 8, Passed 8. Matches the claim of 8.
   - `dotnet run --file tests/timewarp-nuru-tests/routing/routing-33-json-args.cs`: exit 0, Total 19, Passed 19. Matches the claim of 19.
2. `ganda runfile cache --clear` cleared 5 entries. Then `dotnet run tests/ci-tests/run-ci-tests.cs`: exit 0.
   - Multi-mode: Total 1920, Passed 1914, Skipped 6, Failed 0. Matches the claim.
   - Standalone: 30 files, "Standalone finished in 11.9s (0 failed)".
   - The 6 skips:
     - 4 × "Help output format not yet finalized - see task 069" (`repl-23-key-binding-profiles.cs`)
     - 1 × "PSReadLine does not support literal tab insertion" (`repl-37-quoted-strings.cs`)
     - 1 × "Pre-existing bug: IsConfigArg incorrectly filters --option=value syntax" (`routing-24-enum-option-parameters.cs`)

     `git grep` at merge base `5e512c0a` finds each of these skip messages, so all 6 are pre-existing.
3. Sample `samples/endpoints/02-calculator-contracts/calculator-contracts.cs`:
   - `-- calc add --X 2 --Y 3` → `5`, exit 0
   - `-- calc round --Value 1.2` → `1`, exit 0
   - `-- calc round --Value 1.2 --Mode Up` → `2`, exit 0
   - `-- calc total --Values 2 --Values 4 --json-args '{"Notes":[{"Label":"tax","Amount":1}]}'` → `total:6|average:3|notes:1`, exit 0
4. Benchmark `dotnet run --file benchmarks/operation-dispatch/operation-dispatch-overhead.cs -- --aot`: exit 0, 17.6 s wall time. Output:
   ```
   warmup=50
   iterations=2000
   mediator-first-us=27971
   direct-first-us=2509
   mediator-per-call-us=3
   direct-per-call-us=1
   decision=keep-UseDirectInvoke
   aot-mediator-cold-ms=3,2,2
   aot-direct-cold-ms=3,2,2
   aot=ran
   ```
   This is consistent with Results (28636 / 2665 / 3 / 1; AOT 4,3,3 and 3,3,3). `strings` on `/tmp/nuru-508-dispatch-aot/mediator-out/mediator` shows `GeneratedMediatorServiceCollectionExtensions` and `AddGeneratedMediator`. The `direct` binary has neither, so the AOT pair really compares the two dispatch paths.

## Prior findings

| ID | Status | Evidence |
|----|--------|----------|
| M22 | fixed (tested) | `operation-endpoint-derivation.cs:379-408`: with the option omitted, both argv `t508defaults` and `--json-args {}` assert `items:set\|delay:30\|token:set`. Capabilities omit `defaultValue` for Items, Delay, and Token. If the fix were reverted, the output would be `items:null\|delay:0\|token:empty`. Gap: no test supplies a value for these members (Issue 1). |
| M23 | fixed (tested) | `Should_keep_non_finite_enum_const_and_flags_defaults` (line 410) and `T508SpecialQuery` cover `double.NaN`, ±infinity through a const field, a const enum field declared outside the enum (`T508Numbers.Accent`), flags (`Read \| Write`), a cast (`(T508Access)3`), and NaN on an `[Option("quiet")]` property. All of these failed to compile before the fix, so a revert breaks the build. Gap: `float.NaN` and the float infinities (Issue 3). |
| M24 | fixed (tested) | `T508UserQuery(int Id)` is a positional record. Without the primary-constructor path, generated `new(id)` is CS7036. `T508InitQuery.Name` is `required init`; without it, CS9035. Both are asserted at runtime (`id:7`, `name:ada\|extra:3`). Gap: the `init` conditional branch (Issue 2). |
| M25 | fixed (tested) | `T508ParamsQuery.Params` produced `__jsonArgvRejected_@params_0`, which does not compile. The test asserts exit 1 and the `--Params is JSON` message. |
| M26 | fixed (tested) | `T508HideQuery` redeclares `Name` as `new` with `[Option("title")]`. Without the `seenPropertyNames` fix (`endpoint-extractor.cs:848-876`), the base `Name` derives a second option whose `ParameterName` is also `name` (line 673), and the two locals collide (CS0128). Runtime asserts cover `--title ada` → `name:ada` and omission → `name:child`. |
| M27 | fixed | **M4:** `generator-55:275` compiles a single app that calls `UseDirectInvoke()`, with only an external handler. Before the fix, `generator-model.cs` returned false when every app used direct invoke (`if (!mediatorApp) return false;`), so `__GetMediator` was never declared. The test asserts the declaration string and the absence of CS0103 on the updated compilation, so a revert fails it. **M2:** `generator-55:239` sets `build_property.TimeWarpMediatorAssembly=false`, so `GeneratedMediatorDetector.Detect` returns false and no generated mediator exists. The app has no `UseDirectInvoke`. Before the fix, `directInvoke = useDirectInvoke && nested` was false, and the code emitted `__GetMediator()` and `__mediator.Send`. The test asserts `__handler.Handle(` and that `__GetMediator` is absent, so a revert fails it. The external-handler exit-1 path also has a test (`Should_exit_1_for_an_external_handler_when_no_generated_mediator_exists`). |
| M28 | fixed | The benchmark converts ticks to µs with `ToMicroseconds` (ticks × 1e6 / Frequency). It times the first call before warm-up (`mediator-first-us` / `direct-first-us`). `decision` is computed (`directPerCall <= mediatorPerCall`). The `--aot` run alternates binary order across three cold starts, and the figures are in Results. Measurement nit: Issue 4. |
| M20 | fixed (tested) | The `Bag` member is `List<T508Note>`. The test asserts its type contains `List<` and `T508Note` and does not contain `global::` (line 315). |
| M29 | fixed (exit-1 path tested) | `generator-55` asserts the "generated mediator is not available" message and `return 1;`. |
| M30 | fixed (tested) | `Should_skip_pattern_validation_when_the_operation_is_not_a_mediator_message` asserts there is no NURU_A001 for `[Operation("not an endpoint")]` on a type that is not a mediator message. |
| M31 | fixed (tested) | Capabilities `defaultValue` asserts `1.5`, `2`, `Fast`, `3` (flags and cast), `NaN`, `Infinity`, and `-Infinity`. |
| M32 | fixed (tested) | `generator-55` asserts the NURU_H008 message contains `exits 1`. |
| NURU_H009 | tested | `Should_report_nuru_h009_when_the_endpoint_has_no_callable_constructor` uses a private parameterless constructor plus a public constructor with an unmatched parameter. It asserts Error severity and that no `T508BadCtorQuery __command` is emitted. A revert fails it, because `First()` throws when no diagnostic is reported. |

**`.Map<T>()` rule:** the derivation test adds five `.Map<T>()` calls (lines 55-59) and uses no `.DiscoverEndpoints()`. Two new generator-55 tests use `.DiscoverEndpoints()`, but only inside a source string compiled in its own `CSharpCompilation`. Those endpoints never join the CI assembly, so they cannot leak into other tests.

## Issues

### Issue 1 — Severity: suggestion
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs:379
- Description: The M22 fix changes how a supplied value reaches the command. When the generator cannot copy an initializer, the value is no longer in the object initializer. Instead it is assigned after construction behind a guard:
  - On argv: `if (__delay_flagFound_16) __command.Delay = delay;`
  - On `--json-args`: `if (__supplied_Delay) __command.Delay = delay;`, where `__supplied_Delay = __av_Delay || __jsonMap.ContainsKey("Delay")`

  Every test only omits `Items`, `Delay`, and `Token`. Suppose `CommandConstructionEmitter.Supplied` regressed to an expression that compiles but is always false, for example by naming the wrong flag local. Supplied values would then be silently dropped and replaced by the initializer, and every test would still pass. The code generated now looks correct (`artifacts/generated/operation-endpoint-derivation.cs/.../NuruGenerated.g.cs`, lines 9199-9298 and 12020-12030), but nothing guards it. (`T508JsonQuery.Notes = []` does not cover this path: a collection expression is kept as syntax, not as `default!`.)
- Suggestion: In `Should_keep_decimal_float_const_bool_and_skip_unrepresentable_defaults`, add a supplied case on each path. On argv, `t508defaults --Delay 00:00:05` should print `delay:5`. On JSON, `t508defaults --json-args '{"Delay":"00:00:05"}'` should print `delay:5`, and optionally a supplied `Token` should differ from a fresh GUID.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/emitters/command-construction-emitter.cs:82-114; tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs:1056
- Description: The `init`-only conditional branch has no test. This branch emits `if / else if / else` over 2^n supplied combinations, so that an omitted `init` member whose initializer cannot be copied keeps that initializer. No test contract has an `init` member with a non-copyable initializer, such as `public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);`. A wrong mask, a swapped term, or a missing `else` would go unnoticed. The `else` mistake gives CS0165 only when the compiler can see it. Separately, `T508InitQuery.Extra` (a non-required `init` member with initializer `3`) is only tested with the option omitted. The output `extra:3` is the same whether or not `Extra` is derived, so the test does not show that non-required `init` members are back on the CLI. This was part of round-2 M24.
- Suggestion: Add an `init` member with a non-copyable initializer to `T508InitQuery`, or to a new contract. Assert the initializer on omission and the supplied value with the option passed, on argv and on `--json-args`. Also assert `t508init --Name ada --Extra 5` prints `extra:5`.
- Status: open

### Issue 3 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1193-1202
- Description: `float` has its own branch, which emits `float.NaN`, `float.PositiveInfinity`, and `float.NegativeInfinity`. Round-2 M23 named `float.NaN` → `NaN.0f` as one of the cases that failed to compile, but `T508SpecialQuery` covers only `double`. A regression in the float branch would not be caught.
- Suggestion: Add `public float Soft { get; set; } = float.NaN;` (and one infinity) to `T508SpecialQuery`, and assert both the runtime value and the capabilities `NaN`.
- Status: open

### Issue 4 — Severity: nit
- File: benchmarks/operation-dispatch/operation-dispatch-overhead.cs:19-22
- Description: The in-process run always measures the mediator path first. So `mediator-first-us` (about 28 ms) also pays the JIT for the shared Nuru runtime path: `RunAsync`, routing, and the terminal. `direct-first-us` (about 2.5 ms) then reuses that JIT work. The roughly 25 ms gap is therefore not all container build, yet Results reports the two first-call figures side by side. The AOT cold starts, which alternate order, are the fair startup comparison, and they show parity (3,2,2 vs 3,2,2 in this run). Also, per-call values are truncated to whole µs (`3` vs `1`), which is coarse for a computed `decision`.
- Suggestion: Run each path in its own process for the first-call figure, or alternate the order and print both orders. Alternatively, note in Results that the first-call figure includes shared JIT. Report per-call in ns, or with one decimal place.
- Status: open
