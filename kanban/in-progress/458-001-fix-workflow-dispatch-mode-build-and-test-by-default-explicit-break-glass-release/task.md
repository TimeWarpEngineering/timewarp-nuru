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

- [ ] `DetermineMode`: `workflow_dispatch` → `Merge` (not `Release`)
- [ ] `workflow.yml`: add dispatch inputs (`mode`, `confirm`); pass `--mode release` only when `mode == 'release'` and `confirm` matches required phrase
- [ ] `workflow.yml`: OIDC login condition covers release event OR confirmed break-glass dispatch — never plain dispatch
- [ ] Tests for mode detection matrix (all `GITHUB_EVENT_NAME` values + explicit `--mode`)
- [ ] Note the behavior change in the releasing guide (458-008)

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
