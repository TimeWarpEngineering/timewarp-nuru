# Fix NURU H002 False Positive On Named Arguments

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Description

`source/timewarp-nuru-analyzers/validation/handler-validator.cs` — the NURU_H002 closure
detectors (`DetectClosures` lambda path ~:258, `DetectAnonymousMethodClosures` ~:410)
walked every IdentifierNameSyntax in the handler body but did not skip identifiers that
are the *name* of a `NameColonSyntax` or `NameEqualsSyntax`. For
`(string name) => Console.WriteLine(format: name)`, the identifier `format` resolves to
WriteLine's parameter symbol → treated as a captured outer variable → false
**NURU_H002 (Error)** on valid code, blocking the user's build.

## Checklist

- [x] Fix lambda closure-detection path (NameColon + NameEquals skips)
- [x] Fix anonymous-method path (same skips)
- [x] Test: handler using a named argument produces no NURU_H002
- [x] Test: genuine capture still reports NURU_H002
- [x] `ganda runfile cache --clear` + run CI tests

## Results

- Fix: both detectors now skip identifiers that are the name of a `NameColonSyntax`
  (named arguments AND property-pattern names like `s is { Length: > 0 }`, which hit the
  same hole) or a `NameEqualsSyntax` (anonymous-type members `new { Tag = x }`, attribute
  named arguments).
- Regression test: `tests/timewarp-nuru-tests/generator/generator-29-h002-named-arguments.cs`
  (GeneratorDriver-hosted, standalone CI phase like generator-28): named argument,
  property pattern, anonymous-type member → no H002; genuine capture → H002 still fires.
- Bug-first verification: against the unfixed validator, all three false-positive cases
  reported H002 (3 failing assertions); with the fix, 4/4 pass.
- Full CI: multi-mode green + both standalone Roslyn-hosted tests 4/4, exit 0.

## Follow-up discovered (filed in 454-028 notes)

DetectClosures walks `lambda.Body.DescendantNodes()`, which EXCLUDES the body node
itself — so a handler whose body is a lone captured identifier (`() => greeting`) is a
false NEGATIVE: no H002 is reported. This may be accidentally load-bearing (the DSL
interpreter can resolve and inline simple constant captures), so widening detection needs
its own analysis rather than a drive-by change here.

## Session

- Created: 2026-07-06 (full-repo review session)
- Implementation: 2026-07-06 (same session)
