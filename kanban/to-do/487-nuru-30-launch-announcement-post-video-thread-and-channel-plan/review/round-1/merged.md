# Round 1 — merged findings
**Date:** 2026-10-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: documentation/posts/blogs/2026-xx-xx-nuru-3.0-release.md:145
- Description: The drafts say 21 of 28 findings were fixed before release, and that the rest were scheduled or declined. `kanban/done/482-…/review/findings.md` has 28 lettered findings. 22 were fixed before the tag (20 fix-now ids, plus R-7 and S-6 on PR #282). Six are post-3.0 (R-2, R-8, A-5, A-6, A-7, S-5/T-1). None of the 28 is declined. The Counts bullet's 21st token is the changelog backfill (PR #297), not a finding. The range #283–#297 stacked in #298 is correct. Same tally in `release-notes.md:45`, `channels.md:51`, `hooks.md:23`, `x-thread.md:31`, `x-thread.md:44`, and `task.md` Results.
- Suggestion: Say 22 fixed before the tag. Put the 15-PR split and #282 only where there is room. Drop "declined" for the remainder of the 28.
- Source: general
- Disposition notes: Fixed on this task. Blog, release notes, Show HN, hooks, thread post 7, and the day-7 opener now say 22 fixed before the tag. The long form (20 + changelog in #283–#297, two more on #282, six post-3.0) is in the blog, release notes, and task Results. "Declined" is gone. Post 7 stays at 265 Python chars / 244 on X's scale. The day-7 opener is 148 on X's scale.

### M2 — Severity: bug — Status: fixed
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/release-notes.md:30
- Description: "`--capabilities --search <q>` and `--group-filter <g>` narrow the document." `--group-filter` filters the JSON. `--search` runs `nuru search` via `SearchCapabilitiesAsync` and prints that tool's stdout (`capabilities-emitter.cs` `EmitSearchCapabilities`). A missing `nuru` exits 1 with the install hint.
- Suggestion: Split the two flags. Say `--search` shells out to `nuru search`.
- Source: general
- Disposition notes: Fixed on this task. Release notes now say `--group-filter` filters the JSON and `--capabilities --search` runs `nuru search`.

### M3 — Severity: bug — Status: fixed
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/channels.md:83
- Description: The r/dotnet draft says `dotnet run app.cs` gives you help, completion, a REPL, and a 5.9 MB AOT binary. `dotnet run` does not publish. Completion and the REPL come from `.EnableCompletion()` and `.AddRepl()` on the demo, not from two `Map` calls. Help is on by default. File-based `dotnet publish` does default `PublishAot=true`, so the blog's publish command is fine.
- Suggestion: Attribute the binary to `dotnet publish`, and name `.EnableCompletion()` and `.AddRepl()`.
- Source: general
- Disposition notes: Fixed on this task. The r/dotnet paragraph names `.EnableCompletion()` and `.AddRepl()`, and attributes the 5.9 MB binary to `dotnet publish app.cs -c Release -r linux-x64`.

### M4 — Severity: suggestion — Status: fixed
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/release-notes.md:24
- Description: The notes say every endpoint carries `examples`, and list kinds as only `query` / `command` / `idempotentCommand`. `examples` is omitted when null. `aliases` is always emitted. `kind` can be `unspecified` until the route is marked.
- Suggestion: List `aliases` as always present, `examples` only when declared, and add `unspecified`.
- Source: general
- Disposition notes: Fixed on this task. Release notes list `aliases`, include `unspecified`, and say `examples` appears only when declared.

### M5 — Severity: suggestion — Status: fixed
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/video-script.md:23
- Description: The production note says `performance.md`'s 3.3 MB and 4.8 MB figures are for apps without REPL and completion. The doc labels them Direct (AOT) and Mediator (AOT), startup under 1 ms, on .NET 9. It does not mention REPL or completion. The same sentence is in `task.md` Results.
- Suggestion: Describe the table as written, and keep the caption on the measured demo.
- Source: general
- Disposition notes: Fixed on this task. Video script and task Results now describe the Direct / Mediator table on .NET 9.

### M6 — Severity: nit — Status: fixed
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/task.md:65
- Description: Launch gate 4 says a reader of the blog's catch-all example may hit `NURU_H002`. The blog has no catch-all. The snippet that matters is already in the gate (`ids.Sum(i => (long)i)`).
- Suggestion: Point at that snippet, not at the blog.
- Source: general
- Disposition notes: Fixed on this task. The gate points at the `ids.Sum(i => (long)i)` snippet and says the blog has no catch-all example.

## Duplicates / conflicts

- One general reviewer. No overlaps to collapse.
- Orchestrator re-verified M1 against `findings.md` and `gh pr view 282` / `283–298`, M2 against `EmitSearchCapabilities`, and M4 against the capabilities emitter (`Aliases` always emitted, `WhenWritingNull` omits `examples`, `MapMessageTypeToKind` defaults to `Unspecified`). M3's AOT half matches a file-based `dotnet publish` that reports `PublishAot=true`. The known launch gates (CS9137, Mediator prerelease, migration-guide shape, NURU_H002 itself) were not refiled.
