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

- [x] Owner-only history file (M17)
- [x] 0700 on ~/.nuru when creating
- [x] Ignore pattern coverage (M32)
- [x] Tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M17, M32. Do not duplicate 454-019.

## Results

`ReplHistory.Save` writes history with Unix mode 0600 (and hardens an existing permissive file on replace). Newly created parent directories are walked segment-by-segment so both `~/.nuru` and `history` get 0700 (leaf-only `CreateDirectory(path, mode)` would leave intermediates at umask). Windows relies on user-profile ACLs (documented). Default `HistoryIgnorePatterns` now include `*api_key*`, `*api-key*`, `*bearer*`, `*authorization*`, `*sk-*`; docs mark ignore patterns as best-effort. `repl-03b` covers the new defaults and asserts directory/file modes after Save on Unix.

### How to validate

**Smoke:** From the claim worktree:

```bash
dotnet -- tests/timewarp-nuru-tests/repl/repl-03b-history-security.cs
dotnet -- tests/timewarp-nuru-tests/repl/repl-04-history-persistence.cs
```

**Expect:** Both files report all passed / 0 failed (repl-03b: 15 tests including `Should_save_history_file_owner_only_on_unix`; repl-04: 8 tests).

### Review disposition

- **Outcome:** clean (0 open)
- **Effort / roster:** 1 — general
- **Rounds:** 1
- **Final counts:** bug/suggestion/nit all 0 open, 0 fixed, 0 wontfix
- **Artifacts:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
- Re-verified `repl-03b` (15 passed) and `repl-04` (8 passed) during review.
