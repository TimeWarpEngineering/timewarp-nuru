# Stop option descriptions at EndOfOptions

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M6). Suggestion/nits folded: M22, M38, M39.

## Description

`ConsumeDescription(stopAtRightBrace: false)` (`parser.cs:180`) stops at DoubleDash / SingleDash / LeftBrace but **not** EndOfOptions. A standalone `--` is skipped, so it never becomes an `EndOfOptionsSyntax` segment.

Verified with `PatternParser.TryParse`:
- `run --flag -- {*args}` → success (baseline)
- `run --flag|desc -- {*args}` → fails (`{*args}` mis-attached as option value)
- `cmd --opt | a -- b {x}` → success with wrong AST (EndOfOptions discarded, description `"a b"`, `{x}` attached as `--opt` value)

Crash-safety: 50k malformed patterns (seed 470001) produced 0 uncaught exceptions; this is a semantic parse bug, not a throw.

Folded:
- M22: parameter names allow hyphens (`{my-param}`) contrary to documented identifier rules
- M38: `InvalidTypeConstraintError.SupportedTypes` omits types `IsBuiltInType` accepts
- M39: `AdjacentParametersError` spans only the `{` token

## Requirements

- Add `RouteTokenType.EndOfOptions` to the option-description stop set.
- Regression tests for both mis-parse shapes.
- Validate parameter names with `IsValidIdentifierFormat` (hyphens stay legal for option names).
- Align SupportedTypes with IsBuiltInType; widen adjacent-parameter span.

## Checklist

- [x] EndOfOptions stop in ConsumeDescription
- [x] Tests for `|desc -- {*args}` and silent-corruption shape
- [x] M22 parameter-name validation
- [x] M38 / M39 error UX
- [x] Preserve no-throw fuzz guarantee

## Notes

Evidence: parent 470 `review/round-1/merged.md` M6, M22, M38, M39. Fuzz note in `review/round-1/parsing.md`.

## Session

- Implementer: Grok session 01a0c764-7616-7f92-8acc-29005b4b41eb (2026-09-22)

## Results

Option descriptions stop at a standalone `--`, so that token stays an end-of-options segment. Parameter names must match `IsValidIdentifierFormat`. Option names may still contain hyphens. Built-in type names come from one list (`BuiltInTypeNames`) used by the parser, `InvalidTypeConstraintError`, and analyzer diagnostic `NURU_P004`. `AdjacentParametersError` covers the whole second parameter.

`cmd --opt | a -- b {x}` is rejected with `InvalidEndOfOptionsSeparatorError`. The separator is kept, and the pattern is invalid because `--` is not followed by a catch-all. It no longer compiles as an option whose description is `"a b"` and whose value is `{x}`.

Accepted built-in spellings are unchanged and ordinal (`uri` and `Uri` both work; `URI` does not). Analyzer converter aliases such as `int32`, `boolean`, and `version` were not added to the parser.

### Files

- `source/timewarp-nuru-parsing/parsing/parser/parser.cs`
- `source/timewarp-nuru-parsing/parsing/parser/parser.segments.cs`
- `source/timewarp-nuru-parsing/parsing/parser/parser.validation.cs`
- `source/timewarp-nuru-parsing/parsing/parser/built-in-type-names.cs`
- `source/timewarp-nuru-parsing/parsing/parser/parse-error.cs`
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.syntax.cs`
- `tests/timewarp-nuru-tests/parser/parser-18-adjacent-parameters.cs`
- `tests/timewarp-nuru-tests/parser/parser-19-option-description-boundaries.cs`
- `documentation/developer/design/parser/route-pattern-anatomy.md`
- `documentation/developer/ubiquitous-language.md`

### Test outcomes

- `parser-19-option-description-boundaries.cs`: 8 passed
- `parser-18-adjacent-parameters.cs`: 6 passed
- `parser-09-end-of-options.cs`: 12 passed
- `parser-13-syntax-errors.cs`: 6 passed
- `parser-15-custom-type-constraints.cs`: 9 passed
- `parser-11-complex-integration.cs`: 7 passed
- Fuzz: 50,000 patterns, seed 470001, `PatternParser.TryParse` plus `PatternParser.Parse` (only `PatternException` expected). ok=6046 fail=43954 uncaught=0

### How to validate

**Smoke**

```bash
dotnet run tests/timewarp-nuru-tests/parser/parser-19-option-description-boundaries.cs
dotnet run tests/timewarp-nuru-tests/parser/parser-18-adjacent-parameters.cs
```

**Expect**

- parser-19: 8 passed, 0 failed.
  - `run --flag|desc -- {*args}` compiles. Description is `desc`, `--flag` does not expect a value, catch-all name is `args`.
  - `cmd --opt | a -- b {x}` fails `TryParse` with `InvalidEndOfOptionsSeparatorError` and no compiled route.
  - `run {my-param}` fails with `InvalidIdentifierError` for `my-param`.
  - `run --my-param {file}` compiles. Match pattern is `--my-param`.
  - `use {value:not-a-type}` lists every built-in spelling, including `byte`, `sbyte`, `char`, `float`, `uri`, and `Uri`.
- parser-18: 6 passed. For `run {a}{b}`, `AdjacentParametersError` is position 7, length 3.

**Automated gate**

```bash
dotnet run tests/timewarp-nuru-tests/parser/parser-19-option-description-boundaries.cs
dotnet run tests/timewarp-nuru-tests/parser/parser-18-adjacent-parameters.cs
dotnet run tests/timewarp-nuru-tests/parser/parser-09-end-of-options.cs
dotnet run tests/timewarp-nuru-tests/parser/parser-13-syntax-errors.cs
dotnet run tests/timewarp-nuru-tests/parser/parser-15-custom-type-constraints.cs
```

**Not in scope**

The original review fuzz harness is not in the repo. The 50k run reused seed 470001 and the same public API, with a metacharacter-biased generator. Success and failure counts will not match the review's 4166/45834. Uncaught exceptions are the guarantee.
