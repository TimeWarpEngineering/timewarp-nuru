# Round 1 — general
**Date:** 2026-10-05
**Scope reviewed:** branch task/482-005-emit-try-parse-for-datetimeoffset-version-and-type vs origin/master (route-matcher-emitter, type-conversion-map, routing-16-typed-catch-all)

## Summary

`GetBuiltInTryConversion` now maps `datetimeoffset` and `version`, and scalar emit declares the handler local before that TryParse (`DateTimeOffset @when`, `Version v`). Typed catch-all (`string[]`, `Length`) and repeated options (`List<string>`, `Count`) both use `EmitBuiltInArrayConversion`, which loops the same map as scalars, so `uint` and `TimeSpan` convert and a failed TryParse — including `int` overflow — writes the invalid-value line and returns 1 instead of throwing. `Version.TryParse` outs `Version?` into a non-nullable local under `#nullable enable`, but `NuruGenerated.g.cs` opens with `#pragma warning disable`, and that pattern compiles with warnings treated as errors. The new routing-16 tests cover the checklist cases. No issues.

## Issues

None.
