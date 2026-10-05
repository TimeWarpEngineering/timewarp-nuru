# Emit try-parse for DateTimeOffset, Version, and typed catch-all options

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding A-3, R-5 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

Built-in conversions the parser accepts are not emitted, and typed catch-all / repeated options use a partial throwing parser.

`GetClrTypeName` accepts `datetimeoffset` and `version`. `GetTypeConstraintFromClrType` maps `DateTimeOffset` to `datetimeoffset`. `GetBuiltInTryConversion` has neither, so scalar emit writes a warning comment and never declares the converted variable.

`EmitCatchAllTypeConversion` and the repeated-option path call `GetParseExpression` inside `catch (FormatException)`. That switch has `int`, `long`, `short`, `byte`, `double`, `float`, `decimal`, `bool`, `datetime`, and `guid` only. `GetBuiltInTryConversion` also accepts `sbyte`, `ushort`, `uint`, `ulong`, `char`, `TimeSpan`, `DateOnly`, `TimeOnly`, and `IPAddress`. For those, `GetParseExpression` returns the raw string, so the generated array assignment does not compile. For the types it does parse, overflow throws `OverflowException`, which is not `FormatException`.

Evidence: `source/timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs`, `route-matcher-emitter.cs` (`EmitTypeConversions`, `EmitCatchAllTypeConversion`, `GetParseExpression`), `endpoint-extractor.cs` (`GetTypeConstraintFromClrType`). Parent records: `review/analyzers.md` A-3, `review/runtime-core.md` R-5.

## Checklist

- [x] `DateTimeOffset` and `Version` parameters emit a TryParse (or equivalent) and declare the variable the handler uses
- [x] Typed catch-all and repeated options use the same non-throwing conversion as scalar parameters, including `uint` and `TimeSpan`
- [x] Overflow on `{*ids:int}` writes the invalid-value error and returns 1, and does not escape as `OverflowException`
- [x] Add generator tests that compile `{*values:uint}`, a `DateTimeOffset` endpoint parameter, and a `Version` parameter
- [x] Implementation review disposition recorded (round 1, general, clean)

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~CatchAll
```

Expect: The new generator tests compile. `{*values:uint}` is not assigned from a raw string. An out-of-range int catch-all exits 1 with the invalid-value line.

## Results

- `type-conversion-map.cs`: `GetBuiltInTryConversion` now maps `datetimeoffset` (`DateTimeOffset.TryParse`, invariant culture) and `version` (`Version.TryParse`). Scalar emit declares the handler variable for both instead of writing a warning comment.
- `route-matcher-emitter.cs`: removed `GetParseExpression` and the `try { Select(Parse) } catch (FormatException)` pattern. Typed catch-all and repeated options now call one helper, `EmitBuiltInArrayConversion`. It loops over the elements and runs the same `GetBuiltInTryConversion` condition that scalar parameters use. A failed TryParse writes the invalid-value line and returns 1. Overflow is just a failed TryParse, so it cannot escape as `OverflowException`. Every built-in type (`uint`, `TimeSpan`, `IPAddress`, ...) now converts. None are assigned from a raw string. Also dropped the unused `routeIndex` parameter from `EmitCatchAllTypeConversion`.
- Tests: `tests/timewarp-nuru-tests/routing/routing-16-typed-catch-all.cs` has 7 new cases: `{*values:uint}` bind and a negative-value reject, `{*values:TimeSpan}`, `{*ids:int}` overflow exiting 1 with the invalid-value line, a repeated `--id {id:uint}*` bind and reject, a `DateTimeOffset` parameter, and a `Version` parameter bind and reject. 20/20 pass. The full CI suite `tests/ci-tests/run-ci-tests.cs` exits 0.

### Review

- Rounds: 1. Effort 2 (by-diff, 301 lines). Roster: general.
- Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0/0/0; nit 0/0/0. Final open count: 0.
- Disposition: **clean**. No wontfix and no escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- Re-check during review: `dotnet run tests/timewarp-nuru-tests/routing/routing-16-typed-catch-all.cs` passed 20/20.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/routing/routing-16-typed-catch-all.cs
```

Expect: 20/20 pass, including `Should_bind_uint_array_catch_all`, `Should_fail_on_int_overflow_catch_all` (exit 1, `Error: Invalid value in 'ids'. Expected: int[]`), `Should_bind_and_reject_repeated_uint_option`, `Should_bind_datetimeoffset_parameter`, and `Should_bind_version_parameter`. Note: the original smoke command pointed at a `timewarp-nuru-tests.csproj` that does not exist. These tests are runfiles.

## Session

- Created: 529644 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Implemented (claude, ganda task work implement oracle, 2026-10-05)
- Review: grok 01a10ab9-c704-7972-af41-5ba364f25154 (2026-10-05); general reviewer 01a10abf-08e0-7082-9bdd-19b564c14f95

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
- Implementation review: `review/disposition.md` (clean, round 1, general). Live ledger: `review/round-1/merged.md`.
