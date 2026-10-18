# Round 4 — merged findings
**Date:** 2026-10-18
**Sources:** general
**Reviewed commit:** `50e92396` (fix delta `bb2d2b9f..50e92396`, product paths only)

The orchestrator checked the fix against the code and the generated derivation app at `50e92396`:

- `command-construction-emitter.cs` builds one `__commandDefaults` and assigns `P = supplied ? p : __commandDefaults.P`. `NuruGenerated.g.cs` for `T508InitManyQuery` has one defaults instance on argv and on `--json-args`, not one branch per combination. `Label = null!` is copied as `label = null!` and assigned directly. M33 is fixed.
- `T508RequiredDefaultQuery` is `new() { Wait = wait, Tag = tag, Body = body }` on both paths. `NeedsJsonSuppliedFlag` skips required members. M34 is fixed.
- `InterceptorEmitter.EmitMissingHandlerOnDispatch` writes the exit-1 guard before the behavior pipeline. `HandlerInvokerEmitter` emits only a comment on that path. generator-55 `Should_compile_an_external_handler_with_a_behavior_when_no_generated_mediator_exists` passes with no errors other than the harness CS9137. M35 is fixed.
- `OptionBindingMatcher` matches exact names first. Generated `t508case` keeps `--mute` on `Level` and `--Mute` on `Mute`, on argv and on `--json-args`. M36 is fixed.
- The enum branch requires a non-null constant. Generated locals are `unset = null`, `preset = T508Mode.Slow`, and `combined = (T508Access)3`. M37 and M41 are fixed.
- `T508PositionalInitQuery` is `new(name, note)`. `--Name " ada "` prints `name:ada|note:n`. Omitting `--Name` exits 1. M38 is fixed.
- `T508PositionalDefaultQuery` is `new(name, count, mode)`. Omitting `--Count` and `--Mode` prints `count:5|mode:Slow`. Capabilities show `Count` as not required with default `5`. M39 is fixed.
- generator-55: an unbound `[NuruRoute]` constructor parameter is NURU_H009 and is not constructed (M40). An all-optional constructor emits `new()` (M44). An abstract `[Operation]` message contains "it is abstract" (M43).
- Capabilities for `T508Access.Read | T508Access.Write` and `(T508Access)3` are `Read, Write`. `T508Shared.Color` has no `defaultValue`. `float` NaN and infinity are `NaN` and `Infinity`. M45 and M46 are fixed.
- `operation-endpoint-derivation.cs`: 35 passed. `generator-55-missing-operation-handler.cs`: 12 passed. The earlier derivation cases in that same run still pass, so M20 and M22–M32 are not regressed by this delta.

M42 is the supplied-value assertions in `Should_use_a_supplied_value_over_an_initializer_the_generator_cannot_copy` (`delay:5`, `extra:5`). M47 is `PrimeSharedAsync` plus `*-per-call-ns` in `operation-dispatch-overhead.cs`, named as the startup comparison in the changelog. M48 is the runtime-DI sentence in `neutral-contracts.md`. M49 is the acceptance bullet that says invoking the route exits 1. The docs cover `init` members, constructor binding, and NURU_H009. The changelog `### Added` lists NURU_H009.

Full CI and the AOT cold-start script were not re-run in this round. The implementer reported multi-mode 1927 total, 1921 passed, 6 skipped, 0 failed, and standalone 30 files, 0 failed.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 11 | 0 |
| suggestion | 0 | 8 | 0 |
| nit | 0 | 10 | 0 |

Counts are the round-2 and round-3 ledger (M20, M22–M49). Round-1 IDs M1–M19 and M21 stay fixed. No new IDs.

## Prior findings (round 3)

| ID | Status | Evidence |
|----|--------|----------|
| M33 | fixed | One `__commandDefaults` per path in generated `T508InitManyQuery`. `= null!` is `label = null!`. Derivation test `Should_keep_or_replace_many_init_only_initializers` passed. |
| M34 | fixed | Required members are always in the object initializer. `Should_always_set_a_required_member_with_an_initializer_the_generator_cannot_copy` passed. |
| M35 | fixed | Exit 1 is emitted before the behavior pipeline. The behavior-plus-external-handler generator-55 case passed. |
| M36 | fixed | Exact-first `OptionBindingMatcher`. Generated `t508case` and `Should_bind_a_mixed_case_option_pair_with_a_non_copyable_initializer` passed. |
| M37 | fixed | Nullable enum `= null` stays null on argv and on `--json-args {}`. |
| M38 | fixed | `= Name.Trim()` is not copied. Omission exits 1. A supplied padded name prints `ada`. |
| M39 | fixed | Primary-constructor defaults `Count = 5` and `Mode = Slow` are the option defaults. |
| M40 | fixed | Unbound constructor parameter on `[NuruRoute]` is NURU_H009. No `__command` is emitted. |
| M41 | fixed | Omitted `Preset` stays `Slow` and omitted `Combined` stays `(T508Access)3`. |
| M42 | fixed | Supplied `--Delay 00:00:05` and `--Extra 5` print `delay:5` and `extra:5`. |
| M43 | fixed | Docs, changelog `### Added`, and the NURU_H009 reason text. Abstract type asserts "it is abstract". |
| M44 | fixed | All-optional constructor emits `T508OptionalCtorQuery __command = new()`. |
| M45 | fixed | Capabilities show `Read, Write` and omit `Shared`. |
| M46 | fixed | `soft:nan|rise:inf`. Capabilities show `NaN` and `Infinity`. |
| M47 | fixed | `PrimeSharedAsync` runs first. Per-call fields are nanoseconds. Changelog names the AOT cold start as the startup comparison. |
| M48 | fixed | `neutral-contracts.md` says `UseMicrosoftDependencyInjection()` resolves `IMediator` from the container and throws unless one is registered. |
| M49 | fixed | The acceptance bullet says invoking the route exits 1. |

## Issues

None.

## Duplicates / conflicts

No new findings. M33–M49 stay the round-3 IDs, all fixed.

## Verdict

0 findings are open. Disposition is clean.
