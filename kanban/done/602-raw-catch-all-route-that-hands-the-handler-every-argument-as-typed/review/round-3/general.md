# Round 3 — general
**Date:** 2026-11-28
**Scope reviewed:** fix delta `1fa4a72e..c4ea3e2a` (product change: 4711ceed), plus a re-check of prior M1–M4 against the branch vs `master`

## Summary

Round 2 closed with 0 open, but PR #439 `test-multi` went red with CS0433. `generator-81-raw-catchall-diagnostic.cs` has a `#:project` for TimeWarp.Nuru.Analyzers, so compiling it into the multi assembly makes `Lexer` and `Token` ambiguous. 4711ceed fixes this the same way generator-80 is handled. It adds a `CiTestExcludes` entry in `tests/ci-tests/Directory.Build.props` and adds the file to the standalone list in `tests/ci-standalone/run-standalone-tests.cs`. That list's header comment changes from `78..80` to `78..81`, and the test file gains a Design region that says why. The file already guards `RunAllTests()` with `#if !JARIBU_MULTI`, the same as generator-80. parser-20 and routing-43 stay in the multi assembly and reference only TimeWarp.Nuru. routing-43 uses `.Map<T>()`, not unfiltered `DiscoverEndpoints()`.

The recorded validation covers this delta: a local `env -u NURU_CI_PHASE dotnet run tests/ci-tests/run-ci-tests.cs` with 2405 passed, 7 skipped, and 0 failed in the multi phase, and 53 standalone files with 0 failed (generator-81 passed 6). `./bin/dev build` and `ganda repo audit` pass. The next CI run of PR #439 is the merge proof. The host's CI gate owns that check.

The two master commits the branch is behind (`6dbdd5e7`, `afd42301`) touch only this task's kanban files. No product file collides, and master has no other `generator-81-*`.

Goal restatement against the task: the restatement matches the requirements. It adds the CS0433 paragraph and the round-1 acceptances. The "`myapp --help` stays with Nuru when a raw route starts with a literal" clause narrows "`--help` passes through on a raw route" to tokens after the route's literals. That reading follows from "every token after the route's literals" and is not a misread.

No full build, test run, audit, or hook was run for this round.

## Issues

None new. Prior M1–M3 stay fixed, and M4 stays wontfix (see `merged.md`).
