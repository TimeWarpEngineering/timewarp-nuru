# Fix workflow_dispatch mode: build and test by default, explicit break-glass release

Parent: 458 (finding F1 in `458-*/review/findings.md`).

## Description

`workflow-command.cs` `DetermineMode` maps `workflow_dispatch` → `Release`, but
`workflow.yml` only supplies OIDC NuGet credentials when `github.event_name ==
'release'`. A manual dispatch therefore runs the full release pipeline (check,
clean, build, pack) and dies on an unauthenticated push. The two components
disagree about what dispatch means; if credentials ever reach that path, dispatch
becomes a silent publish button.

Target (convention.md rule 4 + event matrix): dispatch defaults to **merge mode**
(clean → build → verify → test, no publish). Release via dispatch is break-glass
only: an explicit workflow input (`mode: release` plus a typed confirmation input),
and the YAML enables `nuget/login` + `--api-key` under exactly that same condition.

## Checklist

- [x] `DetermineMode`: `workflow_dispatch` → `Merge` (not `Release`) — extracted to pure `CiModeDetector` in DevCli content
- [x] `workflow.yml`: add dispatch inputs (`mode`, `confirm`); pass `--mode release` only when `mode == 'release'` and `confirm == 'release'`
- [x] `workflow.yml`: OIDC login condition covers release event OR confirmed break-glass dispatch — never plain dispatch; fail-loud guard step for bad confirm
- [x] Tests for mode detection matrix (16 tests: all `GITHUB_EVENT_NAME` values, explicit `--mode` incl. case-insensitive/bogus-throws/whitespace-throws/empty-fallthrough, precedence both directions)
- [ ] Note the behavior change in the releasing guide (deferred to 458-008 as planned)

## Results

Implemented in commits `dfbae796` (implementation) + `968a196b` (review fixes).

- **`source/timewarp-nuru-devcli/content/any/services/ci-mode.cs` (new):**
  `CiMode` enum + pure `CiModeDetector.DetermineMode(explicitMode, eventName)`.
  `workflow_dispatch` → Merge; explicit `--mode` wins (break-glass); unknown
  non-empty `--mode` throws ArgumentException (fail loud — a typo must not
  silently downgrade a release to a no-op build). Ships in DevCli package
  content; readme gains a services row + migration note (downstream repos with
  a local `CiMode` enum in copied workflow-command.cs hit a loud CS0101 on
  package bump — deliberate forcing function, documented).
- **`tools/dev-cli/endpoints/workflow-command.cs`:** DetermineMode is a thin
  env-reading wrapper; local enum deleted; header documents the event mapping.
- **`.github/workflows/workflow.yml`:** dispatch inputs `mode` (choice
  merge|release, default merge) + `confirm`; fail-loud guard when mode=release
  with confirm ≠ 'release'; OIDC login gated on release event OR confirmed
  dispatch; run step passes `--mode release --api-key` only on confirmed
  dispatch. Confirm string is never shell-interpolated (boolean expression
  results only — no injection surface; verified in review).
- **Review (Phase 4b):** 2 rounds, 1 sonnet reviewer, effort 1. 5 findings →
  4 resolved (1 MED packaging/migration, 1 LOW fail-loud, 2 INFO), 1 INFO
  wontfix (standing actionlint gap, out of scope). Disposition:
  **accepted-exceptions** — see `review/disposition.md`.

### How to validate

Smoke:
1. `dotnet run tests/timewarp-nuru-tests/devcli/workflow-01-mode-detection.cs`
   → Expect: 16/16 passed.
2. Read `.github/workflows/workflow.yml`: dispatch has `mode`/`confirm` inputs;
   "Validate break-glass confirmation" step exists; `nuget/login` `if:` is
   `release event OR (dispatch && mode=='release' && confirm=='release')`.
3. GitHub UI (optional live check): Actions → CI/CD → Run workflow with
   defaults → Expect: pipeline runs merge mode (clean→build→verify→test), no
   NuGet login, no publish. Run with mode=release, confirm blank → Expect: run
   fails fast at "Validate break-glass confirmation".

Automated gate: `ganda runfile cache --clear && dotnet run tests/ci-tests/run-ci-tests.cs`
→ Expect: 0 failed (last run: 1417 total / 1410 passed / 7 skipped / 0 failed).

Not in scope: releasing-guide documentation (458-008); downstream repo
migrations (org rollout).

## Notes

Decided 2026-08-08 (operator): **keep the break-glass path** — confirm-gated
dispatch release stays as the recovery escape hatch (e.g. beta.69-style stranded
releases). Plain dispatch remains non-publishing.

### Implementation plan (Phase 2, 2026-08-08)

1. **New `source/timewarp-nuru-devcli/content/any/services/ci-mode.cs`** —
   `CiMode` enum (moved from workflow-command.cs) + pure static
   `CiModeDetector.DetermineMode(explicitMode, eventName)`;
   `workflow_dispatch` → Merge; explicit `--mode` always wins. Lives in DevCli
   content so the existing services glob compiles it into tools/dev-cli and the
   454-022 test-props pattern compiles it into tests (no `[NuruRoute]`, so no
   multi-mode endpoint contamination).
2. **workflow-command.cs** — DetermineMode becomes a thin env-reading wrapper
   over the pure function; header comment updated; local enum deleted.
3. **workflow.yml** — dispatch inputs `mode` (choice merge|release, default
   merge) + `confirm` (must equal `release`); fail-loud guard step when
   mode=release with bad confirm; nuget/login `if:` covers release event OR
   confirmed dispatch, never plain dispatch; run step three-way branch passing
   `--mode release --api-key` only on confirmed dispatch. Confirm string is
   never shell-interpolated (expression results only — no injection surface).
4. **New test `tests/timewarp-nuru-tests/devcli/workflow-01-mode-detection.cs`**
   (skeleton copied from check-version-01): full auto-detect matrix incl.
   `workflow_dispatch → Merge`, explicit-override matrix incl. case-insensitive
   and bogus values, break-glass precedence both directions.
5. **Wire ci-mode.cs** into `tests/ci-tests/Directory.Build.props` and
   `tests/timewarp-nuru-tests/devcli/Directory.Build.props`.
6. Verify: dev CLI compiles; standalone test runfile passes; full CI run
   (`ganda runfile cache --clear` first — DevCli content changed).

Breaking process change: dispatch no longer attempts to publish. This is the
documented intent of convention.md; call it out in the release notes for DevCli
consumers.
