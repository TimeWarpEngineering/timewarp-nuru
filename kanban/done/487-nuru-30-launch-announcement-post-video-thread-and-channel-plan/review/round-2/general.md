# Round 2 — general
**Date:** 2026-10-09
**Scope reviewed:** fix delta for M1–M6

## Summary

M1–M6 are fixed in the working tree. The review tally is 28 lettered findings: 20 fix-now, R-7 and S-6 fixed on the review task, and six post-3.0. The changelog backfill is not one of the 28, and none of those drafts still call the remainder declined. The uncommitted copy does not add a new false public claim. Post 7 is 244 on X's scale and the day-7 opener is 148, both under 280.

## Resolved prior

- M1 — fixed: `findings.md` Counts are 20 fix-now ids plus R-7/S-6, six post-3.0 (R-2, R-8, A-5, A-6, A-7, S-5/T-1), and the changelog is the extra token. Blog, release notes, Show HN, hooks, post 7, the day-7 opener, and task Results say 22 fixed and do not say declined. Post 7 is 265 Python chars / 244 with the URL counted as 23. The day-7 opener is 148 UTF-16.
- M2 — fixed: `release-notes.md` says `--group-filter` filters the JSON and `--capabilities --search` runs `nuru search`. `EmitSearchCapabilities` shells out to `nuru` with `search` and writes stdout. `PrintCapabilities` applies `groupFilter` to the endpoint list.
- M3 — fixed: the r/dotnet paragraph names `.EnableCompletion()` and `.AddRepl()` (both called in `demo/app.cs.txt`) and attributes the 5.9 MB binary to `dotnet publish app.cs -c Release -r linux-x64`, not `dotnet run`.
- M4 — fixed: release notes list `aliases`, kinds `query`, `command`, `idempotentCommand`, and `unspecified`, and `examples` only when declared. The emitter always sets `Aliases`. `Examples` is omitted when null. `MapMessageTypeToKind` defaults to `Unspecified`, serialized camelCase.
- M5 — fixed: `video-script.md` and task Results describe `performance.md` as Direct 3.3 MB and Mediator 4.8 MB AOT on .NET 9 with startup under 1 ms, and they no longer say those binaries omit REPL and completion.
- M6 — fixed: launch gate 4 points at `ids.Sum(i => (long)i)` and says the blog has no catch-all example. The blog has no `{*` sample.

## Issues

No issues.
