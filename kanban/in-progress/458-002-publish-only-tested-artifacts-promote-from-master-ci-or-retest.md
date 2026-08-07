# Publish only tested artifacts: promote from master CI, or retest

Parent: 458 (finding F2 in `458-*/review/findings.md`, revised 2026-08-07).

## Description

The release run today rebuilds from source with a floating SDK
(`setup-dotnet: '10.0.x'` resolves at run time) and pushes those fresh binaries
without running tests. So the published packages are **never** the bits any test
run exercised — release does not ship "what is in master," it ships a new build of
the same source. Verified enablers: master branch protection has no
`required_status_checks` (PR CI is advisory), the release event never reads the
commit's CI status, and the workflow path filters omit `*.slnx` / `assets/**`.

Two coherent designs (convention.md rule 7); current rebuild-without-retest is the
only incoherent one:

- **Design B — build-once / promote (preferred).** Master merge CI builds, tests,
  uploads the `.nupkg` set (already uploads `Packages-{run_number}`; version is
  final because the props bump merged first). Release job: no rebuild — locate the
  successful CI run for the tag's commit, download its artifacts, run
  check-version gate, push those exact bytes. Fail loudly if no green run /
  artifact exists for the commit.
- **Design A — rebuild + retest (fallback).** Keep the rebuild, insert
  `verify-samples → test` between build and pack.

## Checklist

- [x] Decided 2026-08-08 (operator): **Design B — build-once / promote**. Design A not chosen.
- [ ] Enable required status checks on master (green CI enforced at merge)
- [ ] Release job resolves tag commit → successful CI run → downloads `Packages-*` artifact; hard-fail with re-run guidance when absent; confirm artifact retention covers the bump→release window
- [ ] **Drop workflow path filters entirely** (decided 2026-08-08 — CI runs on every PR/push; closes the `*.slnx`/`assets/**` gap; kanban/docs PRs cost minutes, accepted)
- [ ] Update mode comments in `workflow-command.cs` and the releasing guide (458-008)

## Notes

### Implementation plan (Phase 2, 2026-08-08) — key decisions (all live-verified)

- Release pipeline becomes: tag-gate → check-version → **locate-run →
  download-artifact → verify → push**. No Clean/Build/Pack in release mode;
  merge CI already produces+uploads the nupkg set (GeneratePackageOnBuild +
  existing `Packages-{run_number}` upload; live artifact from the beta.71 run
  verified non-expired at 53 days).
- D2: run selection = successful workflow.yml runs at the tag's HEAD sha,
  push-event first then newest; walk candidates until a non-expired
  `Packages-*` artifact is found (release-event runs at the same sha won't
  have one post-change).
- D3: pure logic (run ordering, artifact selection incl. expiry, bidirectional
  version-pinned set verification, JSON DTOs) in shared content
  `ci-run-promotion.cs`; gh orchestration stays repo-local in
  workflow-command.cs. AOT: DTOs added to DevCliJsonContext.
- D5: `PackProjectsAsync` deleted (unused private method fails build anyway;
  a pack fallback would invite pushing untested bytes).
- D6: workflow.yml gains `actions: read` permission + `GH_TOKEN` env (gh on
  runners needs both); path filters deleted from push+pull_request.
- D8: sequencing — workflow changes merge to master FIRST, then the
  branch-protection PUT (exact command in plan; context `ci`, strict false,
  preserves enforce_admins + 0-approval review requirement). PUT is an
  operator/post-merge step.
- D9: break-glass/local release uses the same promotion path (gh user auth;
  clear failure if gh missing). **Closes the 458-005 untagged
  double-break-glass residual**: release mode never builds locally, so two
  local builds can no longer mix under one version through the pipeline.
- D10: artifact expiry/missing → hard fail with `gh run rerun <id>` guidance
  (rerun executes the same sha — tested-bytes property preserved).
- Tests: promotion-01..04 (run selection, artifact selection, set
  verification, real-payload JSON round-trip through source-gen context).

Raised by operator during 458 review: "it should only release what is in master,
not something else" — that is Design B's property, byte-identical promotion. A
retest is only needed when a rebuild exists.
