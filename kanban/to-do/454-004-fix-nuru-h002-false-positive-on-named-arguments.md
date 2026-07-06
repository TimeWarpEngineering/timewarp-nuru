# Fix NURU H002 False Positive On Named Arguments

Parent: 454 (2026-07-06 full code review). Severity: HIGH.

## Description

`source/timewarp-nuru-analyzers/validation/handler-validator.cs:258-310` (lambda path)
and ~:410 (anonymous-method path) — the closure detector walks identifiers but does not
skip the name inside a `NameColon` (named argument). For a valid handler such as
`(string name) => Console.WriteLine(format: name)`, the identifier `format` resolves to
WriteLine's parameter symbol, is treated as a captured variable, and produces a false
**NURU_H002 (Error)** on perfectly valid code — blocking the user's build.

## Requirements

- Skip identifiers that are the `Name` of a `NameColonSyntax` (and consider
  `NameEquals` in attribute/anonymous-object contexts) in both detection paths.
- Prefer semantic checks (symbol kind = Parameter of the invoked method) over syntax
  position if simpler.

## Checklist

- [ ] Fix lambda closure-detection path (~:258-310)
- [ ] Fix anonymous-method path (~:410)
- [ ] Test: handler using a named argument compiles with no NURU_H002
- [ ] Test: genuine capture still reports NURU_H002
- [ ] `ganda runfile cache --clear` + run CI tests
