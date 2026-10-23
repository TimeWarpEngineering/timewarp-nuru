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

## Goal (implementer restatement)

Add a Nuru diagnostic that warns when an app calls unfiltered `DiscoverEndpoints()` in a
compilation where that call pulls in endpoints meant for other apps (the Jaribu CI multi-mode
assembly). Prove the rule finds the right call sites before any test changes.

Acceptance:

- New id in the Endpoints family (`NURU_A003`), Warning, listed in `AnalyzerReleases.Unshipped.md`.
- Reported from the generator's own app model (one app per `Build()`), at the `DiscoverEndpoints()`
  call, with the endpoint count it pulls in and a `Map<TEndpoint>()` suggestion.
- Silent for a single-app compilation, for a single file with many apps (standalone test runs),
  for `DiscoverEndpoints(typeof(Group))`, and for `samples/`.
- Opt-out: `#pragma warning disable NURU_A003`, or run the file standalone. Documented in the
  diagnostic description and the AGENTS rule.
- Roslyn-hosted tests: multi-file reports, single app silent, single file with many apps silent,
  pragma opt-out suppresses.
- CI test assembly builds with a temporary `NoWarn` for this id only (task 522); no test converted.
- Results list every reported site against the `.DiscoverEndpoints()` call sites under `tests/`,
  which tests exercise discovery itself and which can move to `Map<T>()`.
- CI tests green after `ganda runfile cache --clear`; changelog entry.

## Checklist

- [x] Analyzer and diagnostic id, Unshipped entry
- [x] Analyzer tests (multi-app reports, single-app silent, opt-out)
- [x] Validation report: reported locations against the 71 call sites; samples clean
- [x] Temporary suppression in the CI test assembly so CI stays green until the conversion
- [x] AGENTS rule updated to point at the diagnostic
- [x] CI tests green; changelog entry

## Review

- Round 1: Done

## Notes

- Follow-ups, not in this task: convert the flagged tests to `Map<T>()` and re-measure generated size and CI compile time and memory; possibly split the CI assembly in two; a design note on how multi-mode Jaribu compilation interacts with Nuru code generation (for example, scoping discovery per file in multi-mode).
- Related: 501 (startup and benchmarks), 520 (telemetry benchmark), 521 (PR #348, blocked by these CI runner shutdowns).

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-23). Steven: "we should first create an Analyzer if possible to catch this. And then validate we can. Before changing."
- 2026-10-23 implementer (Claude Opus 5.5, ganda task work): NURU_A003 in the generator, generator-63 tests, validation below, temporary CI `NoWarn`. `ganda repo audit` flagged `runfile-executable` on the new generator-63 runfile at commit time; `ganda repo audit --fix` set its executable bit.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-22T19:56:20Z
- Review oracle: tw-implementation-review, effort 2, roster general (Grok 4.7), session not reported, max-turns uncapped (review budget 120), round 1, disposition clean — 2026-10-23
- Review oracle: review by reviewer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 120) — 2026-10-22T20:13:01Z

## Results

### What changed

- **NURU_A003** (Endpoints, Warning), `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.endpoints.cs`,
  listed in `AnalyzerReleases.Unshipped.md`. Message: "DiscoverEndpoints() adds all {N} endpoints in this
  compilation to this app, and this compilation builds Nuru apps in {M} source files. Map the endpoints
  this app needs with Map<TEndpoint>()." The description names both opt-outs.
- Reported by `NuruGenerator` (there is no separate `DiagnosticAnalyzer` in this project; every NURU
  diagnostic comes from the generator, on the exact model it emits). `validation/discover-endpoints-validator.cs`
  runs once per app in `CreateGeneratorModelWithValidation`. The app is the generator's own unit: one per
  `Build()` call (`uniqueApps`). The endpoint count is the same `FilterEndpointsForApp` result the emitter uses.
- `AppModel` gains `DiscoverEndpointsLocation` (equatable `LocationInfo`, set by the interpreter's
  `DispatchDiscoverEndpoints`) and `BuildFilePath` (set by `AppExtractor`). The location spans
  `DiscoverEndpoints()` only, not the whole builder chain, so a `#pragma` on that line suppresses it.
  `LocationInfo.ToLocation(compilation)` binds it to the syntax tree, the same path NURU056 pragmas use.

### Trigger: why "apps in more than one source file", not "more than one app"

"More than one app" is the wrong trigger. A standalone Jaribu file builds one app per test:
`group-options-01-basic.cs` builds 18 apps, all with `DiscoverEndpoints()`, against the 5 endpoints
declared in that same file. That is intended, and with warnings as errors a "more than one app" rule
would break every standalone run of those files. What makes discovery wrong is that the compilation
holds apps from other files, whose endpoints the app was never meant to see. So the rule fires when the
distinct `Build()` files in the compilation number two or more. A normal CLI has one app, so it never
reports, however many files its endpoints live in.

Not reported, by design:
- `DiscoverEndpoints(typeof(Group))`: discovery is scoped to that group.
- An app that also calls `Map<T>()`: `FilterEndpointsForApp` gives `Map<T>()` priority, so discovery adds nothing.
- Zero endpoints to pull in: there is no cost.

### Validation (before any test change)

CI assembly built with the new generator and no suppression
(`dotnet build tests/ci-tests/run-ci-tests.cs -p:TreatWarningsAsErrors=false`, 5m15s):
**49 NURU_A003 reports**, each "adds all 121 endpoints ... apps in 153 source files".

