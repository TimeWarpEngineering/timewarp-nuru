# Round 1 — merged findings
**Date:** 2026-10-03
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 2 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 0 | 2 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/repl/repl-session.cs:243
- Description: The REPL `key-bindings` command takes the active profile from `ReplOptions` only. A profile loaded from `NURU_KEYBINDINGS` or `.nuru/keybindings.json` was never seen, so the command silently printed the Default catalog. The docs say this case prints an error.
- Suggestion: Use the profile the console reader actually resolved.
- Source: general
- Disposition notes: `ReplConsoleReader.ProfileName` exposes the resolved name. `ReplSession` records it on each read and uses it before the `ReplOptions` fallback. New test `Should_report_a_json_profile_loaded_by_the_reader` loads a JSON profile through `NURU_KEYBINDINGS` and checks that the command errors instead of printing Default.

### M2 — Severity: bug — Status: fixed
- File: tools/nuru-key-bindings/nuru-key-bindings.cs:8
- Description: `nuru-key-bindings -- key-bindings Emacs` printed `Profile: Default` and exited 0. The REPL accepts and documents the positional profile name.
- Suggestion: Accept a positional profile name in the tool.
- Source: general
- Disposition notes: `KeyBindingsQuery` gets an optional positional `[Parameter] ProfileName`, which `--profile` overrides. The entry point now forwards every argument list that does not already start with `key-bindings`, so `-- Emacs` works too. An unknown positional name exits 1. Smoke-tested with Emacs, Vi, and Mine. Tool docs and route examples updated.

### M3 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru/repl/repl-session.cs:243
- Description: `key-bindings` is matched by its first word, so it shadows an app route with the same first word inside the REPL. The changelog does not say so.
- Suggestion: Document the precedence.
- Source: general
- Disposition notes: The first-word match is needed for the flags. The changelog entry now says the built-in takes precedence over an app route with the same first word.

### M4 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru/repl/key-bindings/key-binding-catalog.cs:132
- Description: `EnsureTable()` re-checks the whole static table on every `List` call.
- Suggestion: Check once.
- Source: general
- Disposition notes: A `volatile bool TableChecked` is set after the first successful check, so later calls skip the scan. Drift is still caught by the parity tests and by the first call.

### M5 — Severity: nit — Status: wontfix
- File: tools/nuru-key-bindings/endpoints/key-bindings-query.cs:74
- Description: The tool prints the unknown-profile message on stdout. The REPL prints it on stderr.
- Suggestion: Write the message to stderr.
- Source: general
- Disposition notes: By design. The Design region says the query returns the message as its result and sets exit code 1. Exit code 1 is the machine signal. Moving the message to stderr would mean injecting a terminal into the handler for one line. Decided by the review oracle.

### M6 — Severity: nit — Status: wontfix
- File: source/timewarp-nuru/repl/repl-commands.cs:182
- Description: Any flag value that starts with `-` counts as missing, so `--key -` reports "Missing value for --key."
- Suggestion: Accept dash-prefixed values.
- Source: general
- Disposition notes: Accepting dash-prefixed values would make `--key --detailed` swallow the flag. No catalog chord is displayed starting with `-`, and the substring filter still finds `OemMinus`-style text. Decided by the review oracle.

## Duplicates / conflicts

- None.
