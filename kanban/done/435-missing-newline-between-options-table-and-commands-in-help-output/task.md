# Missing newline between Options table and Commands in help output

## Description

When running `ganda --help`, there is no blank line between the Options table and the "Commands:" heading. The output looks cramped:

```
Options:
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │
│ --version      │ Show version information       │
│ --capabilities │ Show capabilities for AI tools │
└────────────────┴────────────────────────────────┘
Commands:
```

Expected — a blank line between the table and "Commands:":

```
Options:
┌────────────────┬────────────────────────────────┐
│ --help, -h     │ Show this help message         │
│ --version      │ Show version information       │
│ --capabilities │ Show capabilities for AI tools │
└────────────────┴────────────────────────────────┘

Commands:
```

## Checklist

- [x] Locate the help emitter code that renders the Options and Commands sections
- [x] Add a newline after the Options table before the Commands heading
- [x] Verify fix with `ganda --help`

## Results

### Root cause

`HelpEmitter.Emit` writes the Options table and then the Commands section with no blank line between them. `EmitUsage` already ends with `terminal.WriteLine()`, so Version, Description, and Usage stay separated, but `EmitCommands` started the first heading on the next line after the table border.

### Fix

`HelpEmitter.EmitCommands` emits `terminal.WriteLine()` after it decides a Commands or REPL section will be shown, and before the first heading. Help that has neither section is unchanged. Later groups still get the existing blank line between headings.

### Tests

`Should_separate_options_table_from_commands_heading` in `tests/timewarp-nuru-tests/help/help-02-table-formatting.cs` asserts the options-table border (`┘`) is followed by a blank line and then `Commands:`. That file is 10/10, including in the CI multi-mode assembly (`HelpTableFormatting` 10 passed).

`dotnet build timewarp-nuru.slnx` succeeded with 0 warnings. `dotnet run tests/ci-tests/run-ci-tests.cs` exited 0: multi-mode total 1762, passed 1755, skipped 7, and every standalone phase passed. `ganda repo audit` passes (29/29) after `ganda repo audit --fix` recreated the gitignored `bin/dev`.

Installed `ganda --help` still uses the published TimeWarp.Nuru package, so this worktree cannot change that binary. The test exercises the same generated `PrintHelp` path that `ganda --help` renders.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/help/help-02-table-formatting.cs
```

Expect: exit code 0, `Total: 10`, `Passed: 10`. `Should_separate_options_table_from_commands_heading` fails if the options table border is not followed by a blank line before `Commands:`.

### Review disposition

- **Outcome:** clean
- **Effort / roster:** 1 — general only
- **Rounds:** 1
- **Final counts:** bug 0, suggestion 0, nit 0 (0 open, 0 fixed, 0 wontfix)
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
- Re-verified during review: `dotnet run tests/timewarp-nuru-tests/help/help-02-table-formatting.cs` — Total 10, Passed 10. Generated `PrintHelp_2` writes `terminal.WriteLine()` between the Options `WriteTable` and `Commands:`. Generated `PrintHelp_9` writes that same line before the first group heading. `EmitCommands` still returns before that line when there is no Commands or REPL section.

## Notes

- Observed in ganda v1.0.0-beta.20
- Likely in the generated `PrintHelp()` method from `help-emitter.cs`
- 2026-09-29: review oracle (ganda task work, tw-implementation-review effort 1). Round 1 general — disposition clean. Next host nodes: open-pr / done (no apply-review sibling).

## Session

- Review: Cursor implementer-cursor session review-oracle (2026-09-29)

