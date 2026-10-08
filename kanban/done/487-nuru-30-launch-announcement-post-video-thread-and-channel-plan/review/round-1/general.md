# Round 1 — general
**Date:** 2026-10-09
**Scope reviewed:** Launch drafts on `task/487-nuru-30-launch-announcement-post-video-thread-and` versus `origin/master` (merge-base `215b0426`): the blog, X and Nostr blips, and the task-487 folder (thread, channels, hooks, video script, release notes, demo). No product code changes. Claims were checked against `source/`, `changelog.md`, `tests/`, `kanban/done/482-…/review/findings.md`, `documentation/user/reference/performance.md`, and a `dotnet run` of `demo/app.cs.txt` on `TimeWarp.Nuru 3.0.0-beta.79`.

## Summary

The drafts are unpublished 3.0 launch copy. The agent-contract examples I re-ran match beta.79: flat `endpoints`, one top-level `invocation`, and `Error: Unknown key 'bogus' for 'deploy {env} --tag? {tag?}'. Known names: env, tag.` with exit 1. Every blip, thread post, and the day-7 opener is under 280 on X's scale (UTF-16 code units, each URL as 23). The false public sentences are the "21 of 28" review tally, the claim that `--capabilities --search` narrows the JSON document, and the r/dotnet line that `dotnet run` produces a 5.9 MB AOT binary.

## Issues

### Issue 1 — Severity: bug
- File: documentation/posts/blogs/2026-xx-xx-nuru-3.0-release.md:145
- Description: "They filed 28 findings. The 21 that had to be fixed before release went through 15 PRs, and the rest are scheduled after 3.0 or declined, with reasons." The same 21-of-28 tally is in `release-notes.md:45`, `channels.md:51` (Show HN), `hooks.md:23`, `x-thread.md:31` ("fixed 21 issues"), and `x-thread.md:44`. `review/findings.md` has 28 numbered findings (R-1–R-9, A-1–A-7, P-1, S-1–S-10 with T-1 aliased to S-5, D-1). Of those, 22 were fixed before the tag: the fix-now list is 20 finding ids, plus R-7 and S-6 "fixed on this task" (PR #282). The other 6 (R-2, R-8, A-5, A-6, A-7, S-5/T-1) are post-3.0. None of the 28 is declined; the wont-fix notes (AOT CS0436, no snupkg, analyzer Unshipped split) are outside that list. The Counts bullet's 21st token is the word "changelog", which is PR #297 / task 482-015, not one of the 28. The PR range itself is right: #283–#297 are 15 merged PRs stacked in #298.
- Suggestion: Say 28 findings, 22 fixed before the tag (20 of them, plus the changelog backfill, in #283–#297 stacked in #298; R-7 and S-6 on #282), and 6 scheduled after 3.0. Drop "declined" unless you name the unnumbered wont-fix notes separately.
- Status: open

### Issue 2 — Severity: bug
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/release-notes.md:30
- Description: "`--capabilities --search <q>` and `--group-filter <g>` narrow the document." `--group-filter` does filter the JSON (`PrintCapabilities` in `interceptor-emitter.cs`, group filter in `capabilities-emitter.cs`). `--search` does not. It calls `SearchCapabilitiesAsync`, which runs `nuru search` and writes that process's stdout (`capabilities-emitter.cs` `EmitSearchCapabilities`). `SearchQuery.Handler` prints `Found N result(s):` and pattern lines (`source/timewarp-nuru-search/endpoints/search-query.cs`), not a `CapabilitiesResponse`. If `nuru` is missing, the app prints the install hint and returns 1.
- Suggestion: Say `--group-filter` filters the JSON, and `--capabilities --search` shells out to `nuru search` (human listing, requires `TimeWarp.Nuru.Search`).
- Status: open

### Issue 3 — Severity: bug
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/channels.md:83
- Description: "The 1-file version: `#:package TimeWarp.Nuru`, map two routes, and `dotnet run app.cs` gives you help, completion for bash/zsh/fish/pwsh, a REPL, and a 5.9 MB AOT binary that starts in ~3 ms." `dotnet run` does not publish a binary. Native AOT for a file-based app comes from `dotnet publish` (the blog's `dotnet publish app.cs -c Release -r linux-x64` does set `PublishAot=true` by SDK default; the video script's extra `-p:PublishAot=true` is redundant, not required). Completion and the REPL are not implied by two `Map` calls: the demo calls `.EnableCompletion()` and `.AddRepl()` (`demo/app.cs.txt`). Help is on by default (`IrAppBuilder` sets `HasHelp = true`).
- Suggestion: Point at `dotnet publish` for the 5.9 MB binary, and name `.EnableCompletion()` and `.AddRepl()` if those features are part of the claim.
- Status: open

### Issue 4 — Severity: suggestion
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/release-notes.md:24
- Description: "Each endpoint carries `pattern`, `groupPath`, `kind` (`query` / `command` / `idempotentCommand`), `parameters`, `options`, and `examples`." On beta.79 the demo's `--capabilities` JSON has `groupPath` and `aliases` on every endpoint and no `examples` property. `Examples` stays null and is omitted (`CapabilitiesJsonSerializerContext` uses `WhenWritingNull`; the emitter skips the property when the route has none). `kind` can also be `unspecified`: `RouteDefinitionBuilder` defaults `MessageType` to `"Unspecified"` until `AsQuery` / `AsCommand` / `AsIdempotentCommand`, and `MapMessageTypeToKind` maps that to `Unspecified` (`capabilities-emitter.cs`). `capabilities-01-basic.cs` expects `"kind": "unspecified"`.
- Suggestion: List `aliases` as always present, say `examples` appears only when declared, and add `unspecified` to the kind list.
- Status: open

### Issue 5 — Severity: suggestion
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/video-script.md:23
- Description: "`documentation/user/reference/performance.md` claims 3.3 MB and 4.8 MB, but that is for apps without REPL and completion." The same sentence is in `task.md:107`. `performance.md` lines 24–29 label those sizes "Direct (AOT)" and "Mediator (AOT)" with startup "< 1 ms", measured on .NET 9 (line 78). It never says the binaries omit REPL or completion. The ~7–10 ms figure under `documentation/user` is tab-completion latency in `documentation/user/features/shell-completion.md`, not those binaries' startup. Public copy does not present 5.9 MB as the doc's number; this note would if a recorder repeats it.
- Suggestion: Say the doc's 3.3 MB / 4.8 MB table does not state a REPL or completion scope, and keep the caption on the measurement.
- Status: open

### Issue 6 — Severity: nit
- File: kanban/to-do/487-nuru-30-launch-announcement-post-video-thread-and-channel-plan/task.md:65
- Description: The launch gate says NURU_H002 "is a false positive that a reader trying the blog's catch-all example may hit." The blog has no catch-all (`{*…}` does not appear in `documentation/posts/blogs/2026-xx-xx-nuru-3.0-release.md`). A later fixer will not find that example. The analyzer false positive itself is not a new finding.
- Suggestion: Point at the `ids.Sum(i => (long)i)` snippet already in the gate, not at a blog example.
- Status: open