| File | Reports |
|------|--------:|
| `group-options/group-options-01-basic.cs` | 18 |
| `auto/endpoint-nullable-option-01.cs` | 7 |
| `routing/routing-28-group-alias.cs` | 7 |
| `generator/generator-11-endpoints.cs` | 6 |
| `generator/generator-18-partial-class-options.cs` | 4 |
| `routing/routing-27-nested-route-groups.cs` | 4 |
| `routing/routing-26-optional-int-skip.cs` | 3 |
| **Total** | **49** |

Reconciliation against `grep -rn '\.DiscoverEndpoints()' tests` on master (73 lines; the 71 in the
description counted `*.cs` only, without the two `Directory.Build.props` comments):

| Group | Lines | Why not reported |
|-------|------:|------------------|
| Reported (table above) | 49 | Every compiled unfiltered call in the CI assembly. |
| Comments | 6 | `ci-tests/Directory.Build.props:165`, `devcli/Directory.Build.props:42`, `check-version-04:7`, `check-version-07:7`, `generator-19:193`, `generator-20:146`. |
| `#if !JARIBU_MULTI` | 2 | `generator-19:206` (`NoFilter_IncludesAll`) and `generator-20:322`: not compiled in the CI assembly. They run in the standalone phase, one file per compilation. |
| Source strings in Roslyn-hosted tests | 16 | `generator-49` (2), `-52` (3), `-55` (6), `-59` (3), `-60` (2): text fed to an in-memory compilation, and the files are in `CiTestExcludes`. |
| **Total** | **73** | |

No extra report and no missing compiled call site.

Single-file and sample checks:
- Standalone `dotnet build` of the 7 reported files plus `generator-19` and `generator-20` (the standalone
  files that run unfiltered discovery): exit 0, 0 NURU_A003 each.
- All 42 `samples/**/*.cs` that mention `DiscoverEndpoints` (every one is a runfile): `dotnet build` exit 0,
  0 NURU_A003 each.

### Which reported tests exercise discovery itself

None of the 49. Each file declares its own endpoints and tests routing, options, groups, aliases or
code generation of those endpoints, so each can move to `Map<TEndpoint>()` (grouped endpoints keep their
`[NuruRouteGroup]` prefix under `Map<T>()`). Discovery itself is covered elsewhere and is not flagged:
`generator-19` `NoFilter_IncludesAll` and `generator-20` (standalone only), the in-memory sources in
`generator-59`/`-60`, and `samples/endpoints/13-discovery`.

### Temporary suppression

`tests/ci-tests/Directory.Build.props`: `NURU_A003` added to `<NoWarn>` with a comment naming task 522
and the follow-up conversion. Remove it when the 49 sites move to `Map<T>()`.

### Tests

- `tests/timewarp-nuru-tests/generator/generator-63-discover-endpoints-across-apps.cs` (Roslyn-hosted,
  in `CiTestExcludes` and the `run-ci-tests.cs` standalone list like generator-28..60): 6/6 pass.
  - Apps in two files: one warning at `DiscoverEndpoints()` (line/span checked), count 2, 2 files.
  - Single app: silent. Many apps in one file: silent. `DiscoverEndpoints(typeof(Group))`, or discovery plus `Map<T>()`: silent.
  - `#pragma warning disable NURU_A003` around the call: the diagnostic is dropped by Roslyn's pragma
    filter (replayed through `CompilationWithAnalyzers`), and a control case without the pragma keeps it.
- `ganda runfile cache --clear`, then `dotnet run tests/ci-tests/run-ci-tests.cs`: multi-mode 2117 total,
  2111 passed, 6 skipped; standalone 35 files, 0 failed (multi-mode execute 129.9s).

### Docs

- `.agent/local/nuru-specific.md` CI Multi-Mode rule points at NURU_A003 and both opt-outs.
- `skills/tw-nuru/SKILL.md` notes NURU_A003 under `DiscoverEndpoints()`.
- `changelog.md` Unreleased/Added entry.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-63-discover-endpoints-across-apps.cs
sed -i 's/;NURU_A003<\/NoWarn>/<\/NoWarn>/' tests/ci-tests/Directory.Build.props
dotnet build tests/ci-tests/run-ci-tests.cs -p:TreatWarningsAsErrors=false 2>&1 | grep -c "warning NURU_A003"
git checkout tests/ci-tests/Directory.Build.props
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect:

- generator-63: 6 passed.
- The `sed` / build / `git checkout` lines turn the temporary suppression off for one build (about
  5 minutes) and print 49, one line per site in the table above (as of master d0a93b17 plus this
  branch). Do not pass `-p:NoWarn=...` instead: a global `NoWarn` replaces every referenced
  project's own list, and their build fails (CA1305 in the analyzers project).
- `run-ci-tests.cs` exits 0 with the suppression in place, with no NURU_A003 in its output.
- `dotnet build samples/endpoints/01-hello-world/endpoint-hello-world.cs` shows no NURU_A003.

### Review disposition

- Round 1, roster general, effort 2. Disposition **clean** (0 open; no wontfix).
- The restatement matches the task: “more than one app” was replaced, as the task allows, by “apps in more than one source file”, and Results record why.
- Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Session

- Merge conflict fix, 2026-10-23: PR 349 conflicts with master (cb2979a and later) in tests/ci-tests/Directory.Build.props and tests/ci-tests/run-ci-tests.cs, so no CI has run. Merge origin/master into the task branch, resolve both files keeping master's entries and the new generator-63 test entry, push so CI runs, and do not merge the PR. Requested by Usiku hourly PR check.
