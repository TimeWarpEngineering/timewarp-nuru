# Round 1 — general

**Date:** 2026-11-14
**Scope reviewed:** Commit `496d2b3f` (README and `documentation/user/**` snippets) checked against the source public API and against `## Goal (implementer restatement)`. Gate evidence taken from `## Results` and `### How to validate` only.

## Summary

The change rewrites the listed broken snippets onto the current fluent API: `CreateBuilder()` and `EnableCompletion()`, `WithGroupPrefix` … `Done()`, single-argument `Map` plus `WithHandler`, handler parameter names that match route segments, and `UseTelemetry(options => options.ActivitySources.Add("MyApp.Commands"))`. Returned `int` values are taught as terminal output; samples set `Environment.ExitCode`. That matches `NuruApp.RunAsync`, `NuruCallContext.CurrentExitCode`, and the overlap rules (`NURU_R003`: a top-level `{*args}` shares the empty required signature with `Map("")` and scores higher). Anonymous returns became named records, with task 585 still open for the generator; `{message:string}` stays, with task 586 still open, which is what the task asks when the source says the constraint is valid. The goal restatement matches the original task. The recorded consumer check, build, test, and audit cover the change. No findings.

## Issues
