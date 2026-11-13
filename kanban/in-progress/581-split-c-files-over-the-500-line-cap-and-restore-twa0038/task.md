# Split C# files over the 500-line cap and restore TWA0038

## Description

TimeWarp.SourceGenerators 1.0.0-beta.15 (adopted in task 579) ships TWA0038: a 500-line cap per
file. 44 files are over it, including the generator core: dsl-interpreter.cs (2447),
json-args-emitter.cs (2296), endpoint-extractor.cs (2080), route-matcher-emitter.cs (1938),
interceptor-emitter.cs (1908), service-extractor.cs (1197), handler-invoker-emitter.cs (1108),
operation-endpoint-extractor.cs (1063), handler-extractor.cs (1046), nuru-generator.cs (848),
plus tools/dev-cli/endpoints/workflow-command.cs (1415) and tests/.../operation-endpoint-derivation.cs
(1544). 579 set `dotnet_diagnostic.TWA0038.severity = none` in `.editorconfig`.

## Requirements

- Split each over-cap file by responsibility (partial classes per concern for the emitters and
  extractors; test files by scenario group). No behavior change; generator snapshot tests stay green.
- Use the full list from `find source tools tests -name "*.cs" | xargs wc -l | awk "$1>500"`.
- Remove the TWA0038 override from `.editorconfig` so the package default (warning, error under
  warnings-as-errors) applies.
- Because generator and emitter work regresses under Grok fix passes, run this with the Claude
  implementer profile.

## Goal (implementer restatement)

Bring every C# file in `source/`, `tools/` and `tests/` to 500 lines or fewer so the TWA0038
line cap from TimeWarp.SourceGenerators 1.0.0-beta.15 can be enforced, then adopt beta.15.

Actual repo state at claim: the package is still pinned to beta.11 through a
`package-pin-currency.hold` in `.editorconfig` `[ganda.audit]` (not a `TWA0038.severity = none`
line), and `.timewarp/line-cap-baseline.json` grandfathers the 44 over-cap files for the ganda
`line-cap` audit check.

Acceptance:

- Each of the 44 over-cap files is split by responsibility: product classes into `partial`
  files named `<stem>.<concern>.cs`, each with its own `#region Purpose`; test runfiles into
  sibling runfiles (`NNb-…` naming, as `repl-03b`) by scenario group. Pure moves, no behavior change.
- Explicit file lists (CI excludes, standalone list, devcli `Compile Include`s) carry the new files.
- `TimeWarp.SourceGenerators` bumped to 1.0.0-beta.15, the pin hold removed, no TWA0038 override.
- The line-cap baseline no longer lists the split files.
- `./bin/dev build`, `./bin/dev test` and `ganda repo audit` pass.

## Checklist

- [x] Generator emitters and extractors split
- [x] dev-cli workflow-command.cs split
- [x] Test files split
- [x] TWA0038 override removed, build clean, CI green (local gates; CI runs on the PR)

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-13)
- Implement (2026-11-13, implementer-claude under ganda task work): six parallel helper agents did the
  mechanical splits on disjoint file sets; the lead fixed the build, test and audit fallout.
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5, response model claude-sonnet-5-5), session 09211056-5a2d-4f23-9a18-90645ce521fb, max-turns 500, Done — 2026-11-13T12:55:34Z
- Review oracle: Grok 4.7 (grok, vendor xai), body tw-implementation-review, effort 3, roster general, turn cap 200, session id not exposed, disposition clean — 2026-11-13
- Review oracle: review by reviewer-cursor (cursor, vendor xai, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 200), artifacts general.md, merged.md, disposition.md — 2026-11-13T13:02:54Z

## Results

All 44 C# files over 500 lines are split. The largest file in `source/`, `tools/` and `tests/` is now
499 lines (`operation-endpoint-derivation.cs`). `TimeWarp.SourceGenerators` is at 1.0.0-beta.15 and
TWA0038 runs at the package default (error under warnings-as-errors).

What changed:

- **Product classes** (`source/`, `tools/dev-cli/endpoints/`): each over-cap type is now `partial`, with
  `<stem>.<concern>.cs` siblings that each have their own `#region Purpose`. Members moved verbatim.
  Fields, constants and constructors stay in the original file. The splits:
  - analyzers: `dsl-interpreter` (9 files), `json-args-emitter` (8), `route-matcher-emitter` (7),
    `endpoint-extractor` (7), `interceptor-emitter` (6), `service-extractor` (5), then
    `handler-invoker-emitter`, `operation-endpoint-extractor`, `handler-extractor`, `nuru-generator`,
    `extension-method-lowerer`, `ir-app-builder`, `app-extractor` and `service-validator`.
  - other projects: `workflow-command` (5), `nuget-version-service` (3), `nuru-daemon-server`,
    `search-index`, `nuru-app`, `GenerateNuruJsonContextTask` and `nuru-mcp-tool-catalog` (2 each).
- **`release-command.cs`**: its `Handle` method alone was about 435 lines, so moving whole members
  could not bring it under the cap. Steps 6, 7 and 10 are now private methods in
  `release-command.guards.cs`, alongside `LocateReleaseTargetAsync`. The guard helpers print the
  same messages and return false; `Handle` still sets `Environment.ExitCode = 1` and keeps the step
  order. There is a D7 note in its Design region.
- **`tools/dev-cli/telemetry-proof/proof.cs`**: this is a runfile, so `OtlpTraces` and `ProtoReader`
  moved to `otlp-traces.cs`, which `proof.cs` pulls in with `#:include` (supported by SDK 11.0.100).
  Boy-scout fix: the proof did not compile on master (IDE1006 on two local consts, RCS1021 on one
  lambda). Both are fixed by a rename and an expression-bodied lambda.
