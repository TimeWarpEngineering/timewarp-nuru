# Review framework — task 585

**Date:** 2026-11-14
**Host task:** kanban/in-progress/585-anonymous-type-handler-return-breaks-the-build-in-the-generated-nuru-user-types-json-context/
**Diff scope:** task commits on `task/585-anonymous-type-handler-return-breaks-the-build-in` after the origin/master merge `713a9cc3`, HEAD `6c4636ed`. Product diff is `2a71a81c`: `result-value-kind.cs`, `handler-extractor.method-symbols.cs`, `handler-invoker-emitter.output.cs`, `GenerateNuruJsonContextTask.Extraction.cs`, `documentation/user/features/output-handling.md`, and `routing-40-anonymous-type-results.cs`. The branch also contains the merged 583/584/586 commits; those are outside this review.
**Plan / brief:** A delegate handler that returns an anonymous type (`new { ... }`, or a type that contains one, sync or async) must compile. Skip the type in the JSON context and print it through `ToString()`. Cover sync, async, list, command, and null. Update the output-handling note.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer grok (Grok 4.7), body tw-implementation-review, print mode. Cursor conversation `f8e3068c-dd7f-46c7-96ef-048e50412be4`. Turn cap 120; cursor session uncapped (no `--max-turns` flag).

## Budget (by-diff)

- Lines changed: 393
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims by reading the diff and the kitchen `## Results` and `### How to validate` record
- Do not re-run a full build, a full test run, an audit, or the hooks; those belong to the implementer's recorded results, the host audit node, and CI
- Exception: one targeted command may check one suspected bug; the finding names the command and the reason. A full build, full test run, or audit is never that command
- A missing or thin recorded result is a finding, not a reason to run the gates
- Prior rounds are immutable; new work goes in `round-(N+1)/`
