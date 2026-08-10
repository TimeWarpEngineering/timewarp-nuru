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

- [x] Extend `AttestationMode` / `AttestationConfigResolver` with **`Off`**
- [x] Resolver documents **context-sensitive default**: blank → Warn for PR/merge callers, Require for release callers — *or* resolve blank to a sentinel and let callers apply default (prefer pure resolver + caller default to keep unit tests clear)
- [x] `RunReleaseWorkflowAsync` uses resolved mode instead of unconditional hard gate
- [x] `RunPrAttestationStepAsync` treats `off` as skip (no advisory spam required; one line “attestation skipped (mode=off)” is fine)
- [x] `dev workflow --attestation off|warn|require` (wire through workflow command options)
- [x] Update DevCli readme migration notes + any releasing guide mentions of “always enforced in release”
- [x] Unit tests for resolver (`off`, blank+context, typo, case-insensitivity)
- [x] No change to ganda signing; this is verify/skip policy only

## Checklist

- [x] Implement resolver + mode enum
- [x] Wire release + PR paths
- [x] CLI override on workflow command
- [x] Docs (readme / releasing guide if it says release always requires attestation)
- [x] Tests
- [x] Note for consumers: TimeWarp repos should set `"mode": "require"` explicitly if they want require on **both** PR and release; blank already require-only on release

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
- Implementation: grok (2026-08-09) — AttestationMode.Off + nullable ResolveMode + EffectiveMode; shared RunAttestationPolicyStepAsync for PR+release; --attestation CLI; tests 18/18 pass; docs/jsonc comments updated
- Review: grok (2026-08-09) — effort 1 general; round-1 clean disposition under `review/`

## Results

### What was implemented

Release (and PR) attestation policy now follows `.timewarp/dev.jsonc` `attestation.mode` with context-sensitive defaults and a CLI override.

| Mode | PR/merge | Release |
|------|----------|---------|
| unset / blank | warn | **require** |
| `off` | skip verify | skip verify |
| `warn` | advisory | advisory |
| `require` | hard fail | hard fail |

- `AttestationMode.Off` added; `ResolveMode` returns nullable Mode (blank/typo → null); `EffectiveMode` + `DefaultPrMode` / `DefaultReleaseMode`
- Shared `RunAttestationPolicyStepAsync` for PR + release (no dual-path drift)
- `dev workflow --attestation off|warn|require` — precedence CLI > config > context default
- Typos: never Off; PR fail-open Warn; release fail-closed Require; one warning line surfaces the raw value
- Docs: DevCli readme migration, releasing.md Step 1/6, `.timewarp/dev.jsonc` comments

### Files changed

- `source/timewarp-nuru-devcli/content/any/services/attestation-config.cs`
- `tools/dev-cli/endpoints/workflow-command.cs`
- `tests/timewarp-nuru-tests/devcli/attestation-03-mode-resolution.cs`
- `source/timewarp-nuru-devcli/readme.md`
- `documentation/developer/guides/releasing.md`
- `.timewarp/dev.jsonc`

### Key decisions

- Pure resolver + caller default (blank ≠ explicit `"warn"`)
- Shared policy helper for both pipeline modes
- Release require abort reason aligned with PR: `attestation required (mode=require) and not valid`

### Review (Phase 4b)

- **Rounds:** 1
- **Effort / roster:** 1 — general only
- **Final counts:** 0 open / 0 fixed / 0 wontfix (all severities)
- **Disposition:** `clean` — no issues found
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`

### Test outcomes

- `attestation-03-mode-resolution.cs`: 18/18 passed
- `attestation-01-verifier.cs`: 30/30 passed (regression)
- `dotnet build tools/dev-cli/dev.cs` success; `--attestation` visible on `workflow --help`

### How to validate

**Automated gate**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/dev
dotnet run -- tests/timewarp-nuru-tests/devcli/attestation-03-mode-resolution.cs
# expect: Total: 18  Passed: 18

dotnet run -- tests/timewarp-nuru-tests/devcli/attestation-01-verifier.cs
# expect: all passed (regression on verifier; no policy change)
```

**Smoke**

```bash
# CLI surface
dotnet run --file tools/dev-cli/dev.cs -- workflow --help
# expect: option --attestation documented

# Skip path (PR) — should log skip and not require a valid note
dotnet run --file tools/dev-cli/dev.cs -- workflow --mode pr --attestation off
# expect: line containing "Attestation skipped (mode=off)" (or equivalent) and pipeline continues past attestation
```

**Expect**

- Blank config (this repo’s `attestation` often commented out): release still **requires** valid attestation; PR defaults to **warn**.
- Explicit `"mode": "off"` or `--attestation off` skips verify on both paths.
- Typo modes never behave as `off`.

**Not in scope**

- Rebuilding published AOT `./bin/dev` (runfile / `dotnet run --file tools/dev-cli/dev.cs` picks up changes)
- End-to-end full release ship without a note (policy unit-covered; live release still needs tag/gates)
