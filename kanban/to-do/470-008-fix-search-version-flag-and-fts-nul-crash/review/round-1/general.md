# Round 1 — general
**Date:** 2026-09-23
**Scope reviewed:** `source/timewarp-nuru-search/services/database-path.cs`, `source/timewarp-nuru-search/services/search-index.cs`, `source/timewarp-nuru-search/services/search-index-json-context.cs`, `source/timewarp-nuru-search/endpoints/search-query.cs`, `source/timewarp-nuru-search/global-usings.cs`, `tests/timewarp-nuru-search-tests/search-01-fts-sanitizer.cs`, `tests/timewarp-nuru-search-tests/search-02-version-formatter.cs`, `tests/timewarp-nuru-search-tests/search-03-search-index.cs`, `tests/timewarp-nuru-search-tests/search-04-database-path.cs` (`origin/master...HEAD`, product fix `7548dc6e`)

## Summary

The change correctly closes parent 470 findings M11, M12, and M30: `--version` prints `clis.version` via a `LEFT JOIN` onto `SearchResult.CliVersion` and `FormatResultHeader`; `SanitizeFtsQuery` maps all `char.IsControl` code points (including U+0000) to spaces before tokenising, with a `SqliteException` catch around MATCH as defense in depth; `EnsureIndexPath` creates `~/.nuru` at 0700 and pre-creates `index.db` at 0600 on Unix. An AOT-safe `SearchIndexJsonContext` replaces reflection serialize/deserialize while keeping PascalCase legacy `endpoint_json`. Risk is low: fixes are localized, modes are creation-only as required, and search-01 through search-04 cover the falsifiable cases (NUL FTS validity, version header regression, join/reindex, Unix modes).

## Issues

