# Add CLI tool equivalent to Get-PSReadLineKeyHandler

## Description

Create a CLI tool that displays key bindings and their associated handler functions, similar to PowerShell's `Get-PSReadLineKeyHandler`. This tool should list all available keyboard shortcuts, show what each key combination does, and optionally display details about the function that handles each key.

## Checklist

- [x] Research Get-PSReadLineKeyHandler functionality and output format
- [x] Design the CLI command structure and options
- [x] Implement key binding enumeration
- [x] Add output formatting (table, detailed view)
- [x] Support filtering by key chord or function name
- [x] Add unit tests
- [x] Document the new CLI tool

## Notes

Get-PSReadLineKeyHandler is a PowerShell cmdlet that returns information about keyboard shortcuts used by PSReadLine. It shows:
- Key (the key chord, e.g., "Ctrl+a", "Alt+b")
- Function (what the key does, e.g., "BeginningOfLine", "DeleteChar")
- Description (what the function does)

The Nuru equivalent should provide similar functionality for TimeWarp.Nuru's key handling system.

### Reference Output Format

The PowerShell cmdlet organizes bindings by **function category**:

```
Basic editing functions
=======================
Key              Function            Description
---              --------            -----------
Enter            AcceptLine          Accept the input or move to the next line...
Backspace        BackwardDeleteChar  Delete the character before the cursor
Ctrl+C           Copy                Copy selected region to the system clipboard...

Cursor movement functions
=========================
LeftArrow       BackwardChar        Move the cursor back one character
Home            BeginningOfLine     Move the cursor to the beginning of the line

History functions
=================
DownArrow       NextHistory         Replace the input with the next item in the history
UpArrow         PreviousHistory     Replace the input with the previous item in the history

Completion functions
====================
Tab             TabCompleteNext     Complete the input using the next completion

Miscellaneous functions
=======================
Ctrl+Alt+?      ShowKeyBindings     Show all key bindings
Alt+?           WhatIsKey           Show the key binding for the next chord entered
```

### Key Design Considerations

- **Categorized output**: Bindings are grouped by function type (editing, cursor movement, history, completion, etc.)
- **Three-column format**: Key | Function | Description
- **Key chord format**: Uses standard notation like `Ctrl+`, `Shift+`, `Alt+`, `Enter`, `Tab`, arrow keys
- **Possible Nuru command name**: `nuru key-handler`, `nuru show-keys`, or `nuru key-bindings`

### Analysis Report

A detailed feature comparison has been completed:

- **Report:** [2026-01-17T14-30-00_nuru-vs-psreadline-keyhandler-comparison.md](../../../../.agent/workspace/2026-01-17T14-30-00_nuru-vs-psreadline-keyhandler-comparison.md)
- **Key Finding:** Nuru implements ~81-84% of PSReadLine functions (52/62 unique functions)
- **Coverage by Category:**
  - History: 100%
  - Completion: 100%
  - Search: 100%
  - Selection: 88%
  - Basic Editing: 89%
  - Cursor Movement: 86%
  - Miscellaneous: 33-75%
  - Prediction: 0% (not supported)

The report identifies high-priority features to implement, including this CLI tool itself (`ShowKeyBindings`), as well as `GotoBrace`, `WhatIsKey`, and `InsertLineAbove/Below`.

### External Editor Support (ViEditVisually)

PowerShell PSReadLine includes an external editor function for full-screen editing:

