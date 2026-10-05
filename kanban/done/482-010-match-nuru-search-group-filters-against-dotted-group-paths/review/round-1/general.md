# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commit `f1816861` (S-2 dotted `--group` filters)

## Summary

`SearchIndex.NormalizeGroupFilter` splits the `--group` value on `.`, space, and tab, drops empty segments, and joins the rest with single spaces so it matches the `group_path` `InsertEndpointAsync` stores with `string.Join(" ", endpoint.GroupPath)`. `SearchAsync` then matches that value exactly or as a parent (`LIKE $groupPath || ' %'`), with `EscapeLikePattern` still applied before the parameter bind. `docker.remote` and `docker remote` both become `docker remote`. `docker` matches `docker remote`, `docker remote auth`, and `docker remotes`, and does not match `dockerx remote`. `docker.remote` does not match `docker remotes`. The handler passes `query.Group` through unchanged, so every `SearchAsync` caller gets the same normalization. The option description documents the dotted form.

Checked and not filed: capabilities filtering (`capabilities-emitter.cs`) still splits only on `.` and compares segments with `OrdinalIgnoreCase`. SQLite `LIKE` stays ASCII case-insensitive, which this task accepts. Collapsing empty segments (`docker..remote` → `docker remote`) is more permissive than `Split('.')` and is locked by `Should_normalize_group_filter_forms`. A whitespace-only filter becomes empty and skips the predicate. `search-03-search-index.cs` passed 12/12, including the pre-existing `Should_filter_by_group_path_and_keep_version` case for group `index`.

## Issues

No issues.
