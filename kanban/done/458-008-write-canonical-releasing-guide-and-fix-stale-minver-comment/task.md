# Write canonical releasing guide and fix stale MinVer comment

Parent: 458 (finding F9 in `458-*/review/findings.md`).

## Description

There is no "how we release" document anywhere in `documentation/` or the readme —
the process lives only in code comments and kanban history. And
`.github/workflows/workflow.yml:43` says `fetch-depth: 0  # Required for MinVer to
read all tags`, but MinVer is not used anywhere in the repo; the comment describes
a system that doesn't exist.

Target (convention.md rule 10): `documentation/developer/guides/releasing.md` as
the canonical release doc — SSOT location, bump-in-PR flow, `dev release` (once
458-006 lands), event → mode matrix, gate behavior including partial-publish
resume (458-005), prerelease policy outcome (458-007). Point at
`kanban/…/458-*/review/convention.md` (or its eventual timewarp-flow home) for the
org-wide convention.

## Checklist

- [x] Write `documentation/developer/guides/releasing.md`
- [x] Fix `workflow.yml` comment: fetch-depth 0 is for git-tag history checks, not MinVer
- [x] Link the guide from the developer docs index / readme as appropriate
- [x] Keep in sync with 458-001/002/003/005/006 as they land (write last, or update per landing)

## Results

Written last, against implemented behavior (commits `29a17960`, `2de852c2`,
plus a round-2 wording one-liner).

- **`documentation/developer/guides/releasing.md`** — canonical guide: SSOT +
  lockstep derived package set; bump-PR → `dev release` (8 guards, dry-run) →
  release-event promotion pipeline with every gate's exact refusal text and
  remedy; why-promotion; break-glass + partial resume; trusted publishing;
  operator appendix (branch-protection PUT, retention, 456 pointer). The
  deliberate Partial-gate asymmetry between `dev release` and the pipeline is
  documented with reasoning so it isn't "corrected" later.
- **workflow.yml** fetch-depth comment now states the real reason (release
  gate's tag/ancestor checks — MinVer was never used).
- Linked from `guides/overview.md` and root `readme.md`.
- **Review (Phase 4b):** docs-accuracy-validator, 2 rounds — 4 findings
  (1 HIGH: guide invented a `--package` flag on `dev release`; 1 MED; 2 LOW)
  all resolved; disposition **clean** (`review/disposition.md`). Validator
  verified every quoted refusal string word-for-word against code.

### How to validate

Smoke:
1. Read `documentation/developer/guides/releasing.md` next to
   `tools/dev-cli/endpoints/workflow-command.cs` and
   `source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs`:
   step banners, guard order, and refusal strings must match the code.
2. `grep -n MinVer .github/workflows/workflow.yml` → no MinVer mention.
3. Links resolve: `guides/overview.md` → releasing.md; root readme →
   documentation/developer/guides/releasing.md.

Not in scope: the org convention doc itself (lives with the 458 program);
first live `dev release` run remains the end-to-end pipeline confirmation.

## Notes

Sequencing: this task documents the end state — do it after (or alongside the tail
of) the other 458 children rather than first.

### Session — write the guide (2026-08-07)

Wrote `documentation/developer/guides/releasing.md` from a read of the actual
landed implementation (458-001/002/003/005/006), not from task descriptions:
`tools/dev-cli/endpoints/workflow-command.cs` (6-step release-mode pipeline: tag
assertion, tag-pin, ancestor-of-master, check-version, locate-run, download,
verify, push), `release-command.cs` + `release-guard.cs` (8 `dev release` guards
in order, dry-run, tag-then-push-then-gh-release-create with the
push-fails-unwind / release-create-fails-does-not-unwind asymmetry),
`check-version-command.cs` + `publish-state.cs` (three-state gate, package
precedence), `packable-project-service.cs` (derived `IsPackable` set, no
hand-maintained lists), `tag-assertion.cs`, `ci-run-promotion.cs` (run ordering,
artifact expiry handling, package-set verification), `ci-mode.cs`
(event→mode matrix), `.github/workflows/workflow.yml` (triggers, break-glass
`mode`/`confirm` dispatch inputs, OIDC `nuget/login` gating, upload-skip on
release runs), the devcli readme Migration Notes, `convention.md`, and the
458-002 task Notes for the exact outstanding branch-protection PUT (D8).

Also fixed the stale `workflow.yml:38` comment (`fetch-depth: 0` was attributed
to MinVer, which the repo does not use — replaced with the real reason: the
release gate's `git merge-base`/tag-pin/`ls-remote` checks need full history),
and linked the new guide from `documentation/developer/guides/overview.md` and
the root `readme.md`'s Contributing section.

**Self-check / verification:**
- Grepped the guide against the code read above — no contradictions found.
- `dotnet build timewarp-nuru.slnx 2>&1 | tail -2` — clean (workflow.yml comment
  is a YAML comment; cannot affect the build, confirmed anyway).
- All relative links added (`releasing.md` from `overview.md` and `readme.md`)
  point at files that exist.
- No trailing whitespace in the new/edited files.
- Nothing committed — left in the working tree per instruction.

**Facts that were awkward to document (signals for follow-ups, not fixed here):**
- Artifact retention: `workflow.yml`'s upload step sets no `retention-days`, so
  the actual effective retention is a repo Settings → Actions value I could not
  query (no `actions/artifact-and-log-retention` access from this sandbox) —
  documented as "GitHub default (90 days) unless the repo overrides it" rather
  than a confirmed number.
- The branch-protection PUT (458-002 D8) is still an outstanding operator action
  as of this writing — the guide documents it as a to-do, not as done.
- Task 456 (check-version distance warning) is still `to-do`/unimplemented — the
  guide's appendix entry is a forward pointer, not a description of current
  behavior.
