# Fix search version flag and FTS NUL crash

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M11, M12). Suggestion folded: M30.

## Description

`--version` on search (`search-query.cs:9-10,70`) is documented as “Show CLI version in results” but prints `result.Endpoint.Kind`. `SearchResult` has no version field (version lives on `CliInfo` / `clis`).

M12: residual of 454-023. `SanitizeFtsQuery` (`search-index.cs:295-309`) does not strip U+0000. `hello\0world` → FTS5 `unterminated string`; `SearchAsync` does not catch SqliteException. Quote/LIKE fixes from 454-023 remain intact.

M30: `~/.nuru/index.db` is created world-readable (same trust directory as REPL history).

## Requirements

- Join/select `clis.version` onto SearchResult and print it when `--version` is set.
- Strip/reject NUL (and C0 controls) in SanitizeFtsQuery and/or catch SqliteException around MATCH.
- Owner-only mode on the DB and 0700 on `~/.nuru` when creating them (M30).
- Tests (search-01-fts-sanitizer plus version formatter).

## Checklist

- [ ] --version prints CLI version (M11)
- [ ] FTS NUL (M12)
- [ ] index.db file mode (M30)
- [ ] Tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M11, M12, M30.
