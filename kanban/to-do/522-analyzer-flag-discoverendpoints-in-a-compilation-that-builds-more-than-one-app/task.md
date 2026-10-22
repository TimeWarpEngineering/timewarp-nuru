# Analyzer: flag DiscoverEndpoints() in a compilation that builds more than one app

## Description

CI compiles every Jaribu test file into one assembly (`tests/ci-tests/run-ci-tests.cs` plus the `<Compile Include>` globs in `tests/ci-tests/Directory.Build.props`). In that compilation, `.DiscoverEndpoints()` pulls in every `[NuruRoute]` endpoint from every test file, and the generator emits full matcher, help, capabilities and JSON-binder code for all of them into each app that calls it.

Measured on master d0a93b17 (2026-10-23), `artifacts/generated/run-ci-tests.cs/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs`:

- 230 MB, about 4.9 million lines, for 992 apps.
- Median app: 826 generated lines.
- About 80 apps: about 62,600 lines each, 4.2 million lines in total (about 85% of the file). These are the apps that call `.DiscoverEndpoints()`; there are 71 such calls under `tests/`, against about 103 `[NuruRoute]` endpoints.

The cost is quadratic: apps that call `DiscoverEndpoints()` multiplied by endpoints in the compilation. CI test compile time grew from about 2.5 min (2026-10-18) to 5-7 min (2026-10-21), and 7-13 min on slower GitHub runners on 2026-10-22, when runs began dying with "The runner has received a shutdown signal" right after the compile. Local peak memory rose about 16.5 GB during a full CI run on a 32-core box (other work was running too).

The repo rule (`.agent/local/nuru-specific.md`, "CI Multi-Mode Compilation") already says tests must use `.Map<TEndpoint>()` instead of `.DiscoverEndpoints()`. Nothing enforces it. This task adds the enforcement and proves it works **before** any test is changed.

## Requirements

- A Roslyn analyzer in `source/timewarp-nuru-analyzers` with the next free NURU id. Add it to `AnalyzerReleases.Unshipped.md` (never Shipped).
- Rule, to be validated in this task: report `DiscoverEndpoints()` when the compilation contains more than one Nuru app builder (`NuruApp.CreateBuilder` / `CreateEmptyBuilder` / equivalent entry points). In that case discovery pulls in endpoints meant for other apps. Use the semantic model and the generator's own notion of an app, not syntax strings. If "more than one app" turns out to be the wrong trigger, find the right one and record why: for example, endpoints declared in other files that no `Map<T>` in this app references.
- Message names the endpoint count it will pull in, where that is cheap to compute, and suggests `Map<TEndpoint>()`.
- Default severity: **Warning**, so a normal single-app CLI that uses discovery is never affected. A single-app compilation must not report. Samples under `samples/` must stay clean.
- Tests that genuinely test discovery need a sanctioned opt-out: a `#pragma warning disable`, or running them standalone (`JARIBU_MULTI` off). Document which in the diagnostic's help text and in the AGENTS rule.
- **Validate before changing anything:**
  - Build the CI test assembly with the analyzer and record every location it reports. Compare that list against the 71 `.DiscoverEndpoints()` call sites under `tests/`, and explain any difference.
  - Confirm no reports for `samples/` and for single-file standalone test runs.
  - Record in Results which of the reported tests exercise discovery itself and which can move to `Map<T>()`.
- **Do NOT convert any tests in this task.** Keep the new diagnostic from failing the CI build until the conversion lands: warnings are errors, so the CI test assembly needs a temporary, documented `<NoWarn>` or `.editorconfig` severity override for this id only, with this task id. The conversion is a follow-up task.
- Analyzer unit tests: multi-app compilation reports, single-app does not, the opt-out works.
- Run `ganda runfile cache --clear` before the CI tests.

## Checklist

- [ ] Analyzer and diagnostic id, Unshipped entry
- [ ] Analyzer tests (multi-app reports, single-app silent, opt-out)
- [ ] Validation report: reported locations against the 71 call sites; samples clean
- [ ] Temporary suppression in the CI test assembly so CI stays green until the conversion
- [ ] AGENTS rule updated to point at the diagnostic
- [ ] CI tests green; changelog entry

## Notes

- Follow-ups, not in this task: convert the flagged tests to `Map<T>()` and re-measure generated size and CI compile time and memory; possibly split the CI assembly in two; a design note on how multi-mode Jaribu compilation interacts with Nuru code generation (for example, scoping discovery per file in multi-mode).
- Related: 501 (startup and benchmarks), 520 (telemetry benchmark), 521 (PR #348, blocked by these CI runner shutdowns).

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23). Steven: "we should first create an Analyzer if possible to catch this. And then validate we can. Before changing."
