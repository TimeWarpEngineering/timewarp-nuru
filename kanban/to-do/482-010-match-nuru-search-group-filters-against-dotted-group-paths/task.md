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

- [ ] `--group docker.remote` returns endpoints whose group path is `docker remote` and its children
- [ ] `--group docker` still returns that group and its children
- [ ] A group that merely contains the letters as a different segment does not match
- [ ] Add a search-index test that inserts a nested group and queries both forms

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~SearchIndex
```

Expect: The dotted filter and the first-segment filter both return the nested endpoint. An unrelated group is absent.

## Session

- Created: 535148 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
