# Repo matrix — TimeWarp-wide alignment

**Deferred by operator instruction (2026-08-06):** this review's mandate was
narrowed to "make the Nuru convention pristine — correctness first; do not worry
about migration work." Per-repo state audits and migration tasks are therefore
**not** part of 458's deliverable. This file records the known population so the
follow-up audit has a starting list.

Known TimeWarp repos consuming the DevCli pattern or publishing packages
(from 454-022 downstream review and org knowledge):

| Repo | Publishes NuGet | Notes |
|------|-----------------|-------|
| timewarp-nuru | yes (5 packages) | Reference repo; 458 child tasks apply here first |
| timewarp-terminal | yes | DevCli consumer |
| timewarp-ganda | yes | DevCli consumer |
| timewarp-amuru | yes | DevCli consumer |
| timewarp-builder | yes | DevCli consumer |
| timewarp-jaribu | yes | DevCli consumer |
| timewarp-architecture | yes | Real burned-version incident (task 456); uses release-published trigger |
| timewarp-flow | no packages known | Likely N/A — confirm in audit |

**F4 warning (applies before any audit):** any repo configured with
`checkVersionStrategy: git-tag` and publishing on `release: published` has an
inverted release gate today (aborts correct releases, passes mismatched ones) —
see findings F4. The DevCli fix ships from Nuru; consumers pick it up on package
update. If an aligned repo cuts a release before that update lands, expect the
abort behavior.

Follow-up (not created as tasks per instruction): once the Nuru child tasks land
and the convention is ratified, create one audit/alignment task per publishing repo
(or a batched parent in timewarp-flow) using `review/convention.md` as the spec.