- **Documentation:** [about_PSReadLine_Functions](https://learn.microsoft.com/en-us/powershell/module/psreadline/about/about_psreadline_functions?view=powershell-7.5#vieditvisually)
- **Function:** `ViEditVisually` - Opens the current command line in an external editor (reads `$EDITOR` or `$VISUAL` environment variables)
- **Key Binding:** `Escape` + `v` (in Vi mode)
- **Nuru Status:** Not implemented

This is equivalent to bash's `edit-and-execute-command` (Ctrl+x Ctrl+e) and zsh's `edit-command-line`. Implementing this would require:
1. Writing the current input buffer to a temporary file
2. Launching the user's preferred editor (`$EDITOR`/`$VISUAL`)
3. Reading the modified content back into the input buffer

### Comprehensive Function Comparison Report

A detailed analysis against the complete PSReadLine function reference has been completed:

- **Report:** [2026-01-17T17-45-00_nuru-vs-psreadline-functions-comparison.md](../../../../.agent/workspace/2026-01-17T17-45-00_nuru-vs-psreadline-functions-comparison.md)
- **Reference:** [about_PSReadLine_Functions](https://learn.microsoft.com/en-us/powershell/module/psreadline/about/about_psreadline_functions?view=powershell-7.5)
- **Key Findings:**
  - Nuru implements ~54% of practical PSReadLine functions (51/94 core functions)
  - Vi-specific commands (~70 functions) are not implemented
  - Categories with 100% coverage: Completion (7/7), Search (2/2)
  - Categories with 0% coverage: Prediction (0/6), Display/Scroll (0/10)
- **Recommendations:**
  - High Priority: `ShowKeyBindings`, `GotoBrace`, `ViEditVisually`, `WhatIsKey`
  - Medium Priority: `ClearHistory`, `ShowCommandHelp`, `Shell*Word` functions
  - Low Priority: Full Vi mode expansion, prediction functions

The listing command is `key-bindings` (REPL and `tools/nuru-key-bindings`). It prints the four built-in catalogs. A custom or JSON profile is not expanded. `GotoBrace`, `WhatIsKey`, and `ViEditVisually` stay out of this task.

## Session

- Implementer: grok session 01a10099-8707-7d03-90a7-fe9c3f294943 (2026-10-03)
- Review oracle: claude-opus-5-5 (2026-10-03). The general reviewer was a Claude subagent. Effort 3.
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-03T07:53:21Z

## Results

`key-bindings` lists built-in REPL shortcuts the way `Get-PSReadLineKeyHandler` does: category sections and a Key, Function, Description table. `--detailed` prints one block per binding. `--key` and `--function` filter. `--profile` selects Default, Emacs, Vi, or VSCode.

The REPL command uses the active profile name when `--profile` is omitted. A positional built-in name (`key-bindings Emacs`) does the same in the REPL and in the tool. With no `--profile`, the REPL uses the profile the line reader resolved, including one loaded from JSON config, and reports an error for a non-built-in name. `tools/nuru-key-bindings` is the same listing outside the REPL. A bare run lists Default. An unknown profile exits 1.

Bindings are delegates, so `KeyBindingCatalog` is the display table. A parity test checks every built-in chord against the live profile and, for method-group handlers, against `KeyBindingActionRegistry`.

### Files

- `source/timewarp-nuru/repl/key-bindings/key-binding-catalog.cs`
- `source/timewarp-nuru/repl/key-bindings/key-binding-catalog.assignments.cs`
- `source/timewarp-nuru/repl/key-bindings/key-binding-row.cs`
- `source/timewarp-nuru/repl/repl-commands.cs`
- `source/timewarp-nuru/repl/repl-session.cs`
- `source/timewarp-nuru/help/help-options.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs`
- `tools/nuru-key-bindings/`
- `tests/timewarp-nuru-tests/repl/repl-47-key-binding-catalog.cs`
- `documentation/user/tools/key-bindings.md` and the REPL key-binding docs
- `changelog.md` (Unreleased)

### Decisions

- Command name is `key-bindings`, not `show-keys` or `key-handler`.
- Custom and JSON profiles are named and rejected when the name is not a built-in catalog. Their chords are not printed.
- `GotoBrace`, `WhatIsKey`, `ViEditVisually`, prediction, and full Vi mode are not part of this task.

### Tests

`dotnet run tests/timewarp-nuru-tests/repl/repl-47-key-binding-catalog.cs` — 18 passed, 0 failed (includes the JSON-profile regression test from review M1). `dotnet run tests/ci-tests/run-ci-tests.cs`, after clearing the runfile cache, passed: 3738 tests, 0 failed.

CLI smoke: Default table, Emacs `BeginningOfLine` filter, `Ctrl+a --detailed`, Vi `LeftArrow`, `--help`, and `--profile Mine` (exit 1, message names the four profiles).

### How to validate

**Smoke**

```bash
dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs -- --key Ctrl+a --detailed
dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs -- --profile Emacs --function BeginningOfLine
dotnet run tools/nuru-key-bindings/nuru-key-bindings.cs -- --profile Mine; echo "exit=$?"
```

**Expect**

- The detailed run prints `Profile: Default`, `Ctrl+a`, `Function: BeginningOfLine`, and `Move the cursor to the beginning of the line.`
- The Emacs filter prints `Profile: Emacs` and one `BeginningOfLine` row for `Ctrl+a`.
- `--profile Mine` prints `Unknown key binding profile: 'Mine'. Valid profiles are: Default, Emacs, Vi, VSCode.` and the shell `exit` line is `exit=1`.

**Automated gate**

```bash
dotnet run tests/timewarp-nuru-tests/repl/repl-47-key-binding-catalog.cs
```

Expect 18 passed, 0 failed. The run checks catalog parity with Default, Emacs, Vi, and VSCode, filters, the REPL command, and the `key-bindings` row in CLI help when `ShowReplCommandsInCli` is true.

**Not in scope:** `GotoBrace`, `WhatIsKey`, `ViEditVisually`, prediction, and expanding a custom or JSON profile into chords.

### Review

- **Rounds:** 1. **Effort:** 3. **Roster:** general.
- **Final counts:** 2 bugs fixed, 2 suggestions fixed, 2 nits wontfix. 0 open.
- **Disposition:** `accepted-exceptions`.
  - M5: the tool's unknown-profile message stays on stdout by design, and exit code 1 is the signal.
  - M6: flag values that start with `-` stay rejected, so a value cannot swallow the next flag.
- **Fixes:**
  - M1: the REPL listing now follows the profile the reader resolved, including JSON config.
  - M2: the tool accepts a positional profile name.
  - M3: the changelog notes that the built-in shadows a same-named app route.
  - M4: the catalog table is validated once.
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
