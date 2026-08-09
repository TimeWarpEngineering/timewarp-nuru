# Release mode honors attestation.mode (add off; default require)

## Parent

458 — Review Nuru versioning and TimeWarp-wide consistency

## Description

Make the **release** pipeline respect `.timewarp/dev.jsonc` `attestation.mode` instead of always hard-gating, so repos without ganda (or owners who do not run ganda) can still use DevCli release promotion. Keep **TimeWarp-first defaults**: when unset, release still requires a valid attestation.

Today:

- **PR/merge:** `attestation.mode` = `warn` (default) | `require` — works.
- **Release:** ignores config; always requires valid note (458-010). That blocks non-ganda maintainers (e.g. external Windows-only owners) from shipping via the standard release path.

## Locked decisions (from product)

1. **TimeWarp-first defaults** — Absent/blank `attestation.mode` behaves as **`require` for release** (preserve current safety for existing TimeWarp repos that never set the key). PR/merge keeps current default **`warn`** when unset.
2. **One key** — Single `attestation.mode` for all pipeline modes (no separate `prMode` / `releaseMode`).
3. **CLI override** — Add something like `--attestation off|warn|require` on `dev workflow` (and/or release path) for break-glass without editing jsonc. Easy enough; include it.
4. **Config location** — `.timewarp/dev.jsonc` under `"attestation": { "mode": "..." }` (existing).

### Mode semantics (target)

| `attestation.mode` | PR/merge | Release |
|--------------------|----------|---------|
| **unset / blank** | **warn** (unchanged) | **require** (unchanged effective behavior) |
| **`off`** (new) | skip verify | skip verify |
| **`warn`** | advisory, do not fail | advisory, do not fail (log clearly) |
| **`require`** | hard fail if invalid | hard fail if invalid |

CLI `--attestation <mode>` overrides config for that run only (same three values + resolve rules).

Unrecognized non-blank mode: keep current PR behavior (warn + surface typo); for release, prefer **fail closed to require** or same typo warning + require — implementer pick with a unit test; do not silently treat typos as `off`.

## Requirements

- [ ] Extend `AttestationMode` / `AttestationConfigResolver` with **`Off`**
- [ ] Resolver documents **context-sensitive default**: blank → Warn for PR/merge callers, Require for release callers — *or* resolve blank to a sentinel and let callers apply default (prefer pure resolver + caller default to keep unit tests clear)
- [ ] `RunReleaseWorkflowAsync` uses resolved mode instead of unconditional hard gate
- [ ] `RunPrAttestationStepAsync` treats `off` as skip (no advisory spam required; one line “attestation skipped (mode=off)” is fine)
- [ ] `dev workflow --attestation off|warn|require` (wire through workflow command options)
- [ ] Update DevCli readme migration notes + any releasing guide mentions of “always enforced in release”
- [ ] Unit tests for resolver (`off`, blank+context, typo, case-insensitivity)
- [ ] No change to ganda signing; this is verify/skip policy only

## Checklist

- [ ] Implement resolver + mode enum
- [ ] Wire release + PR paths
- [ ] CLI override on workflow command
- [ ] Docs (readme / releasing guide if it says release always requires attestation)
- [ ] Tests
- [ ] Note for consumers: TimeWarp repos should set `"mode": "require"` explicitly if they want require on **both** PR and release; blank already require-only on release

## Notes

### Motivation

- TimeWarp org: keep safe default on release without every repo editing jsonc.
- Portable / no-ganda: set `"mode": "off"` (Roslynk-class or Windows-only maintainers without ganda).
- Operators: CLI override for one-off break-glass without committing config.

### Related code

- `tools/dev-cli/endpoints/workflow-command.cs` — release hard gate (~lines with “ALWAYS enforced in release mode”)
- `source/timewarp-nuru-devcli/content/any/services/attestation-config.cs` — design region currently says release ignores mode
- `source/timewarp-nuru-devcli/readme.md` — 3.0.0-beta.72+ migration notes
- Parent 458; sibling 458-010 (attestation introduce)

### Out of scope

- Changing how ganda signs or note format
- Rolling `off` into all non-TimeWarp repos (consumer choice)
- Removing attestation entirely

## Session

- Created: 2026-08-09 — product decisions: TimeWarp-first default require on release when unset; single mode key; CLI override; config in dev.jsonc
