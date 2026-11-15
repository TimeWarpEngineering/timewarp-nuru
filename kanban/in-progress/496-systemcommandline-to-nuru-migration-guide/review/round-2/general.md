# Round 2 — general
**Date:** 2026-11-15
**Scope reviewed:** `be66b07a..95dc80b8` — the round-1 fix on `documentation/user/guides/migrating-from-system-commandline.md`, `skills/tw-nuru-migrate-system-commandline/SKILL.md`, `glossary.md`, task 591 items 11–13, and the kitchen Results record. Not the whole task diff.

## Summary

The fix commit drops `Order` / `NURU_A002` from the recipe, returns `Unit.ValueTask` from the synchronous exit-code template, stops the argv normalizer at `--`, treats repeat as the array or collection type, spells out the endpoint-property type set (with the glossary distinction), and removes the stray `**` in behavior difference 9. Those six claims match `ExtractParameterFromAttribute`, `ExtractOptionFromAttribute`, `GetTypeConstraintFromClrType`, and `IsRepeatedOptionType` in `endpoint-extractor.properties.cs`. The goal restatement still matches the original task. The fix-pass `## Results` record covers the markdown change with its own build, test, and audit lines. No new defects on this delta.

`skills/tw-nuru/SKILL.md` still says multiple `[Parameter]`s must set `Order` because `NURU_A002` enforces it. That file is outside this delta. The migration recipe, which this task owns, now says not to set `Order`.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:48
- Description: Concept map, worked samples, checklist, and the skill mapping row and Step 5 no longer tell the agent to set `Order`. They say position is property declaration order, `Order` is not read, and `NURU_A002` is not reported on 4.0.0-beta.14. `ExtractParameterFromAttribute` still reads only `Description` and `IsCatchAll`; position is the `GetMembers()` walk. Task 591 item 11 records the product gap.
- Status: fixed

### M2 — Severity: bug — Status: fixed
- File: skills/tw-nuru-migrate-system-commandline/SKILL.md:174
- Description: Step 4 maps `return 2;` to `return Unit.ValueTask;` in the synchronous Step 3 template, and mentions `return Unit.Value;` only for an `async` `Handle`. The required-option bullet uses `Unit.ValueTask` as well. The Step 3 rules agree.
- Status: fixed

### M3 — Severity: bug — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:249
- Description: The normalizer breaks when `arg == "--"` and copies `args[i..]` unchanged. The following sentence matches the sample, and it still says to split at `--` before `RunAsync` when the tail must keep `=` tokens. The skill Step 5 says the same.
- Status: fixed

### M4 — Severity: suggestion — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:55
- Description: Concept map, option-syntax table, churn table, and the skill say a `string[]` (or collection) repeats and that `IsRepeated` is not read on 4.0.0-beta.14. `ExtractOptionFromAttribute` never reads `IsRepeated`; `isRepeated` is `IsRepeatedOptionType`. Task 591 item 12 records the product gap.
- Status: fixed

### M5 — Severity: suggestion — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:164
- Description: Guide and skill list the endpoint-property set that `GetTypeConstraintFromClrType` plus enum binding actually covers, including `Version`, and they say `FileInfo`, `DirectoryInfo`, and `IPAddress` properties do not bind. Fluent `{file:FileInfo}` is attributed to the route-constraint path. `glossary.md` `built-in type` keeps the `TypeConversionMap` set and adds the narrower endpoint subset. Task 591 item 13 records the product gap.
- Status: fixed

### M6 — Severity: nit — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:750
- Description: Behavior difference 9 no longer ends with a stray `**`. The enum sentence is its own paragraph. Item 10's `--version` heading is bold again.
- Status: fixed

## Issues

<!-- No new issues. -->
