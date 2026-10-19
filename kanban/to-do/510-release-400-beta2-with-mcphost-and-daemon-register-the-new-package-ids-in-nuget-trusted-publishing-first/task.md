# Release 4.0.0-beta.2 with McpHost and Daemon: register the new package IDs in NuGet trusted publishing first

## Description

4.0.0-beta.1 (2026-10-18) shipped TimeWarp.Nuru, .Analyzers, .DevCli and .Search. Since then master gained two new package IDs that have never been published: `TimeWarp.Nuru.McpHost` (508-004, PR #330) and `TimeWarp.Nuru.Daemon` (508-005, PR #331). NuGet.org trusted-publishing (OIDC) policies are per package ID, so the release workflow's push step will fail on those two packages unless policies exist for them before the cut. The probe mode (`gh workflow run workflow.yml -f mode=probe`) tests login only, not per-ID policy, so it cannot confirm this.

Roslynk task 003 is blocked on a Nuru release that carries the 508-006 camelCase fix, so this cut should follow 508-006, and the McpHost and Daemon packages are what Roslynk needs for steps 7 and 9 of the 508 order.

## Depends on

- 508-006

## Requirements

- On NuGet.org, under owner TimeWarp.Enterprises, a trusted-publishing policy for each of `TimeWarp.Nuru.McpHost` and `TimeWarp.Nuru.Daemon` matching the existing four (repo `TimeWarpEngineering/timewarp-nuru`, workflow `workflow.yml`). This is a human step in the NuGet UI; record who did it and when.
- Confirm the release pipeline's package-set check includes the two new IDs (CI `Packages-*` artifact lists six packages).
- Bump `<Version>` in `source/Directory.Build.props` to 4.0.0-beta.2 with the changelog entry, after 508-006 is merged. The bump rides this task's branch.
- Cut per `/tw-release`: master CI green at the bump commit, `dev release --dry-run`, `dev release`, watch the `release:published` run, confirm all six on NuGet flatcontainer.
- Tell Roslynk 003 the version.

## Checklist

- [x] Trusted-publishing policies exist for McpHost and Daemon (human, NuGet UI). Steven: the policy is per repository, not per package ID, so no new policy was needed; confirmed by the push succeeding for both new IDs
- [x] 508-006 merged to master (PR #332, cc38a56f)
- [x] Version bump rode PR #332 (508-006 set 4.0.0-beta.2); master CI run 39199737123 green
- [x] `dev release --dry-run` clean, `dev release` cut tag v4.0.0-beta.2 at cc38a56f, release run 39202436725 green, six packages pushed
- [ ] Roslynk 003 notified with the version

## Notes

- If the push step fails on only the new IDs after policies are added: fix the policy and rerun the failed release run (`gh run rerun --failed`). Never cut a second tag.
- Both new packages ship 508-005's `[LocalOnly]`, 508-004's `[Destructive]`, and the `mcp` and `serve` built-in routes; the changelog on master already lists them under Unreleased.
- Related: 508, 508-004, 508-005, 508-006 (blocking), Roslynk 003.

## Session

- Created: 3920594 (2026-10-19)

## Results

- Tag `v4.0.0-beta.2` at cc38a56f; GitHub prerelease https://github.com/TimeWarpEngineering/timewarp-nuru/releases/tag/v4.0.0-beta.2.
- Release run 39202436725 (OIDC trusted publishing) pushed all six packages: TimeWarp.Nuru, .Analyzers, .DevCli, .Search, .McpHost (first publish), .Daemon (first publish), all 4.0.0-beta.2.
- Trusted publishing needed no change: the NuGet.org policy is scoped to the repository and workflow, and the two new package IDs pushed under it.
- Carries: 508-004 McpHost, 508-005 Daemon, 508-006 camelCase operation members and SYSLIB1031 fix (Roslynk 003's blocker).
- Open: Roslynk task 003 still has to be pointed at 4.0.0-beta.2 (Roslynk repo, not this one).
- Released: 2026-10-19, cockpit session (Claude). Cut with `dev release` from master at cc38a56f.
