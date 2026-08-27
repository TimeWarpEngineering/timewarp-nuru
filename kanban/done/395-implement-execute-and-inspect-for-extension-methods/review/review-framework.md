# Review framework — task 395

**Date:** 2026-08-27
**Host task:** kanban/in-progress/395-implement-execute-and-inspect-for-extension-methods/
**Diff scope:** PR #227 — `task/395-implement-execute-and-inspect-for-extension-method` vs `origin/master` (`https://github.com/TimeWarpEngineering/timewarp-nuru/pull/227`)
**Plan / brief:** `task.md` — decompile `lib/` (not `ref/`), purity-fail the whole user-facing `AddX` if anything is not a lowerable `IServiceCollection` call, merge public closed `Add*` into Phase 3 `new`/`Lazy<T>`. No execute-and-inspect. No `IServiceCollection` replay. Hatch unchanged.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** cockpit grok (folderize + framework, 2026-08-27); round-1 general (Herdr `launch` / w8); fix pass implementer `task395` / w7 (`b034c788`); round-2 launching

## Ground rules

- Reviewers are read-only on product code; they write only under the **current** round directory (`review/round-2/` now)
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Judge against `task.md` (decompile + purity + lower), not the historical execute-and-inspect slug
- CI on PR #227 is green; that is not a substitute for this review

## In scope

- `extension-method-lowerer.cs`, `referenced-method-decompiler.cs`, `service-registration-methods.cs`
- `service-extractor.cs` (TryAdd, chains, lower)
- NURU052/054 behavior and tests `generator-41`, `generator-42`
- Packing `ICSharpCode.Decompiler.dll`; `ref/` vs `lib/` resolution
- Fail-closed purity (builders, factories, internals, side effects)

## Out of scope

- Epic 391 Phase 5, task 444 ServiceGen
- Factory lowering, new HttpClient/logging special-cases, open generics
- Rewriting `ganda task work` to include review by default (flow/ganda 011)

## Round 2

**Date:** 2026-08-27
**Prior round:** `review/round-1/` is **frozen**. Do not edit it.
**Diff scope:** post-fix delta `b034c788` (kitchen `04656763`) on PR #227; also re-verify M1/M2 on HEAD vs `origin/master`.
**Write:** `review/round-2/general.md` only.

Re-verify prior IDs **M1** and **M2** (ledger says `fixed`). May raise **new** findings on the fix delta. Carry stable `M#` IDs; new issues get new IDs.
