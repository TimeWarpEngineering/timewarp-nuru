# Round 1 — general
**Date:** 2026-09-28
**Scope reviewed:** branch `task/477-triage-to-do-2026-09-28-close-superseded-tasks-rew` vs `origin/master` — kanban archives, the 437 move, and the 456 / 069 / 365 / 219 rewrites.

## Summary

The triage matches the repo. Archives 434, 436, 255, 257, and 258 each carry a `Closed 2026-09-28 (triage 477)` note; 437 exists only under `kanban/backlog/`. `help-model.cs` is the four filter properties named in the 434 note. Route examples exist as `NuruRouteExampleAttribute` and `WithExample`. `GitTagCheckService` and `CheckVersionStrategy` are gone; `NuGetVersionService` and `PublishStateClassifier` exist, and `check-version` prints `Version in source` / `Latest NuGet version`. Key-binding sources and the two cited REPL runfiles exist; `KeyBindingProfileName` defaults to `"Default"`. `NURU_DEBUG` is gone from the analyzers; `route-matcher-emitter.cs` still emits the DEBUG comments at lines 70, 72, 846, 847, and 850; `TreatWarningsAsErrors` is true under `source/` and false in the three props files 365 lists. `wc -l` over `tests/**/*.cs` matches 219's eleven paths and counts. `git diff --stat fdad4d13^..HEAD -- source tests samples` is empty. Two wording nits in the new notes point a later implementer the wrong way.

## Issues

### Issue 1 — Severity: nit
- File: kanban/to-do/456-check-version-warn-when-source-is-multiple-versions-ahead-of-last-release.md:46
- Description: The triage note says "Requirements below are updated accordingly." The rewritten requirements are the section above that note. A reader who looks below the note finds only the consumer notes, which still describe the old incident and do not restate the single NuGet methodology.
- Suggestion: Point at the requirements above, which already drop the git-tag strategy and the "both strategies" test case.
- Status: open

### Issue 2 — Severity: nit
- File: kanban/to-do/069-global-user-key-binding-profiles.md:15
- Description: The path correction names `NuruAppBuilder.AddRepl(...)` / `AddReplOptions(...)` as how apps enable the REPL. `AddReplOptions` is marked `[Obsolete("Use AddRepl() or AddRepl(Action<ReplOptions>) instead.")]`. The requirements bullet already says `AddRepl(...)` only.
- Suggestion: Name `AddRepl()` and `AddRepl(Action<ReplOptions>)`, and say `AddReplOptions` is obsolete.
- Status: open
