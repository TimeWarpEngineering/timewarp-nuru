# Round 5 — merged findings
**Date:** 2026-11-20
**Sources:** general
**Scope:** fix pass `61bb293f..59102fad`. Carries M1–M8.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 5 | 0 |
| suggestion | 1 | 1 | 0 |
| nit | 0 | 2 | 0 |

## Resolved prior

- M1–M3 (bug), M4 (suggestion), M5 (nit), M6 (bug): fixed in earlier rounds.
- M7 (bug): fixed. A required positional takes back trailing greedy values. routing-09 uses `--tag A run.sh` again.
  The docker build-arg and endpoint collection cases pass. The catch-all shape is split out as M9.
- M8 (nit): fixed. The runfile is `routing-45-binding-strictness.cs`.

## Issues

### M9 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.unknown-options.cs:179;
  route-matcher-emitter.option-parsing.cs:234
- Description: A string repeated option followed by `{required} {*catchall}` with no `--` mis-binds and exits 0.
  The sample `fluent-docker-run --env {e}* -p {port:int}* {image} {*cmd}`, called as
  `fluent-docker-run --env A=1 nginx echo hi`, binds `e=[A=1,nginx,echo]`, `image=hi`, `cmd=[]`.
  Before the branch it bound `image=nginx`, `cmd=[echo,hi]`. Round 4 gave `Unknown command`. The give-back
  returns only the deficit, and the catch-all reserves nothing. `docker --env? {e}* {*cmd}` also leaves the
  catch-all empty. The new routing-45 docker case leaves out `--env`, and the guide and the skill do not say
  what happens to a catch-all.
- Suggestion: When a catch-all follows and no `--` separates it from the options, take one value per flag
  occurrence. The other fix is to give back every value past `keep`. Add routing-45 cases for both shapes.
  The operator may keep the current rule instead. Then document that a repeated option starves a catch-all,
  and move the sample to `-- {*cmd}`.
- Source: general
- Disposition notes:

### M10 — Severity: suggestion — Status: open
- File: kanban/.../task.md `### How to validate`
- Description: The generator change affects every repeated-option route. The record has no
  `ganda runfile cache --clear`, which the repo rules require after a generator change. It also has no CI
  multi or standalone run, and routing-06 was not edited, so it may have run stale generated code.
- Suggestion: In the M9 fix pass, clear the cache and run `env -u NURU_CI_PHASE dotnet run tests/ci-tests/run-ci-tests.cs`.
  Record both phase counts.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer; none. M9 is the part of M7 the fix did not cover. M7 is closed so its fixed cases are not
  re-opened.
