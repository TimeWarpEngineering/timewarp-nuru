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

### Implementation plan (Phase 2)

#### Design choices
- **Pure resolver + caller default:** `ResolveMode` returns `AttestationMode? Mode` null when blank/absent OR unrecognized; `EffectiveMode(resolution, whenUnset)` applies context default. Constants: `DefaultPrMode = Warn`, `DefaultReleaseMode = Require`.
- **Blank ≠ explicit warn:** critical so release blank→require while explicit `"warn"`→advisory on release.
- **Release typo policy:** fail closed to Require + surface typo warning; never treat typos as Off. PR keeps fail-open to Warn.
- **CLI:** `dev workflow --attestation off|warn|require`; precedence CLI > config > context default.
- **Optional shared helper:** `RunAttestationPolicyStepAsync` for PR + release to avoid dual-path drift.

#### File changes
1. `source/timewarp-nuru-devcli/content/any/services/attestation-config.cs` — Off enum; nullable Mode; EffectiveMode; design region rewrite
2. `tools/dev-cli/endpoints/workflow-command.cs` — --attestation option; PR off skip; release uses resolved mode not hard gate
3. `tests/timewarp-nuru-tests/devcli/attestation-03-mode-resolution.cs` — rewrite matrix for new contract + EffectiveMode cases
4. `source/timewarp-nuru-devcli/readme.md` — migration notes (release honors mode)
5. `documentation/developer/guides/releasing.md` — Step 1/6 attestation gate docs
6. `.timewarp/dev.jsonc` — comment accuracy

#### Ordered steps
1. Resolver first + unit tests
2. CLI option + plumb
3. PR path (off/warn/require)
4. Release path replace hard gate
5. Docs
6. Smoke

#### Non-goals
No ganda signing changes; no separate prMode/releaseMode; no org-wide off flip.

## Session

- Created: 2026-08-09 — product decisions: TimeWarp-first default require on release when unset; single mode key; CLI override; config in dev.jsonc
- Orchestration/plan: grok (2026-08-09) — plan finalized: pure ResolveMode + EffectiveMode defaults; Off mode; release honors mode; CLI --attestation
