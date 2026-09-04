# Round 1 — tests-infra
**Date:** 2026-09-04
**Scope reviewed:** `tests/` (especially `tests/ci-tests/`), `samples/`, `benchmarks/`, `runfiles/`, `tools/dev-cli/`, root/`source/` `Directory.Build.props`, `Directory.Packages.props`, `BannedSymbols.txt`, `.github/workflows/workflow.yml`, internals-visible-to generation, analyzer packing in packaging csprojs. Pinned product tree `648369f6` / origin-home `38480f57`, version `3.0.0-beta.77`.

## Summary

CI multi-mode inclusion for `tests/timewarp-nuru-tests/**/*.cs` is healthy after 454-001 (recursive glob + `CiTestExcludes`). The generator-28..42 Roslyn-hosted family is correctly excluded from multi-mode and actually invoked as a second phase in `run-ci-tests.cs`. Two committed, runnable tests are still never executed by CI: `generator-17` (excluded, comment says “run standalone”, not on the standalone list) and `check-version-04` (entire body `#if !JARIBU_MULTI`, not on the standalone list). Legacy `tests/scripts/` hand-lists diverge badly from the tree. Committed `internals-visible-to.g.cs` is stale vs current test stems (454-032 class drift). AOT harness script still points at deleted `bench-nuru-full`. No evidence of 454-025 scratch files or 454-026 net9/net10 analyzer packing mismatch. workflow.yml mode wiring looks sound; samples Directory.Build.props correctly isolates from root analyzers; aspire-otel `WithTerminal` sample is present and was validated under task 467 (not re-broken here).

## CI inclusion drift

Commands run (from repo root):

```bash
find tests -name '*.cs' | sort
# + Python compare of disk vs CiTestExcludes / Compile globs / run-ci-tests.cs standaloneTests
```

**Test directories on disk:** `timewarp-nuru-tests`, `timewarp-nuru-search-tests`, `timewarp-nuru-mcp-tests` only. No `timewarp-nuru-core-tests`, `timewarp-nuru-analyzers-tests`, or other `*-tests` roots. Comment at `tests/ci-tests/Directory.Build.props:20-21` correctly warns that a *new* `tests/<something>-tests` directory must get its own glob.

| Bucket | Count / notes |
|--------|----------------|
| Multi-mode included (`timewarp-nuru-tests/**/*.cs` minus excludes + search `*.cs` + mcp `*.cs` minus mcp-02) | 179 test `.cs` files (+ `lexer-test-helper.cs` helper) |
| `CiTestExcludes` (with reason comments) | 16 files |
| mcp-02 Exclude attr (reason → archived 454-033) | 1 file |
| Standalone second phase in `run-ci-tests.cs` | generator-28..40, 42 (14 files) — **invoked** |
| Excluded / inert but **not** run as documented standalone second phase | **generator-17**, **engine-01** (dead), **check-version-04** (compiled empty under `JARIBU_MULTI`) |
| Missing from both multi globs and excludes | **0** committed test `.cs` under the three `*-tests` trees |
| Non-test under `tests/` | `tests/ci-tests/run-ci-tests.cs`, `tests/scripts/*` (7 runners), `tests/test-apps/.../program.cs` |

**Globs:**

- `../timewarp-nuru-tests/**/*.cs` — recursive ✅
- `../timewarp-nuru-search-tests/*.cs` — **non-recursive** (today only top-level files exist)
- `../timewarp-nuru-mcp-tests/*.cs` — **non-recursive** (today only top-level files exist)

**generator-28 family:** listed in both `CiTestExcludes` and `standaloneTests`; foreach loop at `run-ci-tests.cs:38-57` actually runs them. Verified.

## 454 regression check

| 454 ID | Still present? | Evidence |
|--------|----------------|----------|
| 454-001 H1 CI hand-list excluding tests | **Partially regressed** | Recursive glob + `CiTestExcludes` in place (`Directory.Build.props:18-58`). New dirs under `timewarp-nuru-tests/` would be picked up. **But** `generator-17` is excluded with “run standalone” and is **not** in `run-ci-tests.cs` standalone list (454-001 class: committed test never runs in CI). `check-version-04` is a new instance of the same class via `#if !JARIBU_MULTI` with no second-phase wiring. search/mcp globs remain non-recursive; comment documents new `*-tests` roots must be hand-added. No `timewarp-nuru-core-tests` on disk (legacy scripts still reference it). |
| 454-025 M25 stale scratch files | **No** | No `temp-iconfig-test.cs` / `temp-test-chained` under `tests/` or IVT; exclude entry removed. |
| 454-026 M26 analyzer logging TFM packing | **No** | `source/Directory.Build.props:12` `AnalyzerDependencyTfm=net10.0`; both `timewarp-nuru.csproj:101` and `timewarp-nuru-analyzers.csproj:63` use `lib/$(AnalyzerDependencyTfm)/...Abstractions.dll`. No `lib/net9.0` packing paths remain. |
| 454-032 LOW BannedSymbols / TreatWarningsAsErrors / orphan docs/ / IVT | **Partially** | Single `BannedSymbols.txt` AdditionalFiles at root `Directory.Build.props:84`. TreatWarningsAsErrors comment matches `false` at root (`Directory.Build.props:53-56`) with `true` in `source/Directory.Build.props:7`. No orphan `docs/`. **IVT drift returned:** committed `internals-visible-to.g.cs` (last touch 2026-08-05) missing 24 current test stems including standalone `generator-39/40/42` and all newer `devcli/*` files. |

