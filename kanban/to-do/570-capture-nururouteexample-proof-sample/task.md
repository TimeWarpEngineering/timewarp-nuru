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

## Checklist

- [ ] Create a small pinned Nuru 4.0 beta sample with `[NuruRouteExample("add 2 3", Description = ...)]`
- [ ] Run `--help` and save real output
- [ ] Run `--capabilities` and save real output
- [ ] Runtime-test `--json-args` (documented form) and stdin `-`; record results
- [ ] Commit the capture files alongside the sample
- [ ] Confirm calculator-contracts was not used as the proof source

## Session

- Created: 1419691 (2026-11-09)

## Notes

- Source: Busara REWORK verdict on the Nov 9 Nuru x Fowler brainstorm (Masoko ideas 1–3 folded into one proof capture that feeds the post, image card, and merged silent terminal video). Confirmed by Steven 2026-11-09 (Bangkok).
- Filed only; not launched. Do not start until Steven says so.
