# Fix Help Emitter Brace Escaping

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Description

`source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs:60` emits the app
description into a *generated interpolated string*:
`terminal.WriteLine($"  {EscapeString(model.Description)}");` — but `EscapeString` does
not escape `{`/`}`. A description containing braces (likely, since users describe route
syntax like "greet {name}") produces generated code such as
`terminal.WriteLine($"  greet {name}");` → CS0103, the generated code does not compile.

The inner `$` interpolation is unnecessary since the value is baked in at generation time.

## Requirements

- Either drop the interpolation in the emitted line (emit a plain string literal) or
  escape `{` → `{{` and `}` → `}}` when emitting into interpolated strings.
- Audit the other emitters for the same pattern (six duplicated EscapeString helpers
  exist; see sibling task 454-028 for consolidation).

## Checklist

- [ ] Fix help-emitter.cs:60 emission
- [ ] Grep all emitters for `$"` emission with interpolated user-controlled strings
- [ ] Add generator test: app/route description containing `{name}` compiles and renders
- [ ] `ganda runfile cache --clear` + run CI tests
