# Backfill #region Purpose in every source file and restore TWA0004 to error

## Description

TimeWarp.SourceGenerators 1.0.0-beta.15 (adopted in task 579) ships TWA0004: every C# file needs a
`#region Purpose` block. 290 of 366 files under `source/` have none, so 579 set
`dotnet_diagnostic.TWA0004.severity = none` in `.editorconfig`. This task backfills the regions
and removes that override so the package default (error) applies.

## Requirements

- Every `.cs` file under `source/`, `tools/dev-cli/`, and `tests/` carries a `#region Purpose`
  block (a single `//` line suffices) that says what the file is for; follow the tw-agent-context-regions
  skill (Purpose, optional Design, optional Open Questions). Generated files and `global-usings.cs`
  may use `generated_code = true` in `.editorconfig` instead, as timewarp-architecture does.
- Remove the TWA0004 override added by 579 from `.editorconfig`.
- `dev build` clean. Reference: timewarp-state task 099 did this backfill.

## Goal (implementer restatement)

Make the repo pass TWA0004 (TimeWarp.SourceGenerators beta.13+: every C# file needs a
`#region Purpose`) so the analyzer can run at its default error severity.

Acceptance:
- Every hand-written `.cs` under `source/`, `tests/`, and `tools/dev-cli/` has exactly one
  `#region Purpose` with an honest line about what the file is for (Design only where a real
  rationale exists, e.g. folded from an existing header comment). `*.g.cs` files are generated
  and exempt.
- Runfile shebang and `#:` directives stay first; nothing else in the files changes behavior.
- No TWA0004 suppression remains. As 579 landed, there is no `TWA0004.severity` line: 579 held
  the package at beta.11 instead (`package-pin-currency.hold`). Bumping needs TWA0038 (task 581)
  too, so this task keeps the hold and narrows its reason to 581/TWA0038.
- TWA0004 is proven clean against beta.15; `dev build`, `dev test`, and `ganda repo audit` pass.

## Checklist

- [x] Purpose regions added (source/)
- [x] Purpose regions added (tools/dev-cli/, tests/) — tools/dev-cli/ already had them
- [x] TWA0004 override removed — none existed; 579 pinned beta.11 instead. The hold now names only
      TWA0038 / task 581 (bumping to beta.15 needs 581's file splits too)
- [x] Build clean

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-13)
- 2026-11-13 implementer (claude): merged origin/master (kitchen land commit) before the first
  commit. Backfilled 527 files (290 source/, 237 tests/) via six parallel helper batches; leading
  file-description comments were folded into Purpose (rationale into Design), not duplicated.
  `*.g.cs` (3 internals-visible-to files) stay exempt as generated code.
- The `source-file-line-cap` ratchet (`.timewarp/line-cap-baseline.json`, can only shrink) failed
  for 30 already-over-cap files that grew by 1-6 lines. Trimmed each back to its baseline without
  behavior change: folded leading banners into the regions, merged decorative separator pairs,
  collapsed one-line `<summary>` blocks, dropped class summaries that duplicated the new Purpose,
  dropped a few bare `// Arrange` lines, and removed an orphaned `<summary>` in mcphost-01 that
  documented a type no longer there. generator-27 was 498 lines and would have crossed 500, so
  its banner was folded too.
- Verification against the target package: temporarily set TimeWarp.SourceGenerators to beta.15
  with TWA0038 silenced (not committed). `dev build` passed with zero TWA0004. A probe file with no
  region raised `error TWA0004`, so the rule was live. The CI test assembly built clean too.
- `dev test` flaked twice with FileNotFoundException under `~/.local/share/dotnet/runfile/`:
  concurrent walks (005, 006) cleared the shared runfile cache mid-run. The rerun with a private
  `XDG_DATA_HOME=/tmp/t580/xdg` passed. This was environmental; none of the 580 changes caused it.
- Audit boy-scout: `ganda repo audit --fix` set `window.title` in `.vscode/settings.json`, which
  is gitignored, so there is nothing to commit. `ci-code-coverage` stays an advisory warning
  (not auto-fixable, CI workflow change, out of scope).

## Results

TWA0004 (#region Purpose) is satisfied repo-wide. Every hand-written `.cs` under `source/`,
`tests/`, and `tools/dev-cli/` has exactly one `#region Purpose`. The package stays pinned at
beta.11. The pin reason in `.editorconfig` (`package-pin-currency.hold`) and
`Directory.Packages.props` now names only TWA0038 / task 581, which splits the over-cap files and
then bumps to beta.15. After that bump, TWA0004 runs at its default severity of error.

### Gates

| Gate | Command | Result |
|------|---------|--------|
| Build | `./bin/dev build` | pass (exit 0, 0 errors) |
| Test | `XDG_DATA_HOME=/tmp/t580/xdg ./bin/dev test` | pass (exit 0): multi-mode 2179 passed of 2185 (rest skipped), standalone 0 failed. Two earlier runs on the shared cache failed on cache files deleted mid-run (see Session) |
| Audit | `ganda repo audit` | pass (exit 0) after line-cap trims; 2 advisory warnings (ci-code-coverage, vscode-window-icon, the latter fixed locally by `--fix`) |

### How to validate

Smoke:
```bash
git ls-files 'source/*.cs' 'tests/*.cs' 'tools/dev-cli/*.cs' | grep -v '\.g\.cs$' \
  | xargs grep -L '#region Purpose'          # prints nothing
# Optional: prove against the analyzer (do not commit)
sed -i 's/TimeWarp.SourceGenerators" Version="1.0.0-beta.11"/TimeWarp.SourceGenerators" Version="1.0.0-beta.15"/' Directory.Packages.props
printf '\n[*.cs]\ndotnet_diagnostic.TWA0038.severity = none\n' >> .editorconfig
./bin/dev build 2>&1 | grep -c TWA0004       # 0
git checkout -- Directory.Packages.props .editorconfig
ganda repo audit                             # exit 0, source-file-line-cap PASS
```

Expect: no file listed by the grep; zero TWA0004 diagnostics on beta.15; audit exit 0 with
`source-file-line-cap` passing (no file grew past its baseline).

## Notes

Follow-up of 579.
