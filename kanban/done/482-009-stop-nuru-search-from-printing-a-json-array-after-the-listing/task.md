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

- [x] A search with matches prints the text listing and does not print a JSON array after it
- [x] A search with no matches prints `No results found.` and does not print `[]`
- [x] Add a test that captures stdout for both cases

## How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-search-tests/search-05-search-query-output.cs
```

Expect: `Passed: 2`. The hit test runs the real `TimeWarp.Nuru.Search` assembly as `search deploy` against a seeded temporary index. Its stdout ends at `    Deploy hello world`. The miss test runs `search nonexistentzzz`, and its stdout is exactly `No results found.`. Neither stdout contains a JSON array (`[{` / `[]`).

## Session

- Created: 534611 (2026-10-03)
- Body filled from task 482 review: grok 01a109e2-e070-73a0-991d-a38c4b580ef1 (2026-10-05)
- Implemented: claude implementer under ganda task work (2026-10-05)
- Review: grok 01a10be1-9df1-71f0-b11e-035c41517cc9 (2026-10-05)
- Review oracle: review by implementer-grok (grok, model grok-4.7), session not reported, max-turns 80 — 2026-10-05T11:51:27Z

## Results

- `SearchQuery` is now `IQuery<Unit>` (handler returns `default`), matching `index list`. The generated invoker no longer serializes a `SearchResult[]` after the listing. No machine-readable mode existed, so none is lost.
- New `tests/timewarp-nuru-search-tests/search-05-search-query-output.cs` runs the real `TimeWarp.Nuru.Search` assembly as a child process with `HOME` set to a temporary directory whose `.nuru/index.db` is pre-seeded. It captures stdout for a hit and for a miss. Both tests fail on the old code (JSON array appended) and pass with the fix.
- An in-process `Map<SearchQuery>()` was not usable because the generator does not pick up `[NuruRoute]` endpoints from a referenced assembly (it reported "Unknown command"). That is why the test spawns a process.
- Added `InternalsVisibleTo("search-05-search-query-output")` so the runfile can seed the index through the internal `SearchIndex(logger, dataSource)` / `DatabasePath.EnsureIndexPath`.
- `dotnet run tests/ci-tests/run-ci-tests.cs` passes (exit 0), including `SearchQueryOutput` 2/2.

### Review

- Rounds: 1. Roster: general. Effort: 1 (138-line diff, commit `cb2561e3`).
- Counts: bug 0 open / 0 fixed / 0 wontfix; suggestion 0; nit 0. Final open count: 0.
- Disposition: clean. No wontfix. No escalation.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
- Re-checked: `dotnet run tests/timewarp-nuru-search-tests/search-05-search-query-output.cs` passed 2/2. Generated search route assigns `TimeWarp.Mediator.Unit result` and returns `Environment.ExitCode` with no `JsonSerializer.Serialize` call.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-search-tests/search-05-search-query-output.cs
```

Expect: `Passed: 2`, with the hit stdout ending at the description line and the miss stdout exactly `No results found.`. Neither contains a JSON array.

## Notes

- Parent review: `kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/review/findings.md` on the task 482 branch until that PR merges.
- Implementation review: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`. Outcome clean.
