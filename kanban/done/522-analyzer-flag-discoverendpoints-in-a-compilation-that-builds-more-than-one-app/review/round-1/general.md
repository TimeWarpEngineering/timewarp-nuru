# Round 1 — general
**Date:** 2026-10-23
**Scope reviewed:** branch vs merge-base `380ace11` — NURU_A003 in the generator, generator-63, CI `NoWarn`, AGENTS rule, changelog, task Results validation.

## Summary

NURU_A003 is a Warning on the generator’s own app model: one app per `Build()` that returns `NuruApp`, reported at the unfiltered `DiscoverEndpoints()` call. It fires only when distinct `Build()` files are two or more, the filtered endpoint count is non-zero, and the app has neither a group filter nor `Map<T>()`. That count is `FilterEndpointsForApp`, the same priority the emitter uses, so a scoped `DiscoverEndpoints(typeof(Group))` or an app that also maps a type does not warn. A single app file stays silent, including a normal CLI whose endpoints live in other files (`tools/dev-cli` `.Build()` calls are Amuru commands, not `NuruApp.Build()`). The location is an equatable `LocationInfo` bound back to the syntax tree, the same path NURU056 pragmas use, and the span is `DiscoverEndpoints()` so a pragma on that line can suppress it.

The implementer restatement matches the task. The task’s literal “more than one app” trigger is the one the task says to replace if it is wrong; Results record why a standalone file with many apps must stay silent, and the acceptance text matches that trigger. No misread.

The recorded reconciliation holds on this tree. `.DiscoverEndpoints(` appears 87 times under `tests/`. Seven of those are this branch (the new `NoWarn` comment and generator-63). Of the other 80, 7 are `DiscoverEndpoints(typeof(...))` and do not match a `.DiscoverEndpoints()` grep, leaving 73. Those 73 split into the 49 compiled unfiltered calls in the seven listed files, 6 comments, 2 `#if !JARIBU_MULTI` calls, and 16 in-memory source strings in excluded generator tests. Filtered calls in `generator-19` and `generator-20-issue184-repro.cs` are compiled into the CI assembly and correctly omitted. `NoWarn` is only on `tests/ci-tests/Directory.Build.props`, names task 522, and is required because `RunAnalyzersDuringBuild` is false while the generator still reports. No test was converted. generator-63 is excluded from the multi-mode assembly and listed for the standalone phase, same as generator-60.

This sandbox could not re-execute `dotnet run` (runfile cache and NuGet mutex are outside the writable tree). The finding bar is the source and the call-site recount, not a fresh test run.

## Issues
