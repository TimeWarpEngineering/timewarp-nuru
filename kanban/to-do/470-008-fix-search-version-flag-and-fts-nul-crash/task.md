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

- [x] --version prints CLI version (M11)
- [x] FTS NUL (M12)
- [x] index.db file mode (M30)
- [x] Tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M11, M12, M30.

## Session

- 2026-09-23 — implementer (Claude Fable 5.1, `ganda task work 470-008`, implement oracle). Claimed
  worktree `task/470-008-fix-search-version-flag-and-fts-nul-crash`. Implemented M11, M12, M30 plus
  one latent AOT bug found by the new end-to-end test (see Results). All four search test files
  green locally; full CI multi-mode run recorded below.

## Results

### What changed

- **M11 — `--version` prints the CLI version.** `SearchAsync` now `LEFT JOIN`s `clis` and selects
  `clis.version` into a new `SearchResult.CliVersion`. The header line is built by
  `SearchQuery.Handler.FormatResultHeader(result, showVersion)`: `  <pattern> [<cli>]` or
  `  <pattern> [<cli>@<version>]` (`@unknown` only if the `clis` row is missing).
  `result.Endpoint.Kind` no longer leaks into the version slot.
- **M12 — NUL / control characters.** `SanitizeFtsQuery` maps every `char.IsControl` code point
  (U+0000, other C0, DEL, C1) to a space before tokenising, so `hello\0world` becomes
  `"hello"* "world"*` and a control-only query returns empty. As defense in depth, `SearchAsync`
  catches `SqliteException` around the MATCH, logs a warning with the raw query, and returns no
  results instead of crashing the CLI.
- **M30 — file modes.** `DatabasePath.EnsureIndexPath(dir)` creates `~/.nuru` with `0700`
  (`Directory.CreateDirectory(path, UnixFileMode)`) and pre-creates an empty `index.db` with
  `0600` via `FileStreamOptions.UnixCreateMode` (an empty file is a valid SQLite database, and
  SQLite inherits the main file's mode for `-journal`/`-wal`). Modes are applied at creation only,
  per the requirement; pre-existing files are left as they are. Windows relies on user-profile
  ACLs (no Unix mode is set).
- **Latent AOT bug (found by the new tests).** `InsertEndpointAsync` / `SearchAsync` used the
  reflection-based `JsonSerializer.Serialize/Deserialize<EndpointCapability>` overloads in an
  `IsAotCompatible` tool. Replaced with a search-local `SearchIndexJsonContext`
  (`[JsonSerializable(typeof(EndpointCapability))]`, default naming) — the output is
  byte-compatible with what reflection wrote (PascalCase, unindented, `Kind` via its string
  converter), so existing `~/.nuru/index.db` rows stay readable. Covered by
  `Should_store_endpoint_json_in_legacy_pascal_case_format`.
- **Testability.** Added `internal SearchIndex(ILogger, string dataSource)` so tests run the real
  schema against `:memory:` without touching `~/.nuru`; IVT entries added for the three new test
  assemblies in `global-usings.cs`.

Files: `source/timewarp-nuru-search/services/{database-path,search-index,search-index-json-context}.cs`,
`source/timewarp-nuru-search/endpoints/search-query.cs`, `source/timewarp-nuru-search/global-usings.cs`,
`tests/timewarp-nuru-search-tests/search-0{1,2,3,4}-*.cs`.

### Out of scope, observed

- Result headers print the group path twice when the stored pattern already carries it
  (`index index rebuild`). Pre-existing display behaviour, unchanged here.
- Files created by older versions under `~/.nuru` remain at their original mode; tightening
  existing files was not required by this task (470-011 uses the same creation-only rule).

### How to validate

**Smoke**

```bash
# 1. Unit / integration tests (each is a Jaribu runfile; all use temp dirs or :memory:)
dotnet run tests/timewarp-nuru-search-tests/search-01-fts-sanitizer.cs      # 27 tests
dotnet run tests/timewarp-nuru-search-tests/search-02-version-formatter.cs  # 5 tests
dotnet run tests/timewarp-nuru-search-tests/search-03-search-index.cs       # 7 tests
dotnet run tests/timewarp-nuru-search-tests/search-04-database-path.cs      # 5 tests

# 2. Real binary against a throw-away HOME (never touches your ~/.nuru)
dotnet build source/timewarp-nuru-search/timewarp-nuru-search.csproj
exe=source/timewarp-nuru-search/bin/Debug/net10.0/TimeWarp.Nuru.Search
tmp=$(mktemp -d)
HOME=$tmp $exe index rebuild --cli $exe
ls -la $tmp/.nuru
HOME=$tmp $exe search rebuild --version
HOME=$tmp $exe search "$(printf 'rebu\x00ild')"
HOME=$tmp $exe search "$(printf 'hello\x01world')" --version
rm -rf $tmp

# 3. Full CI multi-mode (search tests are globbed into the single assembly)
ganda runfile cache --clear
dotnet run tests/ci-tests/run-ci-tests.cs
```

**Expect**

- Step 1: every file reports `Failed: 0`; the sanitizer file includes
  `Should_execute_fts_query_for_embedded_nul`, the index file includes
  `Should_join_cli_version_onto_search_results` and `Should_not_throw_for_query_with_embedded_nul`.
- Step 2: `ls -la` shows `drwx------ … .nuru` and `-rw------- … index.db`.
  `search rebuild --version` prints `  index index rebuild [TimeWarp.Nuru.Search@3.0.0-beta.NN+<sha>]`
  (a version string, never `command`/`query`). Both control-character searches print
  `No results found.` and exit 0 — no `SqliteException` / `unterminated string`.
- Step 3: CI exits 0 with no new failures.

