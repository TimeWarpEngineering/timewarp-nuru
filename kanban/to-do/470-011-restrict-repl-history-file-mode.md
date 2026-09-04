# Restrict REPL history file mode

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M17). Suggestion folded: M32.

## Description

`ReplHistory.Save()` (`repl-history.cs:187`) uses `File.WriteAllLines` and never sets a restrictive Unix mode. Typical umask 022 → `0644`. Default path is `~/.nuru/history/<app>` with `PersistHistory` default true. Confirmed on the review host: existing `~/.nuru/history/*` entries are `-rw-r--r--`.

M32: default `HistoryIgnorePatterns` miss `Bearer`, `Authorization`, `api_key` / `api-key`, `sk-…`. Combined with world-readable files, missed lines are more exposed. `repl-03b` covers the current defaults only.

## Requirements

- Create/replace the history file with owner-only mode (UnixFileMode.UserRead | UserWrite). chmod `~/.nuru` / `history` to 0700 when creating them.
- On Windows, rely on user-profile ACLs (document).
- Extend default ignore patterns (M32) and add repl-03b cases. Document that ignore patterns are best-effort.
- Assertion on file mode after Save in repl-03b (Unix).

## Checklist

- [ ] Owner-only history file (M17)
- [ ] 0700 on ~/.nuru when creating
- [ ] Ignore pattern coverage (M32)
- [ ] Tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M17, M32. Do not duplicate 454-019.
