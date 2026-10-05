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

- [ ] `DateTimeOffset` and `Version` parameters emit a TryParse (or equivalent) and declare the variable the handler uses
- [ ] Typed catch-all and repeated options use the same non-throwing conversion as scalar parameters, including `uint` and `TimeSpan`
- [ ] Overflow on `{*ids:int}` writes the invalid-value error and returns 1, and does not escape as `OverflowException`
- [ ] Add generator tests that compile `{*values:uint}`, a `DateTimeOffset` endpoint parameter, and a `Version` parameter

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~CatchAll
```

Expect: The new generator tests compile. `{*values:uint}` is not assigned from a raw string. An out-of-range int catch-all exits 1 with the invalid-value line.

## Session

- Created: 529644 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
