# Round 1 — general
**Date:** 2026-09-29
**Scope reviewed:** branch `task/435-missing-newline-between-options-table-and-commands` vs `origin/master` — `HelpEmitter.EmitCommands` and `Should_separate_options_table_from_commands_heading`.

## Summary

`Emit` still writes header, usage, options, then commands. `EmitCommands` returns immediately when there are no visible routes and REPL commands are hidden, so help with neither section does not gain a trailing blank line. When a section will be shown, it emits one `terminal.WriteLine()` before the first heading. Later groups and the REPL block keep their existing blank line only when a previous heading was already written, so the new line does not stack with those. Generated `PrintHelp_2` is `WriteTable` for options, then `WriteLine()`, then `Commands:`. Generated `PrintHelp_9` (group-only routes) is the same blank line before `config:`. Per-route help already inserts `WriteLine()` before Parameters, Options, and Examples; that path is unchanged. Re-ran `help-02-table-formatting.cs`: 10 passed, including `Should_separate_options_table_from_commands_heading`. No bugs, suggestions, or nits.

## Issues

<!-- none -->
