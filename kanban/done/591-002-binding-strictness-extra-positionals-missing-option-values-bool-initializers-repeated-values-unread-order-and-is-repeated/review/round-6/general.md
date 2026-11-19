# Round 6 — general
**Date:** 2026-11-20
**Scope reviewed:** fix pass `59102fad..1d29674c` (commit `1d29674c`, the M9/M10 fix). Reviewer: review oracle,
Claude Opus 5.5, effort 2. Gates were not run. This review reads the diff and the `## Results` record.

## Summary

The fix adds `TakesOneValueBeforeCatchAll`. A repeated option whose element is accepted without a conversion check
(`accepts is null`) emits `if` instead of `while`, so one flag takes one value. That happens when a catch-all
follows and no `--` comes before it. The emitted body has no `break` when `accepts` is null, so the `if` form
compiles and the greedy-run tracking still records the single value for the give-back. routing-45 adds five cases.
They cover the fluent-docker-run sample, a repeated `--env`, typed `-p 80 443`, `--env? {e}* {*cmd}`, and the `--`
route. The migration guide (table and checklist item 3) and the migrate skill state the rule. The gate record has a
cache clear and both CI phases. M9 and M10 are fixed.

To make the new cases fit, the fix squeezed routing-45 under the repo's 500-line cap (task 581) instead of splitting
it. That is a new finding.

## Issues

### Issue 1 — Severity: bug
- File: tests/timewarp-nuru-tests/routing/routing-45-binding-strictness.cs (whole file; 499 lines before and after)
- Description: The implementer removed blank lines from about 20 existing tests and from the command classes so that
  five new tests fit under the 500-line cap. The Session line says so. The new tests also put each builder chain
  (`.Map(...).WithHandler(...).AsCommand().Done()`) on one line of more than 200 characters. The only effect of these
  edits is that the file-size check passes. The file has not been split, and splitting is what the cap asks for.
  Task 581 split oversized files for this reason. This is the "silencing the check" form. The check is the 500-line
  file cap.
- Suggestion: Restore the blank lines. Move the repeated-option cases (greedy runs, give-back, catch-all, endpoint
  collection) into a new runfile such as `routing-46-repeated-option-binding.cs`. Use the same `#if !JARIBU_MULTI`
  entry point and a distinct test class. Break the long builder chains over several lines, as in the rest of the
  routing tests. Record a `dotnet run --file` count for both files.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.option-parsing.cs:283;
  documentation/user/guides/migrating-from-system-commandline.md:226 and checklist item 3;
  skills/tw-nuru-migrate-system-commandline/SKILL.md:322
- Description: The rule depends on `accepts is null`. That covers a string element, and also an element type that
  has no built-in conversion (an enum, a custom converter type, `Uri`, `FileInfo`). This is what
  `RepeatedElementAccepts` documents. The XML summary, the guide, and the skill say "a string repeated option takes
  one value" and "a typed repeated option still takes every token that converts". A repeated enum or custom-converter
  option that comes before a catch-all takes one value per flag, which those sentences do not say. The behaviour is
  the safe one. Only the wording is off.
- Suggestion: Say "a string element, or one with no built-in conversion" in the summary, and say the same in the
  guide and the skill (for example "a string or enum repeated option"). Do this in the same fix pass as Issue 1.
- Status: open
