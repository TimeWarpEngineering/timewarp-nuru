# Round 1 — general
**Date:** 2026-11-16
**Scope reviewed:** branch `task/594-update-outdated-package-pins-amuru-beta-5` vs merge-base `cbe25e38`. Product diff is `Directory.Packages.props` and `tests/timewarp-nuru-tests/routing/routing-41-argv-fuzz-property.cs`.

## Summary

The six central pins match the task table. `Directory.Packages.props` is the only version source for these packages; `#:package` directives name TimeWarp.Amuru and CsCheck with no version, so they follow the central pins. The only call-site edit renames `Gen.OneOfConst` to `Gen.OneOf` on constant arrays and value tuples. CsCheck 5.0.1 keeps `OneOf<T>(IGen<T>[])` and `Frequency<T>((int, IGen<T>)[])` at `OverloadResolutionPriority(1)`, so the unchanged generator `Gen.OneOf` and `Gen.Frequency` calls still bind to the generator overloads. No `OneOfConst`, `FrequencyConst`, or other CsCheck 5 removals remain in `*.cs`. The goal restatement matches the original task. Recorded `## Results` cover `ganda nuget outdated`, `./bin/dev build`, `./bin/dev test` (multi-mode and standalone), and `ganda repo audit`. This review did not re-run those gates.

## Issues

<!-- None. -->
