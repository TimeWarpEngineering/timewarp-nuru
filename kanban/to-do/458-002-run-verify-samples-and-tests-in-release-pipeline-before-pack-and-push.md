# Run verify-samples and tests in release pipeline before pack and push

Parent: 458 (finding F2 in `458-*/review/findings.md`).

## Description

`RunReleaseWorkflowAsync` in `tools/dev-cli/endpoints/workflow-command.cs` is
`check-version → clean → build → pack → push` — no `verify-samples`, no `test`. A
release is cut from a tag that can point at any commit, so the published artifacts
may never have been tested in the run that publishes them.

Target (convention.md rule 7): release pipeline is
`check-version → clean → build → verify-samples → test → pack → push`. Nothing is
pushed from a run that did not build and test those exact artifacts.

## Checklist

- [ ] Insert verify-samples and test steps between build and pack in `RunReleaseWorkflowAsync`
- [ ] Renumber the step banners (currently 1/5..5/5)
- [ ] Any test failure aborts before pack — verify non-zero exit propagates
- [ ] Update the mode comment block at the top of `workflow-command.cs`
- [ ] Update the releasing guide (458-008) pipeline description
