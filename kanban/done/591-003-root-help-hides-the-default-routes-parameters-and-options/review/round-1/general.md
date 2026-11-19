# Round 1 — general
**Date:** 2026-11-19
**Scope reviewed:** same as framework (help-emitter.cs, help-11 test, auto-help.md, migration guide item 1, migrate skill step 9 and behavior list, kanban move)

## Summary

`HelpEmitter` now splits visible routes into "promoted" root default routes (no group prefix, no
command literal, and at least one parameter, option, or example) and command routes. Promoted routes
add a usage line, a description, a `Parameters:` table, rows at the top of the `Options:` table, and
an `Examples:` block; they are dropped from `Commands:`. The usage tail becomes `app [options]` when
no other command is visible. Description-only default routes and named commands are unchanged.
Generated strings go through `EscapeForStringLiteral`, and the usage pattern is concatenated rather
than interpolated, so parameter braces are safe. The change matches the goal restatement and the
original task (no misread). Recorded gates cover the change: build pass, help-11 6/6 standalone,
help-01..09 regression runs, audit pass; help-11 is inside the CI `timewarp-nuru-tests/**` glob and
uses `.Map<T>()`, so it is safe in multi-mode. Risk is low; the two findings below are polish.

## Issues

### Issue 1 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs:350
- Description: `FormatOptionDisplay` and the row logic in `EmitPromotedParameters` / `EmitPromotedExamples`
  copy the option-display switch, the `(optional)` description fallback, the parameter Name/Required/Type
  columns, and the example lines from `route-help-emitter.cs:215-305`. The root page and the
  `<command> --help` page can drift if one copy changes.
- Suggestion: move the shared formatting (option display string, parameter row values) into
  `HelpPatternHelper` and call it from both emitters.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs:274
- Description: when an app has more than one promoted root default route (for example `Map("")` with
  options plus `Map("{*args}")`), their parameters are merged into one `Parameters:` table and their
  options into the one `Options:` table with no route attribution; only the stacked usage lines show
  which belongs to which. No test covers this case.
- Suggestion: either group per route or accept it as an edge case; the usage lines already disambiguate.
- Status: open
