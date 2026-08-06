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

- [ ] Decide A vs B (record here; B preferred per review)
- [ ] If B: enable required status checks on master (green CI enforced at merge)
- [ ] If B: release job resolves tag commit → successful CI run → downloads `Packages-*` artifact; hard-fail with re-run guidance when absent; confirm artifact retention covers the bump→release window
- [ ] If A: add verify-samples + test steps to `RunReleaseWorkflowAsync`, renumber banners, verify failure aborts before pack
- [ ] Either: add `*.slnx` and `assets/**` to workflow path filters
- [ ] Update mode comments in `workflow-command.cs` and the releasing guide (458-008)

## Notes

Raised by operator during 458 review: "it should only release what is in master,
not something else" — that is Design B's property, byte-identical promotion. A
retest is only needed when a rebuild exists.
