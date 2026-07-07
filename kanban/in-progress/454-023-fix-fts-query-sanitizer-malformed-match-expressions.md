# Fix FTS Query Sanitizer Malformed Match Expressions

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M23).

## Description

`source/timewarp-nuru-search/services/search-index.cs:290-314` (`SanitizeFtsQuery`;
exception thrown at ~:268) — the sanitizer doubles parentheses (`(` → `((`) and strips
`*`, but never wraps tokens as FTS5 quoted strings. The result feeds
`endpoints_fts MATCH $query`.

Concrete failures (verified logic-level):
- searching `(` yields `((*` → unbalanced-paren FTS5 syntax error
- searching `***` strips all tokens → `MATCH ''` → syntax error

Both raise an uncaught `SqliteException` from ExecuteReaderAsync, so a plain search
string crashes the command with a stack trace instead of returning "No results found."

This is a robustness bug, not injection — the value is properly parameterized.

Also LOW (same file, ~:254-255): `e.group_path LIKE $groupPath || '%'` binds `--group`
input without escaping `%`/`_` — over-matches, no injection.

## Requirements

- Correct FTS5 escaping: wrap each token in double quotes (doubling internal quotes),
  append prefix `*` OUTSIDE the quotes; return no-results early for an empty token list.
- Escape LIKE wildcards in the group filter (ESCAPE clause).

## Checklist

- [ ] Rewrite SanitizeFtsQuery with proper FTS5 token quoting
- [ ] Empty-token-list → graceful "no results"
- [ ] LIKE wildcard escaping for --group
- [ ] Tests: `(`, `***`, quotes, normal multi-word queries
