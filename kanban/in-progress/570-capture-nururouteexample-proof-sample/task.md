# Capture NuruRouteExample proof sample

## Description

Build a small sample pinned to Nuru 4.0 beta that declares a route with `[NuruRouteExample("add 2 3", Description = ...)]`. Run `--help` and `--capabilities` and commit the real captured output. Also test the documented `--json-args` syntax, including the stdin `-` form, which is documented but not yet runtime-verified. The capture is the evidence for a planned X post about worked examples shipping in `--help`/`--capabilities`, its image card (the route declaration next to its `--help` output), and a short silent terminal video (discover with `--capabilities`, then invoke).

## Requirements

- Sample pinned to Nuru 4.0 beta (not an older package).
- At least one route uses `[NuruRouteExample(...)]` with a Description.
- Commit real captured `--help` and `--capabilities` output from that sample (source-read of the README is not enough).
- Runtime-verify documented `--json-args` syntax, including the stdin `-` form; record pass/fail with the exact command used.
- Do not use the existing calculator-contracts sample as proof — it does not use the attribute.
- Make no claims about token counts or AOT numbers in the capture or notes.

## Goal (implementer restatement)

Produce real, reproducible evidence that a route's worked example ships in `--help` and
`--capabilities`, for a later X post, image card, and silent terminal video.

Acceptance:

- A small runfile sample that references the **published** `TimeWarp.Nuru` 4.0 beta package
  (a pinned `#:package` version, not the repo's project reference and not a 3.x package).
- One endpoint route declares `[NuruRouteExample("add 2 3", Description = "...")]`.
- `--help` (per-route and root) and `--capabilities` output captured by actually running the
  sample, committed as text files next to the sample.
- `--json-args` runtime-checked in the documented forms: inline `{...}`, `@path`, and stdin `-`
  (pipe and heredoc), with the exact commands, exit codes, and pass/fail recorded.
- The calculator-contracts sample is not used as the proof source.
- No token-count or AOT claims in the capture or notes.

## Checklist

- [x] Create a small pinned Nuru 4.0 beta sample with `[NuruRouteExample("add 2 3", Description = ...)]`
- [x] Run `--help` and save real output
- [x] Run `--capabilities` and save real output
- [x] Runtime-test `--json-args` (documented form) and stdin `-`; record results
- [x] Commit the capture files alongside the sample
- [x] Confirm calculator-contracts was not used as the proof source

## Session

- Created: 1419691 (2026-11-09)
- 2026-11-21 implementer: built `proof/calc.cs` against the published `TimeWarp.Nuru@4.0.0-beta.16`
  (the newest on nuget.org; source is at 4.0.0-beta.17, unpublished). Captured `--help`,
  `add --help`, `--capabilities`, and eight `--json-args` checks with `proof/capture.sh`.
  All eight passed, including stdin `-` from a pipe and from a heredoc.
- Observation: 4.0.0-beta.16 writes ANSI color codes when stdout is a pipe and `NO_COLOR=1` is
  set, so the plain `*.txt` captures are the raw `*.ansi.txt` bytes with escapes removed by sed.

## Notes

- Source: Busara REWORK verdict on the Nov 9 Nuru x Fowler brainstorm (Masoko ideas 1–3 folded into one proof capture that feeds the post, image card, and merged silent terminal video). Confirmed by Steven 2026-11-09 (Bangkok).

## Results

The proof lives in `proof/` beside this file. `proof/readme.md` lists each file and the command
that produced it.

- Sample: `proof/calc.cs`, `#:package TimeWarp.Nuru`, pinned to 4.0.0-beta.16 by a new
  `PackageVersion` in the root `Directory.Packages.props`. The restored assets list only
  `TimeWarp.Nuru/4.0.0-beta.16` (no project reference). The `Directory.Build.*` files in `proof/`
  keep the repository build and analyzers out.
- Audit fix: the first audit failed `cpm-consistency` on the inline `#:package TimeWarp.Nuru@4.0.0-beta.16`
  (missing PackageVersion, runfile with @version). The pin moved to `Directory.Packages.props` with a
  comment naming this proof. The recapture after the move was byte-identical to the first one.
- Route: `[NuruRoute("add", ...)]` with `[NuruRouteExample("add 2 3", Description = "Adds 2 and 3, prints 5")]`.
- `proof/capture/help-add.txt`: per-route help with an `Examples:` section, `add 2 3`, and its description.
- `proof/capture/help-root.txt`: root help.
- `proof/capture/capabilities.json`: the `add` endpoint carries
  `"examples": [{"command": "add 2 3", "description": "Adds 2 and 3, prints 5"}]` and the
  `invocation` object (`stdin: "-"`, `filePrefix: "@"`).
- `proof/capture/json-args.txt`: 8 PASS, 0 FAIL. Inline object, stdin `-` (pipe and heredoc),
  `@path`, argv overriding JSON, an unknown key (exit 1), and the attached form (exit 1).
- The calculator-contracts sample was not used. The capture makes no token-count or AOT claims.

### Gates

- `./bin/dev build`: succeeded, 0 warnings, 0 errors. Rerun after the `Directory.Packages.props` edit: succeeded.
- Test files: none added or edited, and no production code changed. The proof is a standalone
  runfile outside the solution. `./kanban/in-progress/570-capture-nururouteexample-proof-sample/proof/capture.sh`
  is its check: 8 PASS, 0 FAIL.
- `ganda repo audit`: first run failed `cpm-consistency` (inline runfile version, see above). After the fix:
  passes, exit 0, with one advisory warning (`vscode-window-icon`, `.vscode/settings.json` missing, non-blocking, pre-existing).

### How to validate

Smoke:

```bash
kanban/in-progress/570-capture-nururouteexample-proof-sample/proof/capture.sh
git diff --stat -- kanban/in-progress/570-capture-nururouteexample-proof-sample/proof/capture
```

Expect: `8 PASS` and `0 FAIL`, and no diff under `capture/`. The regenerated output matches the
committed capture. `capture/help-add.txt` shows `Examples:` / `add 2 3` / `Adds 2 and 3, prints 5`.