## Issues

### Issue 1 — Severity: bug
- File: `tests/ci-tests/Directory.Build.props:24-26,38` and `tests/ci-tests/run-ci-tests.cs:19-35`
- Description: `generator-17-local-function-config.cs` is in `CiTestExcludes` because it is a top-level-statements program (local-function `ConfigureServices` pattern) and cannot live in the multi-mode assembly. The exclude comment says to run it standalone, and 454-001 recorded it as “verified passing standalone,” but `run-ci-tests.cs`’s `standaloneTests` array never includes it. CI therefore never compiles or executes this committed regression test (454-001 silent-exclusion class).
- Suggestion: Add `generator-17-local-function-config.cs` to the `standaloneTests` list in `run-ci-tests.cs` (same pattern as generator-28+), or convert the scenario into a multi-mode-safe form and drop the exclude.
- Status: open

### Issue 2 — Severity: bug
- File: `tests/timewarp-nuru-tests/devcli/check-version-04-endpoint-zero-package.cs:3-12` (and absence from `tests/ci-tests/run-ci-tests.cs:19-35`)
- Description: The entire test file (namespace, `ModuleInitializer`, and cases) is wrapped in `#if !JARIBU_MULTI` so the multi-mode assembly compiles it as a no-op. The file comment says “Run standalone only,” and `tests/timewarp-nuru-tests/devcli/Directory.Build.props:35-45` correctly compiles `check-version-command.cs` for standalone runfiles — but CI’s second phase does not invoke this file. Endpoint-level coverage for the zero-package / delimiter-only `--package` guard (458-005) never runs on the CI path (`tools/dev-cli` → `run-ci-tests.cs`).
- Suggestion: Append `check-version-04-endpoint-zero-package.cs` to `standaloneTests` in `run-ci-tests.cs`. Prefer also listing it in `CiTestExcludes` (or documenting the `#if` inert pattern) so drift audits treat it like generator-17 rather than a silently empty include.
- Status: open

### Issue 3 — Severity: bug
- File: `benchmarks/aot-benchmarks/run-benchmark.sh:13`
- Description: Harness runs `publish/bench-nuru-full/bench-nuru-full`, but the only Nuru project on disk is `benchmarks/aot-benchmarks/bench-nuru/` with `<AssemblyName>bench-nuru</AssemblyName>` (`bench-nuru.csproj:3`). Recent result docs (`results/2026-01-21-aot-benchmark.md`) already use `bench-nuru`. The script will fail to locate the Nuru binary (and also omits other present benches: clifx, mcmaster, powerargs).
- Suggestion: Change the hyperfine entry to `publish/bench-nuru/bench-nuru` (and align the display name). Optionally extend the script to cover the other published projects or document why they are omitted.
- Status: open

### Issue 4 — Severity: suggestion
- File: `tests/ci-tests/Directory.Build.props:60,66` and `tests/ci-tests/Directory.Build.props:20-21`
- Description: `timewarp-nuru-search-tests` and `timewarp-nuru-mcp-tests` use non-recursive `*.cs` globs while `timewarp-nuru-tests` uses `**/*.cs`. Nested test files under search/mcp would be silently skipped. Separately, creating a new `tests/<name>-tests` root still requires a manual glob add (documented), which is the same drift class 454-001 fixed for the main tree.
- Suggestion: Switch both to `**/*.cs` (with bin/obj excludes), and/or add a CI drift guard that fails when a tracked `tests/**/*-tests/**/*.cs` is neither matched by a Compile include nor listed in `CiTestExcludes` / standalone phase.
- Status: open

### Issue 5 — Severity: suggestion
- File: `source/timewarp-nuru/internals-visible-to.g.cs:2` (and siblings under parsing/mcp); `runfiles/generate-internals-visible-to.cs:34-66,73`
- Description: Committed IVT lists are stale: 24 test stems present on disk are missing (including standalone `generator-39`, `generator-40`, `generator-42`, and newer `devcli/*` / `attestation-*` / `promotion-*` / `workflow-*` files). Generator header still claims `scripts/generate-internals-visible-to.cs` though the tool lives at `runfiles/generate-internals-visible-to.cs`. Multi-mode mostly uses assembly name `ci-tests` (which is present), so this may not break CI today, but standalone friends-access and 454-032’s “regenerate after new tests” discipline have regressed.
- Suggestion: Re-run `dotnet run runfiles/generate-internals-visible-to.cs`, commit the three `.g.cs` files, fix the generator banner path, and consider wiring regeneration into the build/dev-cli so drift cannot recur.
- Status: open

