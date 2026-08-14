# Escape U+0085 U+2028 U+2029 in generated string literals

## Description

`EmitterStringUtils.EscapeForStringLiteral`
(source/timewarp-nuru-analyzers/generators/emitters/emitter-string-utils.cs) escapes
`\\ " \n \r \t` but not the other characters the C# lexer treats as new-lines inside a string
literal: U+0085 (NEL), U+2028 (LINE SEPARATOR), U+2029 (PARAGRAPH SEPARATOR). Any user-supplied
description containing one of these (e.g. written as `"a b"` in their source — Roslyn's
`ValueText` hands the generator the raw character) is re-emitted verbatim into generated code,
producing an invalid string literal (CS1010/CS1003) in the generated file.

Exposure is systemic: every route/parameter/option description call site, plus example
command/description text (added by task 464). Surfaced during task 464's review
(kanban/.../464-.../review/round-1/general.md Issue 4).

## Requirements

- `EscapeForStringLiteral` emits `\u0085` / `\u2028` / `\u2029` escape sequences for these
  characters.
- All emitter call sites get the fix automatically (single shared helper — verify no emitter
  bypasses it with raw concatenation of user text).
- Regression test: a route with U+2028 in a description and in a `[NuruRouteExample]` compiles
  and renders (help + capabilities) without generator errors.

## Checklist

- [x] Extend EscapeForStringLiteral with the three characters
- [x] Audit emitters for user-text paths that bypass the helper
- [x] Regression test with U+2028/U+0085 in description + example text
- [x] Full CI test sweep green

## Notes

- Found by task 464 round-1 review; deliberately excluded from 464's scope because the gap
  predates it and spans all description call sites.

### Implementation plan

**Problem:** `EmitterStringUtils.EscapeForStringLiteral` escapes `\\ " \n \r \t` but not C#
`New_Line_Character` extras U+0085 / U+2028 / U+2029. Roslyn `ValueText` hands generators the
raw char; re-emitting into a double-quoted literal → CS1010/CS1003.

**Step 1 — Extend helper (only production change expected)**

File: `source/timewarp-nuru-analyzers/generators/emitters/emitter-string-utils.cs`

Keep chained `.Replace(..., StringComparison.Ordinal)`. Append **after** existing replaces
(order matters — new escapes introduce `\`):

```csharp
.Replace("\u0085", "\\u0085", StringComparison.Ordinal)
.Replace("\u2028", "\\u2028", StringComparison.Ordinal)
.Replace("\u2029", "\\u2029", StringComparison.Ordinal)
```

Replacement must be the six characters `\u2028` in the returned string (not a re-inserted raw
char). Update XML doc. No API surface change (`internal static`).

**Step 2 — Audit emitter bypasses**

All description / example / free-text embeds already go through `EscapeForStringLiteral` in
help, route-help, capabilities, completion, repl, behavior, route-matcher, telemetry, version
emitters — inherit fix automatically.

Residual non-escaped embeds (identifiers / fixed forms, out of scope unless audit finds free
text): enum member names, option long/short forms, param names/type constraints in
completion/repl, config section keys in service-resolver. Document residual; do not expand
scope.

**Step 3 — Regression tests**

New file: `tests/timewarp-nuru-tests/help/help-09-unicode-newline-escapes.cs` (pattern: help-07/08).

- Unique `h09-` prefixes.
- Real Unicode via `\uXXXX` in source.
- Test A: fluent route description with U+2028 → `--help` exit 0 + output contains raw char.
- Test B: `[NuruRoute]` + `[NuruRouteExample]` with U+2028/U+0085/U+2029 → route `--help` and
  `--capabilities` (deserialize JSON; assert string properties, do not match raw JSON escapes).
- Optional Test C: fluent `.WithExample` with unicode newlines.
- Do not unit-test internal helper directly.

**Step 4 — Validation**

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/help/help-09-unicode-newline-escapes.cs
dotnet run tests/timewarp-nuru-tests/help/help-07-description-special-chars.cs
dotnet run tests/timewarp-nuru-tests/help/help-08-route-examples.cs
dotnet run tests/timewarp-nuru-tests/capabilities/capabilities-06-examples.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

**Out of scope:** identifier embeds in completion/repl; `\0`/other controls; display of U+2028.

## Session

- Created: 0f730c83-90e5-4a4c-8bb2-3020fdd469d6 (2026-08-13)
- Orchestration: grok (2026-08-14) — full phases 1–5
- Plan: 01a0004d-2aa7-75c2-a642-81959f7a0d82
- Implementer: 01a0004f-dbd3-7613-b8ff-a2242d74e823
- Review (round 1 general): 01a00053-acfa-7672-a54c-0825bd4304a0

## Results

### What was implemented

Extended `EmitterStringUtils.EscapeForStringLiteral` so U+0085 (NEL), U+2028 (LINE SEPARATOR),
and U+2029 (PARAGRAPH SEPARATOR) are escaped as `\u0085` / `\u2028` / `\u2029` before embedding
into generated C# string literals. All free-text emitter call sites inherit the fix via the
shared helper. Added Jaribu regression tests covering fluent and Endpoint DSL paths for
`--help` and `--capabilities`.

### Files changed

| File | Change |
|------|--------|
| `source/timewarp-nuru-analyzers/generators/emitters/emitter-string-utils.cs` | Three Replace calls + XML doc |
| `tests/timewarp-nuru-tests/help/help-09-unicode-newline-escapes.cs` | New regression (4 tests) |

### Key decisions / deviations

- Single production change on the shared helper; no per-emitter edits.
- Residual unescaped identifier embeds (enum members, option forms, param names, config
  section keys) left out of scope — not free-text description/example paths.
- No deviations from the written plan.

### Test outcomes

| Command | Result |
|---------|--------|
| `dotnet run tests/timewarp-nuru-tests/help/help-09-unicode-newline-escapes.cs` | 4/4 pass |
| `dotnet run tests/timewarp-nuru-tests/help/help-07-description-special-chars.cs` | 3/3 pass |
| `dotnet run tests/timewarp-nuru-tests/help/help-08-route-examples.cs` | 8/8 pass |
| `dotnet run tests/timewarp-nuru-tests/capabilities/capabilities-06-examples.cs` | 4/4 pass |
| `dotnet run tests/ci-tests/run-ci-tests.cs` | 1593 passed, 7 skipped, 0 failed |

### Review (Phase 4b)

- **Effort:** 1 (general only)
- **Rounds:** 1
- **Final counts:** 0 open / 0 fixed / 0 wontfix across bug/suggestion/nit
- **Disposition:** `clean` — `review/disposition.md`
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`

### How to validate

**Smoke**

```bash
cd /path/to/timewarp-nuru
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/help/help-09-unicode-newline-escapes.cs
```

**Expect**

- All 4 tests pass.
- Successful run proves the generator produced compilable code for descriptions/examples
  containing U+0085/U+2028/U+2029 and that `--help` / `--capabilities` retain the raw characters
  at runtime.

**Automated gate**

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/help/help-09-unicode-newline-escapes.cs
dotnet run tests/timewarp-nuru-tests/help/help-07-description-special-chars.cs
dotnet run tests/timewarp-nuru-tests/help/help-08-route-examples.cs
dotnet run tests/timewarp-nuru-tests/capabilities/capabilities-06-examples.cs
dotnet run tests/ci-tests/run-ci-tests.cs
```

**Depends on:** analyzer rebuild (clear runfile cache after analyzer source changes).

**Not in scope:** hardening identifier embeds in completion/repl; escaping other control chars;
display/layout of U+2028 in terminals.
