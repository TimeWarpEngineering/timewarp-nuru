# Round 1 — general
**Date:** 2026-10-18
**Scope reviewed:** same as framework (`master...task/506`, `spikes/roslynk-cli/**` and task.md `## Results`), with Roslynk `788735a1` read for the tool signatures.

## Summary

The spike does what the task and the restatement ask. Two hand-written routes call `GetDiagnosticsTool` and `FindReferencesTool` in-process. Route and option descriptions match Roslynk's `[Description]` text word for word, including the `ParameterDescriptions` constants. The agent contract works. The code has no correctness defect: exit codes follow `error=` headers, and Nuru's generated `RunAsync` returns `Environment.ExitCode`. The findings are about how two Results claims read for the people who use them (Pete, task 507, and the daemon decision).

Re-verified on this machine, with all exits as stated:

- `--capabilities` is 6,519 characters and has both endpoints.
- `diagnostics --help` has the description and `Values may come from --json-args.`
- `schema-report` prints `tools=29`, `clean=137`, `blocked=2`, `host=13`, and synthetic sizes 32,599 and 3,371.
- The binding probe prints `includeAnalyzers=False ... ids=CS0246 ... tool=find_references`, and omission gives `includeAnalyzers=null`.
- `refs` worked with inline JSON and with `@path`. `diagnostics` worked with stdin `-`, with `solutionid` in the JSON.
- A missing solution prints `error=Faulted` and exits 1.
- `warm-pair` gave first 5,771 ms and second 0.673 ms.
- `ganda repo audit` passes.
- No CI glob or slnx picks up `spikes/`.

Goal restatement versus task: no misread. "When it can actually be run" for the SimCube baseline matches the task's "where possible".

## Issues

### Issue 1 — Severity: suggestion
- File: kanban/to-do/506-roslynk-cli-spike-on-nuru-pitch-for-pete/task.md:78 (also :151 and the Cold versus warm section)
- Description: The warm diagnostics figure of 0.68 ms is Roslynk's memoized diagnostics result, not a warm query. `GetDiagnostics` keys its instance cache on `includeAnalyzers` alone (`cacheKey = $"{includeAnalyzers}"`, GetDiagnosticsTool.cs:107). `RequestDiagnosticsAsync` "reuse[s] the cached result when nothing changed". The second call in `warm-pair` repeats the same arguments, so it measures a cache lookup. The cold 6 s is load plus compile (analyzers off), not load alone. The pitch says "the load is seconds and the second call is under a millisecond". Pete or the daemon decision could read that as "any warm call is under 1 ms". The `refs` pair (second call median 25.9 ms) is the uncached warm figure, and it already supports the same verdict.
- Suggestion: In the pitch and in the gap paragraph, say that the warm diagnostics call is a cache hit for the same `includeAnalyzers` key. Cite `refs` at about 26 ms as the uncached warm cost. Describe the cold cost as load plus first compile. Optionally add one warm pair whose second call changes the key (analyzers on), to show what a warm compile costs. The daemon verdict (GREEN for a process per command) does not change.
- Status: open

### Issue 2 — Severity: suggestion
- File: kanban/to-do/506-roslynk-cli-spike-on-nuru-pitch-for-pete/task.md:78 and :117; spikes/roslynk-cli/schema/schema-report.cs:101-110
- Description: "137 parameters whose CLR types already bind" is true for types, but not all of them carry their defaults. Six `bool` tool parameters default to `true`: `get_diagnostics.includeAnalyzers` and `get_members.includeMethods`, `includeFields`, `includeProperties`, `includeEvents`, and `includeNestedTypes`. A non-nullable Nuru bool flag always starts false (the Results binding table says so). A naive 507 generator would therefore silently flip those defaults. The spike handled this by hand for `includeAnalyzers` (`bool?` plus `?? true`). The "Task 507 shape" paragraph does not carry that rule, and `schema-report` classifies all six as `clean` without looking at `ParameterInfo.DefaultValue`. Related: `Classify` also marks a non-nullable enum `clean`, but Results says a non-nullable enum option does not compile. No top-level enum exists today, so this has no effect yet.
- Suggestion: In the 507 shape paragraph, state the rule: a `bool` parameter whose default is `true` becomes `bool?`, and the handler supplies the default. Give the count (6). Optionally have `schema-report` report a separate `clean-needs-nullable` (or `defaultTrue`) count, and treat a non-nullable enum the same way, so 507 reads one number.
- Status: open

### Issue 3 — Severity: nit
- File: kanban/to-do/506-roslynk-cli-spike-on-nuru-pitch-for-pete/task.md:70
- Description: The parenthetical "(`includeErrors`, `symbolName` stays positional and is lowercased by Nuru to `symbolname`)" mixes an example with a separate rule, and reads as if `symbolName` were an option.
- Suggestion: Split it. Option long forms are the tool parameter names (`includeErrors`). Positional parameters are lowercased (`solutionId` → `solutionid`, `symbolName` → `symbolname`).
- Status: open
