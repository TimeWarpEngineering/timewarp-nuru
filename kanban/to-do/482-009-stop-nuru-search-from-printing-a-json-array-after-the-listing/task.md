# Stop nuru search from printing a JSON array after the listing

## Parent

482 — Complete code review of TimeWarp.Nuru before the official 3.0 release

## Description

Finding S-1 from task 482. Fix it before the `3.0.0` tag. The parent review record is the source of the disposition. Do not reopen the whole review on this id.

`nuru search` prints the human listing and then the generator serializes the handler return value.

`SearchQuery` is `IQuery<SearchResult[]>`. The handler writes the listing (including `No results found.`) and returns the array. The invoker writes `JsonSerializer.Serialize(result, ...)` for a non-null return. Empty and successful searches both append a JSON array.

Evidence: `source/timewarp-nuru-search/endpoints/search-query.cs`, `source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs`. Parent record: `review/supporting.md` S-1.

Keep a machine-readable mode if one already exists. The default human listing must be the only stdout for a normal search.

## Checklist

- [ ] A search with matches prints the text listing and does not print a JSON array after it
- [ ] A search with no matches prints `No results found.` and does not print `[]`
- [ ] Add a test that captures stdout for both cases

## How to validate

Smoke:

```bash
dotnet test tests/timewarp-nuru-tests/timewarp-nuru-tests.csproj --filter FullyQualifiedName~SearchQuery
```

Expect: Stdout for a hit ends at the description line. Stdout for a miss is the no-results line. Neither ends with a JSON array.

## Session

- Created: 534611 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