### Issue 6 — Severity: suggestion
- File: `tests/scripts/run-nuru-tests.cs:58-71`; `tests/scripts/run-all-tests.cs:138-139,206`; `tests/scripts/run-mcp-tests.cs:32-36`
- Description: Legacy hand-list runners diverge from the tree and from `ci-tests`. `run-nuru-tests.cs` points parser tests at nonexistent `timewarp-nuru-tests/parsing/parser/...` (actual path is `parser/`). `run-all-tests.cs` references deleted `timewarp-nuru-core-tests` and `timewarp-nuru-analyzers-tests` (skips with “NOT FOUND”). `run-mcp-tests.cs` still hand-runs broken `mcp-02` and omits `mcp-06` / `mcp-07`. Official CI uses `tools/dev-cli` → `run-ci-tests.cs` only, so this is not a green-CI hole, but the scripts are committed foot-guns.
- Suggestion: Delete or rewrite the scripts to delegate to `tests/ci-tests/run-ci-tests.cs`, or add a banner that they are unsupported and fail fast.
- Status: open

### Issue 7 — Severity: suggestion
- File: `tests/ci-tests/Directory.Build.props:27-28,39`; `tests/timewarp-nuru-tests/completion/engine/engine-01-input-tokenizer.cs:1-42`
- Description: `engine-01` remains in `CiTestExcludes` because it references `ParsedInput` / `InputTokenizer`, which are absent from `source/` (removed in #360). It does not compile standalone either. 454-001 already noted “Delete or rewrite”; the dead file is still committed.
- Suggestion: Delete `engine-01-input-tokenizer.cs` and its `CiTestExcludes` entry, or rewrite against the current completion tokenizer API and re-include it.
- Status: open

### Issue 8 — Severity: suggestion
- File: `tests/timewarp-nuru-tests/generator/generator-19-group-filtering.cs:190-199`; `tests/timewarp-nuru-tests/generator/generator-20-parameterized-service-constructor.cs:148-149,306-308`
- Description: Individual test methods (`NoFilter_IncludesAll`, Gen20KanbanQuery-related cases) are gated `#if !JARIBU_MULTI` with comments to run the file standalone. Those files are included in multi-mode (so other tests run) but the gated methods are never executed by CI’s standalone phase (unlike generator-28+). Same 454-001 “verified standalone” documentation gap as Issue 1, smaller blast radius.
- Suggestion: Either add these two files to the standalone second phase (accepting duplicate multi coverage for ungated methods) or extract the standalone-only cases into dedicated files that are CiTestExcluded + standalone-listed.
- Status: open

### Issue 9 — Severity: nit
- File: `samples/Directory.Build.props:12-13`
- Description: Comment still says TreatWarningsAsErrors is “Temporarily disabled while debugging source generator (see #365)” while the value has been the long-term samples default (`false`), matching the intentional root/samples vs `source/` split documented at root `Directory.Build.props:53-56`.
- Suggestion: Replace the “temporarily / #365” wording with the same permanent rationale used at the repo root.
- Status: open

### Issue 10 — Severity: nit
- File: `source/timewarp-nuru/timewarp-nuru.csproj:46`
- Description: `Microsoft.Extensions.Logging` sets `GeneratePathProperty="true"` but packing only consumes `PkgMicrosoft_Extensions_Logging_Abstractions` (`:101`). The non-Abstractions path property appears unused.
- Suggestion: Drop `GeneratePathProperty` from the Logging (non-Abstractions) PackageReference unless a pack step needs it.
- Status: open

## Notes (non-issues verified)

- **workflow.yml:** `contents: read`, standard `actions/checkout@v4` (not `pull_request_target`), mode matrix via `CiModeDetector` + break-glass `confirm=release`. Probe mode skips build/publish. No skip-tests path found on merge/PR.
- **454-026 / analyzer packing:** unified on `AnalyzerDependencyTfm=net10.0`.
- **BannedSymbols:** single AdditionalFiles include; no duplication.
- **Samples isolation:** `samples/Directory.Build.props` does not import root props (avoids analyzer/`TreatWarningsAsErrors` bleed). aspire-otel uses Aspire 13.5.3 `WithTerminal()`; task 467 marked working — no compile-break evidence in this pass. `verify-samples` discovers all shebang samples including aspire/editions when category filter is unset.
- **mcp-02:** still excluded with reason pointing at archived 454-033; not re-opened here.
- **generator-28..42 second phase:** present and invoked.
