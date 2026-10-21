# Review framework — task 508-017

**Date:** 2026-10-21
**Host task:** kanban/to-do/508-017-daemon-a-client-from-another-build-must-not-stop-a-server-with-calls-in-flight/
**Diff scope:** branch `task/508-017-daemon-a-client-from-another-build-must-not-stop-a` vs `master` (merge-base `5f916790`), commit `583cbbbf`. Product files: `source/timewarp-nuru-daemon/` (client, launcher, options, paths, protocol, server, readme), `documentation/user/features/served-apps.md`, `tests/timewarp-nuru-daemon-tests/daemon-01-server.cs`. Surrounding: `changelog.md` Unreleased, idle-stop and build-check call sites.
**Plan / brief:** Socket per build, and refuse (never replace) when two builds share one socket. A client must not stop a server that has calls in flight. Status names the build. Idle timeout still reaps unused servers. Docs explain two builds.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** grok `01a15c0d-bddb-7211-8558-a8e2fcaf8e2e` (2026-10-21)

## Budget (by-diff)

- Lines changed: 542
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
