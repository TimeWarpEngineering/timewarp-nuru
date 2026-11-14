# Round 1 — general
**Date:** 2026-11-14
**Scope reviewed:** branch `task/586-route-parameter-with-the-built-in-string-type` vs merge-base `2c66bb2c` (master). `EmitTypeConversions` string branch, `TypeConversionMap.IsStringConstraint`, option `needsConversion`, catch-all capture names, `routing-38-string-type-constraint.cs`.

## Summary

`{message:string}` now declares the handler local from the positional capture (`string message = __message_N`, or `string?` when the parameter is optional) and skips conversion. `PositionalCaptureVar` already stores a typed parameter in `__name_N` as `string` or `string?` according to `IsOptional`, and the new assignment uses that same flag, so the types line up. The alias matcher calls `EmitTypeConversions` too. A string catch-all still skips conversion because the capture is already `string[]`; `IsStringConstraint` also treats `string?` as that capture, which is the name the invoker and the JSON slot model use. A `{value:string}` option takes the untyped option path, so a property default stays the raw initializer and the value stays `string?`, matching an untyped string option. `routing-38` pins required and optional positionals (given and omitted), `String` casing, a string beside an `int`, and required and optional options. `## Results` records `./bin/dev build`, `./bin/dev test` (2449 passed, the new file's 8 cases), and `ganda repo audit`, plus a revert check that the positional case is CS0103 without the branch. The restatement matches the task. The casing bullet is the same case-insensitive built-in match `TypeConversionMap` already uses. No product defect.

## Issues
