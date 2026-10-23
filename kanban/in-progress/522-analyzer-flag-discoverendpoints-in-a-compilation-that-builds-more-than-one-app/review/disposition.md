# Disposition — task 522

**Date:** 2026-10-23
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 1 (general, effort 2) raised no findings. NURU_A003 matches the task: it warns on unfiltered `DiscoverEndpoints()` when the compilation builds apps in more than one source file. It stays silent for one app file, for group-filtered discovery, and for an app that also calls `Map<T>()`. It has pragma and standalone opt-outs and a temporary CI `NoWarn` for this id, and no test was converted. The recorded 49-site reconciliation matched a recount of `.DiscoverEndpoints()` under `tests/`.

Round 2 (general, effort 2) re-reviewed the branch after the `origin/master` merge (task 521) and the renumber of this task's test to generator-64. The merge keeps master's 521 files unchanged and keeps both CI list entries. 521 adds no `DiscoverEndpoints` call, so the count of 49 still holds, and generator-64 passes 6/6. Two cosmetic nits from the renumber were marked wontfix (see below).

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | nit | `run-ci-tests.cs:32` comment says `generator-28..63`, should be `..64`. Comment only. Deferred to the follow-up conversion task, which edits the same CI files, so this CI-cost PR does not need another fix round. | review orchestrator |
| M2 | nit | generator-64 still uses namespace `Gen63DiscoverEndpointsAcrossApps` and assembly name `Gen63DiscoverEndpoints`. These are internal to a standalone test, with no collision and no behavior effect. Deferred for the same reason. | review orchestrator |

## Escalations

None.
