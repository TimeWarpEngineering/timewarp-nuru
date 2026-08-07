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

- [ ] Write `documentation/developer/guides/releasing.md`
- [ ] Fix `workflow.yml` comment: fetch-depth 0 is for git-tag history checks, not MinVer
- [ ] Link the guide from the developer docs index / readme as appropriate
- [ ] Keep in sync with 458-001/002/003/005/006 as they land (write last, or update per landing)

## Notes

Sequencing: this task documents the end state — do it after (or alongside the tail
of) the other 458 children rather than first.
