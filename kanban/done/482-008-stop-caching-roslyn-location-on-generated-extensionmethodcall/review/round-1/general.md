# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** commit a9fd7822 — `ExtensionMethodCall` stores `LocationInfo` instead of a Roslyn `Location`

## Summary

`ExtensionMethodCall` is a field of `AppModel`, which is the `GeneratorModel` the `NuruGeneratorModel` step compares before emit. Both construction sites in `service-extractor.cs` (special-cased `AddLogging` / `AddHttpClient`, and an `AddX` the lowerer refuses) now store `LocationInfo.CreateFrom`. `ModelValidator` passes the `Compilation` into `ValidateExtensionMethods`, and `LocationInfo.ToLocation` binds the diagnostic to the current syntax tree, so NURU052 stays in source. Re-ran generator-37 (3/3) and generator-42 (8/8). No defect in the diff or the surrounding call sites.

## Issues

None.
