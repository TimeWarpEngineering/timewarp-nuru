# Round 1 — merged findings
**Date:** 2026-10-18
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 2 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: open
- File: kanban/to-do/506-roslynk-cli-spike-on-nuru-pitch-for-pete/task.md:78 (also :151, Cold versus warm)
- Description: The warm diagnostics time (0.68 ms) is a hit on Roslynk's memoized diagnostics result. The cache key is `includeAnalyzers` alone (GetDiagnosticsTool.cs:107), and the warm pair repeats identical arguments. The cold 6 s is load plus first compile. The pitch's "the load is seconds and the second call is under a millisecond" overstates what a warm call costs in general.
- Suggestion: Say that the warm diagnostics call is a cache hit. Cite the `refs` warm median (about 26 ms) as the uncached warm cost. Describe cold as load plus compile. Optionally measure a warm pair whose second call changes the key. The verdict stays GREEN.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: kanban/to-do/506-roslynk-cli-spike-on-nuru-pitch-for-pete/task.md:78, :117; spikes/roslynk-cli/schema/schema-report.cs:101-110
- Description: Six of the 137 "clean" parameters are `bool` parameters that default to `true` (`get_diagnostics.includeAnalyzers`, `get_members.include{Methods,Fields,Properties,Events,NestedTypes}`). Generated as non-nullable flags, their defaults flip to false. The 507 shape paragraph and `schema-report` do not flag this. `Classify` also counts a non-nullable enum as clean, though Results says that shape does not compile. That case is latent, because no top-level enum exists today.
- Suggestion: Add the rule (`bool` with default `true` becomes `bool?`, and the handler supplies the default) and the count of 6 to the 507 shape paragraph. Optionally have `schema-report` emit a separate count for these.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: kanban/to-do/506-roslynk-cli-spike-on-nuru-pitch-for-pete/task.md:70
- Description: The pitch's naming parenthetical mixes the option rule with the positional-lowercasing rule. `symbolName` reads like an option.
- Suggestion: Split it into two sentences, one for option long forms and one for positional names.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. Single reviewer.
