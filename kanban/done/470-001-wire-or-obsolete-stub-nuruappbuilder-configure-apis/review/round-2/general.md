# Round 2 — general
**Date:** 2026-09-22
**Scope reviewed:** M1 fix delta (IsPerCommandHelpRoute LongForm-only + help-10 --helper regression)

## Summary

M1 is fixed. `IsPerCommandHelpRoute` no longer uses substring `Contains("--help")`; it classifies only `OptionDefinition` segments with `LongForm is "help"` and `!ExpectsValue`. The new `Should_list_helper_option_that_is_not_a_help_route` regression maps `h10-run --helper` and asserts it appears in default `--help`, while `Should_hide_per_command_help_rows_by_default` still asserts synthetic `h10-status --help` rows stay hidden. Documented `blog --help?` user routes remain classified as per-command help because the parser stores LongForm `"help"` with no value parameter (`ExpectsValue` false; `?` is a separate optional token). No new issues in the fix delta.

## Prior findings

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs
- Notes: Re-verified against the uncommitted delta. The Ordinal `OriginalPattern`/`FullPattern` `Contains("--help")` early-return is gone. Remaining check is `segment is OptionDefinition option && !option.ExpectsValue && option.LongForm is "help"`. `--helper` is LongForm `"helper"`, so it is listed; `--help?` is LongForm `"help"` with `ExpectsValue` false, so it is still filtered when `ShowPerCommandHelpRoutes` is false.

## Issues

<!-- new findings only; omit if none -->
