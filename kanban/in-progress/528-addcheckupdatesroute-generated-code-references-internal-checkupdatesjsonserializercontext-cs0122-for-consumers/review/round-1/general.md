# Round 1 — general
**Date:** 2026-10-24
**Scope reviewed:** branch `task/528-addcheckupdatesroute-generated-code-references-int` vs merge-base `b41bfff3`, implementation commit `f2e18481`

## Summary

`AddCheckUpdatesRoute()` still opts in through a no-op builder method, and `CheckUpdatesEmitter` now emits `CheckUpdatesGitHubRelease.ParseReleases(json)` instead of the internal `CheckUpdatesJsonSerializerContext`. The context stays `internal`; the new public method is the same source-generated `JsonSerializer.Deserialize` call the consumer used to compile against, hidden with `EditorBrowsable(Never)`, and it is the public entry point the task allowed. `generator-65` compiles that call chain under assembly name `Gen65ConsumerApp`, asserts that name is absent from TimeWarp.Nuru's InternalsVisibleTo list, and requires zero errors; `NuruGenerator` extracts apps from `Build()`, so the chain is the one that sets `HasCheckUpdatesRoute` and emits the handler. The local-feed nuget.config maps `TimeWarp.Nuru` and `TimeWarp.Nuru.*` on their own trie nodes, so those ids come from the candidate feed and other packages from nuget.org. The implementer restatement matches the original task. This session's `dotnet build` stopped on sandbox NU1301 (NuGet repository-signature proxy 403); the committed `evidence/4.0.0-t528.2` probe row is PASS and the built `add-check-updates-route` app printed `pong`.

## Issues
