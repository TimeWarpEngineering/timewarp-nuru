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

Breaking process change: dispatch no longer attempts to publish. This is the
documented intent of convention.md; call it out in the release notes for DevCli
consumers.