- **Test runfiles**: 22 new sibling runfiles use the `NNb-…` naming (`repl-03b` precedent), plus
  `operation-endpoint-derivation-{groups,initializers,shapes}.cs`. Each has its own namespace, test
  class and `Register()`. Test methods moved verbatim; per-file helpers are copied. A few tests moved
  back after a standalone build showed they needed types from the original file: generator-26
  circular runtime DI, mcphost-01 console capture and help-01 same-prefix. The CI multi-mode
  assembly still runs 2185 tests, the same count as before the split. The repo now has 2409
  test-method lines versus 2407; the extra two are the copied `Setup`/`CleanUp` in repl-18b.
- **Lists**: the three new standalone files (check-version-07b, generator-55b, generator-72b) are
  in `CiTestExcludes` and `run-standalone-tests.cs`. The two `nuget-version-service` partials are
  in both devcli `Compile Include` lists. `runfiles/generate-internals-visible-to.cs` was re-run,
  which added 23 test assemblies to the three `internals-visible-to.g.cs` files.
- **Config**: `Directory.Packages.props` moves beta.11 to beta.15. The `package-pin-currency.hold`
  is gone from `.editorconfig`; there was never a `TWA0038.severity` line.
  `.timewarp/line-cap-baseline.json` is now empty (`source-file-line-cap` passes).
- **Suppression baseline** (`analyzer-suppressions`):
  - Moved pragmas have their entries moved to the new paths:
    - daemon session: CA1031 ×2, CA2025
    - search-index query: CA2100
    - operation-endpoint-derivation-shapes: NURU_H008 and TWM001
    - generator-26b: NURU056
  - Copied header pragmas are removed rather than re-baselined:
    - The fixture types in the three new operation-endpoint-derivation files are `internal`,
      which clears CA1062 and CA1819 without a suppression.
    - generator-26b's CA1305 is fixed with `CultureInfo.InvariantCulture`; its CA1848 and CA1873
      were not needed.
    - The routes that needed RS0030 were not copied.
    - The help-01b test that needed RCS1163 moved back to help-01.
  - **Two CA1805 entries are new** and need reviewer attention: one narrow
    `#pragma warning disable/restore CA1805` around `Mute = false` (initializers) and one around
    `Unset = null` (shapes). These are the explicit default initializers the tests pin, and they were
    covered by the original file's header pragma. Removing either breaks its test: the flag
    binding fails, or the capability `defaultValue` goes missing. Neither could move back into the
    original, which is at 499 lines.

Follow-ups and risks:

- The telemetry proof (`dotnet run tools/dev-cli/telemetry-proof/proof.cs`) fails on master and here
  at the stage-children check (`span count 42`). The OTLP decoding in the moved code works: the
  first two proofs pass. The failure is pre-existing and not from this split; it is worth its own task.
- Building a test runfile standalone with `dotnet build` reports IDE1006, RCS1021, CA2016 and RCS1238
  on generator-15/26, repl-23, mcphost-01 and operation-endpoint-derivation. These come from the
  original files and reproduce on master's copies. Moved code carries them as-is.
- `ci-code-coverage` and `vscode-window-icon` audit warnings are pre-existing and non-blocking.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| build | `./bin/dev build` | pass: 8 projects, 0 warnings |
| test | `ganda runfile cache --clear && ./bin/dev test` | pass: multi-mode 2185 total, 2179 passed, 6 skipped; standalone 45 files, 0 failed |
| audit | `ganda repo audit` | pass (exit 0): 42 passed, 2 pre-existing warnings (`ci-code-coverage`, `vscode-window-icon`) |

The first audit run failed `analyzer-suppressions` (27 entries from moved or copied pragmas). The
baseline edits and fixes above came next, then build, test and audit were re-run with the results shown.
The post-commit attest on the first commit then failed `runfile-executable`: the 23 new test
runfiles were committed without the executable bit. A follow-up commit sets +x, and the attest
after it passes.

### How to validate

Smoke:

```bash
find source tools tests -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" | xargs wc -l | awk '$1>500 && $2!="total"'
grep -n "TimeWarp.SourceGenerators" Directory.Packages.props
grep -n "TWA0038\|package-pin-currency.hold" .editorconfig
./bin/dev build && ganda runfile cache --clear && ./bin/dev test
ganda repo audit
dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-derivation-shapes.cs
```

Expect:

- The `find` prints nothing (no file over 500 lines).
- `Directory.Packages.props` pins `TimeWarp.SourceGenerators` at `1.0.0-beta.15`, and `.editorconfig`
  has no TWA0038 or pin-hold line.
- Build passes; the test suite reports 2185 multi-mode tests (6 skipped) and 0 standalone failures.
- The audit exits 0 with `source-file-line-cap` and `analyzer-suppressions` passing.
- The shapes runfile prints `Total: 8 Passed: 8`.

### Review

Round 1, roster general, effort 3, turn cap 200. Session id not exposed. Counts: bug 0/0/0, suggestion 0/0/0, nit 0/0/0 (open/fixed/wontfix). Disposition `clean`. No wontfix and no escalation. The restatement matches the task: the live TWA0038 hold was the beta.11 pin, not a severity override, and this branch removes that hold.

Paths: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Review

- Round 1: Done

## Notes

Follow-up of 579. Consider child tasks per project if one PR is too large to review.
