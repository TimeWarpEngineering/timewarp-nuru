# Review framework — task 508-002

**Date:** 2026-10-19
**Host task:** kanban/to-do/508-002-expose-the-generated-capabilities-catalog-as-a-public-runtime-api/
**Diff scope:** branch `task/508-002-expose-the-generated-capabilities-catalog-as-a-pub` vs `master` (`a5bf6cfd`, 17 files, +663/−37). Product commit `feat(capabilities): expose the generated catalog as NuruApp.Capabilities`.
**Plan / brief:** Public in-process `CapabilitiesResponse` (`NuruApp.Capabilities` and `FilterByGroup`), `--capabilities` prints that instance, and `EndpointCapability.returnType` describes the result. Docs in `agent-invocation.md` and `built-in-routes.md`.
**Effort:** 2 (by-diff, 700 lines). Roster is general only.
**Reviewer roster:** general
**Session IDs:** grok `01a1500b-9a86-7da0-b791-964e7fe71249` (2026-10-19, round 1). grok `01a15021-cc40-7bf0-a559-e4ba8bcc975b` (2026-10-19, round 2).

## Round 2 scope

Fix delta `45fe5339` vs `b70770fd` (79 lines, effort 1, roster general). Re-check M1. Product paths: `capabilities-emitter.cs`, `capabilities-08-runtime-catalog.cs`, `documentation/user/features/agent-invocation.md`.

## Budget (by-diff)

- Lines changed: 79
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
