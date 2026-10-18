# Round 1 — plan_alignment
**Date:** 2026-10-18
**Scope reviewed:** `task.md` (Description, Requirements, Goal restatement, Notes, Results) against `git diff 5e512c0a..f522c6e2`, docs, sample, changelog

## Summary

The restatement matches the task closely. Most acceptance items are implemented, documented and tested: metadata-name resolution, the derivation rules, group precedence, JSON members, the bool and enum defaults, renderers, problem rendering and capabilities. Reading the code turned up four gaps against the requirements:
- `UseDirectInvoke()` breaks the build for handlerless endpoints.
- Mediator dispatch is now the default for existing `[NuruRoute]` endpoints. This is undocumented and probably breaks handlers that use a typed HttpClient.
- NURU_H008 is an Error, while the docs say an unseen handler "is still accepted at build time".
- The overhead measurement is too coarse to support the opt-out decision.

The version bump from 3.0.0 to 3.1.0-beta.1 follows the repo rule that a stable package takes no prerelease dependency.

## Misreads (restatement vs task)

None of substance. The restatement keeps every requirement, folds in the 506 findings and the group decision from Notes, and adds `IIdempotentCommand<T>`. One small omission: it does not repeat the Notes requirement to document that argv cannot supply a JSON member. The docs cover that point anyway.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/models/generator-model.cs:85
- Description: When every app uses `UseDirectInvoke()`, `__GetMediator()` is not emitted. Handlerless endpoints and `EmitMissingHandlerStartup` still call it, so the generated code fails with CS0103. That contradicts the docs and XML doc promise that "an endpoint with no nested handler still goes through the mediator". No test covers it. (Duplicate of general Issue 3.)
- Suggestion: Require the mediator whenever a direct-invoke app has a route with `NestedHandlerTypeName is null`, and add a standalone test.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs:372
- Description: Mediator dispatch is now the default for every command endpoint, including existing `[NuruRoute]` nested handlers, while the requirement says those keep working unchanged.
  - Handler dependencies now come from the mediator container, which does not register typed HttpClient services, so `samples/endpoints/15-httpclient` most likely fails at runtime.
  - The change also contradicts the 3.0 changelog statement "Apps that never inject a mediator type get no container code".
  - None of this is in the changelog.
- Suggestion: Register typed HttpClient services in the mediator container, or keep direct invoke as the default for nested-handler `[NuruRoute]` endpoints. Run sample 15, and record the behavior change in the changelog.
- Status: open

### Issue 3 — Severity: bug
- File: documentation/user/features/neutral-contracts.md:53
- Description: The docs say a handler that is not visible "is still accepted at build time", with startup returning exit code 1. In the code, NURU_H008 is `DiagnosticSeverity.Error` (diagnostic-descriptors.handler.cs:110), so the build fails unless the user suppresses an Error. The requirement says "a diagnostic when none is registered at build time *where it can tell*, otherwise a startup error". The CLI-forwarding / daemon split this task exists for can register handlers the compilation cannot see. Results also claims that "the route still exists so startup can report a missing registration", which is only true after suppression.
- Suggestion: Make NURU_H008 a Warning, or keep it an Error and document that relying on the startup check requires suppression. Align Results either way.
- Status: open

### Issue 4 — Severity: suggestion
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs:41
- Description: The overhead measurement is a single in-process JIT run at millisecond resolution. The mediator run goes first and pays the warm-up cost, and both 30-call loops round to 0 ms. There is no AOT or cold-start measurement, and the result is written to a hard-coded `/tmp` file from a CI test.
- Suggestion: Measure with ticks or a benchmark runfile, warm up both paths, measure AOT cold start with and without `UseDirectInvoke()`, and record the numbers and the decision in the task or docs. Move the timing out of the CI test.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:528
- Description: The requirement says a *registered* `IResponseRenderer<TResponse>` wins. The code instead constructs any *visible* renderer that has a public parameterless constructor. A DI-registered renderer is ignored, and a renderer that has constructor dependencies is silently skipped.
- Suggestion: Resolve renderers from the registered services, or document that "visible" is not the same as "registered" and prefer a registered renderer when one exists.
- Status: open

### Issue 6 — Severity: suggestion
- File: changelog.md:10
- Description: The changelog has a single `### Changed` entry. It leaves out the new public API (`UseDirectInvoke()`, `[NuruCliBinding(Group)]`), NURU_H008, renderer and problem rendering, the `Environment.ExitCode` reset, external-handler `[NuruRoute]` classes now becoming endpoints, and the default mediator dispatch.
- Suggestion: Add an `### Added` section and keep a `### Changed` entry for the dispatch default.
- Status: open

### Issue 7 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:223
- Description: `classSymbol.GetMembers()` returns only declared members, so public settable properties inherited from a base request class are not derived. The task and docs say "every public settable member".
- Suggestion: Walk the base types, skipping the group marker bases, or document that only declared members are derived.
- Status: open

### Issue 8 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/interceptor-emitter.cs:1156
- Description: The missing-handler check runs on every `ExecuteRouteAsync`, before `--help`, `--version` and `--capabilities`. (Duplicate of general Issue 13.)
- Suggestion: Run it once, or only when the affected route matches.
- Status: open

### Issue 9 — Severity: nit
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs:30
- Description: The new tests use `.DiscoverEndpoints()` against the repo rule for CI multi-mode tests. (See tests Issue 1, which confirms 6 CI failures.)
- Suggestion: Use `.Map<T>()`.
- Status: open
