# Match nuru search --group filters against dotted group paths

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding S-2 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

`--group` does not match the dotted group filter Nuru forwards.

Indexed `group_path` is `string.Join(" ", endpoint.GroupPath)`. Search filters with `e.group_path LIKE $groupPath || '%'`. Capabilities filtering splits the filter on `'.'` and compares those segments to `GroupPath` (the group prefix split on spaces). `--group docker.remote` does not prefix-match the stored `docker remote`.

Evidence: `source/timewarp-nuru-search/services/search-index.cs` (`InsertEndpointAsync`, `SearchAsync`), `source/timewarp-nuru-analyzers/generators/emitters/capabilities-emitter.cs` (`groupFilter.Split('.')`). Parent record: `review/supporting.md` S-2.

Accept the dotted form the capabilities filter already documents. A space-separated filter may keep working if callers already pass one.

## Checklist

- [x] `--group docker.remote` returns endpoints whose group path is `docker remote` and its children
- [x] `--group docker` still returns that group and its children
- [x] A group that merely contains the letters as a different segment does not match
- [x] Add a search-index test that inserts a nested group and queries both forms

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~SearchIndex
```

Expect: The dotted filter and the first-segment filter both return the nested endpoint. An unrelated group is absent.

## Session

- Created: 535148 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Implemented by claude implementer under ganda task work (2026-10-05)
- Review: grok 01a10bf2-0f25-71c1-a584-404e36a61679 (2026-10-05)
- Review oracle: review by implementer-grok (grok, model grok-4.7), session not reported, max-turns 80 — 2026-10-05T12:09:40Z

## Results

`SearchIndex.SearchAsync` now normalizes the `--group` filter with a new `NormalizeGroupFilter`
before it runs the query. The filter is split on `.` and whitespace, then joined with single spaces,
which is the format `InsertEndpointAsync` stores in `group_path`. So `docker.remote` and
`docker remote` both become `docker remote`. The SQL predicate was `group_path LIKE $g || '%'`, a raw
string prefix. It is now `group_path LIKE $g OR group_path LIKE $g || ' %'`, which matches whole
segments only. With this change `docker.remote` no longer matches `docker remotes` or `dockerx remote`.
Matching is still ASCII case-insensitive through SQLite `LIKE`, as in the capabilities filter. The
`--group` option description now documents the dotted form.

Files:
- `source/timewarp-nuru-search/services/search-index.cs` (`SearchAsync`, new `NormalizeGroupFilter`)
- `source/timewarp-nuru-search/endpoints/search-query.cs` (option description)
- `tests/timewarp-nuru-search-tests/search-03-search-index.cs`: adds 5 tests covering a nested
  group indexed with dotted, space-separated, and first-segment filters, string-prefix siblings
  (`docker remotes` and `dockerx remote`) that must not match, and normalizer edge cases.

Verified: `search-03-search-index.cs` passes 12/12. The full `tests/ci-tests/run-ci-tests.cs` run
passes after a runfile cache clear and exits 0.

### Review

- Rounds: 1. Roster: general. Effort: 1 (172-line diff, commit `f1816861`).
- Counts: bug 0; suggestion 0; nit 0; open 0.
- Disposition: clean. No wontfix. No escalation.
- Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.
- Re-checked: `dotnet run tests/timewarp-nuru-search-tests/search-03-search-index.cs` passed 12/12, including dotted, space-separated, and first-segment filters, the string-prefix exclusion, and the pre-existing `index` group filter.

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-nuru-search-tests/search-03-search-index.cs
```

(The search tests are runfiles and are also included in `tests/ci-tests`. There is no
`timewarp-nuru-tests.csproj` to filter on, so the original smoke command does not apply.)

Expect: All 12 tests pass. `Should_match_dotted_group_filter_and_children` and
`Should_match_first_segment_group_filter` return the nested `docker remote` endpoints.
`Should_not_match_group_sharing_only_a_string_prefix` returns nothing for `docker.remote`
against the `dockerx remote` and `docker remotes` groups.

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
- Implementation review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`. Outcome clean.
