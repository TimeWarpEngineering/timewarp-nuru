# Round 1 — general
**Date:** 2026-11-15
**Scope reviewed:** task 497 product diff vs merge-base `b41073ac`: `Directory.Packages.props`, `tests/ci-tests/Directory.Build.props`, `tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs`. Kitchen `## Goal (implementer restatement)`, `## Results`, and `### How to validate`.

## Summary

CsCheck 4.9.1 is pinned under the Testing group in `Directory.Packages.props` and pulled in by `#:package CsCheck` on the new runfile. `tests/ci-tests/Directory.Build.props` does not add a second `PackageReference` (the comment names NU1504); transitive file directives are already on, so the multi-mode compile sees the directive. Nothing under `source/` references CsCheck. The build-blocking walks are 1000 cases from `Check.Seed` or `PinnedSeed` (`0N0XIzNsQ0O2`). `WalkAsync` prints `CsCheck_Seed=` from `PCG.ToString(state)` before `Generate(pcg, null, ...)`, which is the same null-min generate CsCheck 4.9.1 uses when it replays a `CsCheck_Seed`. `SampleAsync` runs only when `NURU_ARGV_FUZZ_EXPLORE=1`, and the file does not call `Faster`. `WithoutKnownFindings` and the well-formed `/` and `-` prefix are written down in the kitchen with the two `[Skip]` pins; that is a stated pilot exclusion, not a silent weakening. `## Results` records build, test (2238 / 2230 / 8 skipped, standalone 48 files), and audit, plus the executable-bit hook fix. The restatement matches the task. No product defect.

## Issues
