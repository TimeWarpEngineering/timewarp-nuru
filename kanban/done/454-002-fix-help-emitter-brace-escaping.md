# Fix Help Emitter Brace Escaping

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Description

`source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs:60` emitted the app
description into a *generated interpolated string*
(`terminal.WriteLine($"  {EscapeString(model.Description)}");`) but `EscapeString` does
not escape `{`/`}`. A description containing braces (likely, since users describe route
syntax like "greet {name}") produced generated code such as
`terminal.WriteLine($"  greet {name}");` → CS0103, the generated code did not compile.

The inner `$` interpolation was unnecessary since the value is baked in at generation time.

## Checklist

- [x] Fix help-emitter.cs:60 emission (dropped the emitted `$` — plain string literal)
- [x] Also fixed help-emitter.cs:53: version was emitted into an interpolated string with
      NO escaping at all — now a plain literal via EscapeString
- [x] Grep all emitters for `$"` emission with interpolated user-controlled strings
- [x] Add generator test: descriptions containing `{name}`, quotes, backslashes compile and render
- [x] `ganda runfile cache --clear` + run CI tests

## Results

- Bug reproduced before fixing: with the old emitter, the new test failed to build with
  `NuruGenerated.g.cs(639,49): error CS0103: The name 'name' does not exist` — exactly
  the reported failure mode.
- Fix: both emitted lines are now plain (non-interpolated) string literals; EscapeString
  handles `\`, `"`, and newlines. No brace escaping needed once interpolation is gone.
- Regression test: `tests/timewarp-nuru-tests/help/help-07-description-special-chars.cs`
  (3 tests: braces in app description, quotes/backslashes in app description, braces in
  route description). Passes standalone and in CI (auto-included by glob).
- Emitter audit: route-help-emitter emits descriptions as plain literals (safe);
  check-updates/interceptor emitters interpolate only runtime variables (intentional);
  route-matcher-emitter interpolates param/option names, which the pattern lexer restricts
  to brace/quote-free tokens (safe today — EscapeString consolidation tracked in 454-028).
- Full CI: 1274 tests, 1267 passed, 0 failed, 7 pre-existing skips.

## Session

- Created: 2026-07-06 (full-repo review session)
- Implementation: 2026-07-06 (same session)
