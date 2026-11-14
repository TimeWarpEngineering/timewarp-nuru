# Bump Microsoft.CodeAnalysis.CSharp.CodeStyle to 5.12.0 and fix the IDE0028 sweep

## Description

PR #409 (feature/generator-fixes-2026-11-14, 2026-11-15) moved `Microsoft.CodeAnalysis.CSharp` and
`Microsoft.CodeAnalysis.Analyzers` to 5.12.0 because TimeWarp.SourceGenerators 1.0.0-beta.16 requires
Roslyn 5.12. `Microsoft.CodeAnalysis.CSharp.CodeStyle` stayed at 5.9.0 on purpose: at 5.12.0 the
CodeStyle analyzers report IDE0028 (collection initialization can be simplified) as a build error
(warnings are errors in this repo) across the analyzers project. Probe on that branch:

- 90 IDE0028 errors in 19 files, all under `source/timewarp-nuru-analyzers/`
  (validation/service-validator.cs, generators/extractors/service-registration-methods.cs,
  generators/emitters/completion-data-extractor.cs, generators/dependency-graph-builder.cs,
  generators/extractors/extension-method-lowerer.cs, and others)

This task finishes the pin currency: bump CodeStyle and make the code conform.

## Goal (implementer restatement)

Move the last Roslyn-family pin, `Microsoft.CodeAnalysis.CSharp.CodeStyle`, from 5.9.0 to 5.12.0 so it
matches `Microsoft.CodeAnalysis.CSharp`/`.Analyzers`, and make the code compile clean under the new
analyzer set without suppressing anything.

Acceptance:

- `Directory.Packages.props` pins CodeStyle at 5.12.0.
- Every IDE0028 (and any other new IDE diagnostic) is fixed in code via the code fix (collection
  expressions), not by `#pragma`, `NoWarn`, or a severity downgrade; a severity change is allowed only with
  a reason and this task id in `.editorconfig`.
- No behaviour change: generator output stays identical (generator tests stay green).
- `./bin/dev build`, `./bin/dev test`, and `ganda repo audit` pass; `ganda nuget outdated` no longer lists CodeStyle.
- `## Results` records the before/after diagnostic count and each gate result.

## Checklist

- [x] `Directory.Packages.props`: `Microsoft.CodeAnalysis.CSharp.CodeStyle` 5.9.0 → 5.12.0
- [x] Fix every IDE0028 (collection expressions / simplified initializers); use the code fix, do not suppress
- [x] Any other new IDE diagnostics 5.12 turns on: fix the same way, or justify a severity change in `.editorconfig` with a reason and this task id
- [x] `ganda runfile cache --clear`, `./bin/dev build`, `./bin/dev test` green
- [x] `ganda repo audit` passes; `ganda nuget outdated` no longer lists CodeStyle
- [x] Results section with the before/after count

## Results

`Microsoft.CodeAnalysis.CSharp.CodeStyle` 5.9.0 → 5.12.0 in `Directory.Packages.props`. The only new diagnostic
5.12 turns on here is IDE0028. Every site was fixed with the IDE0028 code fix (`dotnet format style
--diagnostics IDE0028`), which rewrites `new(comparer)` / `new(comparer) { … }` / `new T(source)` to
C# 15 collection-expression arguments (`[with(comparer), …]`). No `#pragma`, `NoWarn`, or `.editorconfig`
change; the suppression baseline is unchanged.

### Before / after

| Project | Before (IDE0028 errors) | After |
|---|---|---|
| `source/timewarp-nuru-analyzers` | 90 in 19 files | 0 |
| `source/timewarp-nuru-build` | 12 (6 sites × 2 builds) in 2 files | 0 |
| `source/timewarp-nuru`, `source/timewarp-nuru-mcphost` | not reached (build stopped earlier); 10 sites in 8 files | 0 |
| `source/timewarp-nuru-devcli/content` (runfile sources, outside the slnx) | 3 (failed 3 standalone check-version tests) | 0 |
| `tools/dev-cli` | 1 (would break the next `dev self-install`) | 0 |

`tests/timewarp-nuru-tests/devcli/nupkg-layout-02-csproj-gate-parity.cs` got the same one-line rewrite as the
`nupkg-layout-check.cs` line it mirrors (CI tests set `EnforceCodeStyleInBuild=false`, so it was not an error
there; kept for parity and for standalone runs).

Hand touch-ups: `boolean-converter.cs` was re-indented after the fixer emitted `with(…)` at column 0.
The fixer also reordered usings in `add-configuration-locator.cs` and `mcphost/global-usings.cs`; those
two unrelated changes were reverted. Runfile sources outside the solution (devcli content, tools/dev-cli,
the test) were edited by hand with the same rewrite the fixer produced.

No behaviour change: `[with(x)]` lowers to the same constructor call; the generator snapshot/emit tests are green.

### Gates

| Gate | Command | Result |
|---|---|---|
| Build | `./bin/dev build` | PASS: 8 builds, 0 warnings, 0 errors |
| Test | `ganda runfile cache --clear && ./bin/dev test` | PASS: multi-mode 2233 total, 2227 passed, 6 skipped; standalone 48 files, 0 failed (first run failed 3 standalone tests on the devcli-content IDE0028 sites; fixed, rerun green) |
| Audit | `ganda repo audit` | PASS (exit 0). 1 advisory warning, `vscode-window-icon`: `.vscode/settings.json` missing in this worktree (git-ignored, machine-specific; no repo change possible) |
| Currency | `ganda nuget outdated` | "All packages are up to date!"; CodeStyle 5.12.0 up to date |

`dotnet run --file tools/dev-cli/dev.cs -- self-install` was run once after editing `tools/dev-cli/`; it compiled clean.

### How to validate

Smoke:

```bash
grep CodeStyle Directory.Packages.props
ganda runfile cache --clear && ./bin/dev build && ./bin/dev test
git grep -nE "\\[with\\(" -- source tools tests | wc -l
```

Expect: CodeStyle pinned at 5.12.0; build reports 0 warnings / 0 errors with no IDE0028; `./bin/dev test`
ends with "Tests completed successfully!" (standalone 0 failed); 61 `[with(` lines (origin/master has 0),
and no `#pragma warning disable IDE0028` / `NoWarn` added (`git diff origin/master -- . | grep -E "IDE0028|NoWarn"`
shows nothing).

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-15)
- 2026-11-15 implementer-claude: merged origin/master, re-folderized the kitchen, bumped CodeStyle, swept IDE0028 (see Results).
- Implement oracle: implemented by implementer-claude (claude, vendor anthropic, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-11-14T18:26:35Z

## Notes

Mechanical sweep, no behaviour change intended. Keep the analyzer project's generated output byte-identical
(generator tests cover it). Pin comment in Directory.Packages.props already names 5.12 as the Roslyn floor.
