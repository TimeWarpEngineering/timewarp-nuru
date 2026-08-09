# Review framework — task 458-011

**Date:** 2026-08-09
**Host task:** kanban/in-progress/458-011-release-mode-honors-attestationmode-add-off-default-require/
**Diff scope:** commit `9fe106ec` (`feat(devcli): honor attestation.mode in release; add off + CLI override`) — files:

- `source/timewarp-nuru-devcli/content/any/services/attestation-config.cs`
- `tools/dev-cli/endpoints/workflow-command.cs`
- `tests/timewarp-nuru-tests/devcli/attestation-03-mode-resolution.cs`
- `source/timewarp-nuru-devcli/readme.md`
- `documentation/developer/guides/releasing.md`
- `.timewarp/dev.jsonc`

**Plan / brief:** Release honors `attestation.mode`; add `Off`; pure `ResolveMode` + `EffectiveMode` with PR default Warn / release default Require when blank; CLI `--attestation` overrides config; typos never Off (PR fail-open Warn, release fail-closed Require).

**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok orchestration 2026-08-09

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
