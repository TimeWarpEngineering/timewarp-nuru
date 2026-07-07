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

## Notes

### Implementation Plan (2026-07-07)

#### Decisions

| # | Decision | Choice |
|---|----------|--------|
| 1 | FTS5 quoting | Wrap each token in `"..."`, double internal `"`, append `*` outside. Always append `*`. Strip nothing. |
| 2 | Empty token list | `SearchAsync` returns empty `List<SearchResult>` early, before SQL execution |
| 3 | LIKE wildcards | `ESCAPE '\'` clause + escape `\`→`\\`, `%`→`\%`, `_`→`\_` in parameter |
| 4 | Test infra | Standalone runfile under `tests/timewarp-nuru-search-tests/` with `#:project` |
| 5 | Visibility | Promote `SanitizeFtsQuery` `private`→`internal`; new `EscapeLikePattern` helper also `internal` |

#### Step 1: Rewrite SanitizeFtsQuery (lines 290-314)
File: `source/timewarp-nuru-search/services/search-index.cs`
- Promote `private` → `internal`
- Replace body: split on spaces, for each token double internal `"` → `""`, wrap in `"..."`, append `*` outside closing quote. Strip nothing.
- Transforms: `hello` → `"hello"*`, `(` → `"("*`, `***` → `"***"*`, `hello"world` → `"hello""world"*`, `hello world` → `"hello"* "world"*`, whitespace → `""` (empty)

#### Step 2: Early return for empty sanitized query in SearchAsync
File: `source/timewarp-nuru-search/services/search-index.cs`, after line 234
- Insert: `if (string.IsNullOrEmpty(sanitizedQuery)) { return results; }` before `await using SqliteCommand cmd = ...`
- Prevents `MATCH ''` from reaching ExecuteReaderAsync

#### Step 3: Add EscapeLikePattern helper
File: `source/timewarp-nuru-search/services/search-index.cs`, near SanitizeFtsQuery
- New `internal static string EscapeLikePattern(string input)` — escapes `\` first, then `%`, then `_`
- Order matters: `\` → `\\`, `%` → `\%`, `_` → `\_`

#### Step 4: Fix LIKE clause (lines 252-256)
File: `source/timewarp-nuru-search/services/search-index.cs`
- Change `LIKE $groupPath || '%'` to `LIKE $groupPath || '%' ESCAPE '\\'`
- Change `AddWithValue("$groupPath", groupPath)` to `AddWithValue("$groupPath", EscapeLikePattern(groupPath))`
- Trailing `'%'` (literal in SQL) remains a wildcard — only user-supplied $groupPath is escaped

#### Step 5: Add InternalsVisibleTo for test file
File: `source/timewarp-nuru-search/global-usings.cs:18`
- Add `[assembly: InternalsVisibleTo("search-01-fts-sanitizer")]`

#### Step 6: Create test directory + Directory.Build.props
New file: `tests/timewarp-nuru-search-tests/Directory.Build.props`
- Import parent Directory.Build.props, add `using TimeWarp.Nuru.Search.Services`

#### Step 7: Create test file
New file: `tests/timewarp-nuru-search-tests/search-01-fts-sanitizer.cs`
- 13 tests (9 FTS sanitizer + 4 LIKE escaping):
  - FTS: `(` → `"("*`, `***` → `"***"*`, `hello"world` → `"hello""world"*`, `hello world` → `"hello"* "world"*`, whitespace → `""`, empty → `""`, `hello^world` → `"hello^world"*`, `[test]` → `"[test]"*`, `(hello)` → `"(hello)"*`
  - LIKE: `100%` → `100\%`, `my_cli` → `my\_cli`, `path\to` → `path\\to`, `mygroup` → `mygroup`

#### Step 8: Verify
1. `ganda runfile cache --clear`
2. `dotnet run tests/timewarp-nuru-search-tests/search-01-fts-sanitizer.cs` (standalone — expect 13 pass)
3. `dotnet run tests/ci-tests/run-ci-tests.cs` (full CI — no search tests in CI, count unchanged)

#### Files touched
- Edit: `source/timewarp-nuru-search/services/search-index.cs` (SanitizeFtsQuery rewrite + empty guard + EscapeLikePattern + LIKE clause fix)
- Edit: `source/timewarp-nuru-search/global-usings.cs` (InternalsVisibleTo)
- Create: `tests/timewarp-nuru-search-tests/Directory.Build.props`
- Create: `tests/timewarp-nuru-search-tests/search-01-fts-sanitizer.cs` (13 tests)

#### Risk assessment
- No CI coverage for search tests (standalone runfile, matches mcp-07 precedent)
- No behavioral change for normal queries: `"hello"*` produces same prefix-match as old `hello*`
- LIKE escaping only affects user-supplied $groupPath, not the trailing SQL literal `%`
- Robustness fix: crashes become "no results" — same discipline as 454-015 fuzz guarantee
