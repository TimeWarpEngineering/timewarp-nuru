# nuru search drops all its log output because it never configures logging

## Description

`nuru search` runs, but every warning and error it logs is thrown away. `source/timewarp-nuru-search/program.cs` never calls `AddLogging`, so the generator hands each `ILogger<T>` a NullLogger:

- `SearchQuery.Handler` (endpoint constructor): auto-index messages.
- `CapabilitiesClient`: "CLI returned exit code", "empty capabilities", "failed to deserialize", and the Error-level "failed to parse capabilities JSON". These are exactly the messages that explain why a CLI didn't get indexed.
- `SearchIndex`: "search failed" (Error) and the index/clear messages.

So when indexing or a search fails, the user gets no output explaining why.

Found in 541 (PR #358): adding NURU_H007 for endpoint constructors made nuru-search fail to build, because it injects a logger with no `AddLogging`. 541 left endpoint constructors out of H007 and documented that.

## Checklist

- [ ] Add logging to nuru-search (`ConfigureServices(s => s.AddLogging(...))`), with warnings and errors to stderr so stdout stays clean for results
- [ ] Extend NURU_H007 to endpoint handler constructors (the part 541 deferred), and update the "endpoint constructors don't warn yet" note in both `logging.md` copies
- [ ] Fix any other repo app/sample that the new warning breaks
- [ ] Test: H007 fires for an endpoint constructor `ILogger<T>` without `AddLogging`, and not with it
- [ ] Verify by hand: `nuru search --cli <bad-path>` prints the warning

## Notes

- Behaviors and delegate handlers already warn (541). Endpoint constructors are the remaining silent case.
- This is breaking for user apps that treat warnings as errors. Add a changelog entry.

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23)
