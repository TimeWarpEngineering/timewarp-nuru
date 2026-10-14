# Review framework

## Budget (by-diff)

- Lines changed: 91
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Scope

**Date:** 2026-10-14
**Host task:** kanban/to-do/504-bump-version-to-300-for-the-stable-release/
**Diff scope:** branch `task/504-bump-version-to-300-for-the-stable-release` vs `master` (product files: `source/Directory.Build.props`, `changelog.md`, `documentation/user/guides/migrating-to-3.0.md`, `skills/tw-nuru/SKILL.md`; kitchen `task.md` excluded from findings)
**Plan / brief:** task.md checklist: `<Version>` 3.0.0, changelog `[Unreleased]` → `[3.0.0] - 2026-10-14` with lead paragraph, stable install wording in user docs and skill.
**Effort:** 1 (general only)
**Reviewer roster:** general (review oracle, claude opus-5.5, in-session; no subagent spawned for a 13-line product diff)
**Session IDs:** review oracle claude (session id not exposed to the headless run)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
