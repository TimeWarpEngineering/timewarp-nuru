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

- [ ] Extend EscapeForStringLiteral with the three characters
- [ ] Audit emitters for user-text paths that bypass the helper
- [ ] Regression test with U+2028/U+0085 in description + example text
- [ ] Full CI test sweep green

## Notes

- Found by task 464 round-1 review; deliberately excluded from 464's scope because the gap
  predates it and spans all description call sites.

## Session

- Created: 0f730c83-90e5-4a4c-8bb2-3020fdd469d6 (2026-08-13)
