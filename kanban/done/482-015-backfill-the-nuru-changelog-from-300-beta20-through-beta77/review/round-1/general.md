# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commit d7c53758 changelog.md (3.0.0-beta.20 through beta.77 and the Unreleased scope line)

## Summary

Dated sections exist for beta.20 through beta.77, quiet or unpublished betas are called out, and the three beta.42 capabilities bullets were moved out of Unreleased. Heading dates all match either the GitHub release `publishedAt` calendar day (UTC) or the tag commit’s calendar day (UTC or +0700); beta.43 is the tag-commit day (2026-01-29), not the UTC publish day (2026-01-30). The beta.42 move, the beta.77 version window (2026-08-27 to 2026-09-25, no tag or NuGet package), and the remaining Unreleased bullets (migration guide, Mediator 14, `key-bindings`, MCP not packable) sit after `v3.0.0-beta.76`. Several sections still put a shipped change on the wrong beta, or drop a user-facing change that is in the tag range but not in the thin release notes.

## Issues

### Issue 1 — Severity: bug
- File: changelog.md:88
- Description: beta.70’s contents are knowable, and they are not the beta.71 delta. Tag `v3.0.0-beta.69` (`14b51a9a`, 2026-04-27) has `<Version>3.0.0-beta.70</Version>` in `source/Directory.Build.props`. The version bump to beta.70 is `a17b7802` (2026-04-27 11:36 +0700), before that tag; the bump to beta.71 is `bd192dd6` (2026-04-28), after it. NuGet has `3.0.0-beta.70` published 2026-04-27T08:16:44Z, about 90 seconds after GitHub release `v3.0.0-beta.69` (2026-04-27T08:15:09Z). There is no NuGet `3.0.0-beta.69` and no `3.0.0-beta.21` or `3.0.0-beta.77`. The check-version / `IRepoConfigService` (#187) / JSON-context / dependency bullets at lines 93–95 are in that beta.70 tree (ancestors of the mis-tagged release). The only user-facing commit in `v3.0.0-beta.69..v3.0.0-beta.71` is the `[Parameter(Description)]` help fix (`5b47c9e0`, PR #218, 2026-06-15), which belongs on beta.71 and already is. Saying beta.70 “cannot be confirmed” and that “changes from beta.69 to beta.71 are listed under beta.71” assigns the published beta.70 package to a beta.69 heading and implies its body is the later help fix.
- Suggestion: Move the lines 93–95 bullets onto beta.70, and note that the GitHub release was tagged `v3.0.0-beta.69` while the tree and the NuGet package are beta.70. State that beta.69 was not published. Keep only the per-command `--help` fix under beta.71. Delete the “cannot be confirmed” sentence.
- Status: open

### Issue 2 — Severity: bug
- File: changelog.md:103
- Description: Grouping beta.26 with beta.27 attributes beta.27’s MCP changes to a beta that does not contain them. `v3.0.0-beta.25` and `v3.0.0-beta.26` are the same commit (`ac2ede19`); `git diff` is empty, and the version property is still `3.0.0-beta.25`. NuGet has beta.25 and beta.27 and no beta.26. `v3.0.0-beta.26..v3.0.0-beta.27` adds `examples.json` / `GitHubCacheService` and the `CacheManagementTool` / `GenerateHandler` fixes (8 files, +414/−1251). The two GitHub release bodies are the same text, but the beta.26 tag does not have that diff. The heading lists those MCP changes as shared. A later beta in the group has a distinct user-facing change, and the group hides that.
- Suggestion: Give beta.27 its own section with the MCP bullets. Replace the beta.26 entry with one line: same commit as beta.25, not published to NuGet. Do not treat identical release-note text as the same ship.
- Status: open

### Issue 3 — Severity: bug
- File: changelog.md:348
- Description: The `--capabilities` flag is attributed to beta.24. It shipped in beta.22. Commit `6a8ae628` (`feat(capabilities): add --capabilities flag`, task 157, 2025-12-21) is an ancestor of `v3.0.0-beta.22`. That tag and `v3.0.0-beta.23` both contain `AddCapabilitiesRoute` and tests that call `RunAsync(["--capabilities"])`. beta.22’s section (line 370: “Changes from beta.20 to beta.22 are listed here”) does not mention the flag. beta.24’s Added list pairs it with per-command `--help` (#356). #356 (`c6fa1d93`) is in `v3.0.0-beta.23..v3.0.0-beta.24` and can stay there; #157 cannot.
- Suggestion: Move the `--capabilities` flag to the beta.22 Added list. On beta.24, keep per-command `--help` (#356). Mention source-generated capabilities emission only if that sentence is clearly a change of implementation, not the introduction of the flag.
- Status: open

### Issue 4 — Severity: bug
- File: changelog.md:379
- Description: beta.22’s breaking list omits removal of public `MapMultiple`. At `v3.0.0-beta.20`, `NuruCoreAppBuilder.MapMultiple` has three public overloads and the REPL uses them (`exit`/`quit`/`q`, `clear`/`cls`). Commit `00e61bef` (2025-12-20, inside `v3.0.0-beta.20..v3.0.0-beta.22`) deletes those overloads; `v3.0.0-beta.22` has no `MapMultiple` method, only a test comment about migrating to repeated `Map` calls. The beta.22 GitHub notes are six lines and do not mention it. The changelog records the `Map(pattern, handler)` / `MapDefault` removal from the same era and not this one. Callers of `MapMultiple` break in beta.22.
- Suggestion: Add a breaking bullet on beta.22: `MapMultiple` was removed; register each pattern with `Map` (commit `00e61bef`).
- Status: open

### Issue 5 — Severity: bug
- File: changelog.md:67
- Description: beta.72’s release notes are only PRs #219 and #220. The changelog’s task-454 sweep does not include two user-facing commits in `v3.0.0-beta.71..v3.0.0-beta.72`. `5bfe6aee` (2026-07-06) accepts multi-character single-dash options and removes the POSIX grouping heuristic, so a short option is no longer matched as a character inside a longer token (`-e` matched `-help`). `e3dea0c6` escapes `{`, quotes, and backslashes in generated help text; a description such as `greet {name}` previously produced invalid interpolated code (CS0103). Neither commit is in beta.71, and neither was reverted before `v3.0.0-beta.72`. beta.76’s U+0085/U+2028/U+2029 fix is a later, different escape.
- Suggestion: Under beta.72 Fixed, add the single-dash option change (and the removed grouping heuristic) and the help-emitter escaping of braces, quotes, and backslashes. Keep the existing release-process and task-454 bullets.
- Status: open

### Issue 6 — Severity: suggestion
- File: changelog.md:163
- Description: `(#442)` is not a GitHub pull request or issue. `gh pr view 442` and `gh issue view 442` both fail to resolve. The change is kanban task 442 (`442-fix-unit-type-ambiguity-in-source-generator-use-fully-qualified-timewarpnuruunit.md`) and was merged as PR #191 (`5c4d937f`, “v3.0.0-beta.57: Fix Unit type ambiguity in source generator”). The beta.57 release notes say “Closes #442”, and the commit message uses `(#442)` for that kanban id. Elsewhere this changelog uses `#NNN` for GitHub numbers and `task NNN` for kanban (for example line 51, `task 440, #222`). The fix itself is on the right beta: `c0a887ac` is in `v3.0.0-beta.56..v3.0.0-beta.57` and emits `global::TimeWarp.Nuru.Unit`.
- Suggestion: Cite `task 442` and PR `#191`, not `#442`.
- Status: open
