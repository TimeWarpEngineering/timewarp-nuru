# Round 1 — merged findings
**Date:** 2026-11-15
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 3 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:48
- Description: The concept map says several arguments are `[Parameter(Order = 0)]`, `[Parameter(Order = 1)]`, and that `Order` is required by analyzer `NURU_A002`. The same row is in the skill (`skills/tw-nuru-migrate-system-commandline/SKILL.md:94`). `ExtractParameterFromAttribute` reads `Description` and `IsCatchAll` only (`source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.properties.cs:207-218`). Position is the `GetMembers()` walk. `NURU_A002` exists as `MultipleParametersRequireOrder` in `diagnostic-descriptors.endpoints.cs` and is never reported. An agent who sets `Order` opposite declaration order, trusting the diagnostic text, binds arguments in declaration order. An agent who hunts a missing `NURU_A002` is chasing a diagnostic this package does not emit.
- Suggestion: Say that positional order is property declaration order, and that `Order` is not read and `NURU_A002` is not reported on 4.0.0-beta.14. Keep `Order = n` out of the recipe, or mark it as unused. File the dead diagnostic and the unread property on task 591 if they are still considered product defects.
- Source: general
- Disposition notes: Fixed. Guide concept map, worked samples, and checklist drop `Order`; position is declaration order, `Order` unread and `NURU_A002` unreported on beta.14. Skill mapping row and Step 5 likewise. Filed as task 591 item 11.

### M2 — Severity: bug — Status: fixed
- File: skills/tw-nuru-migrate-system-commandline/SKILL.md:173
- Description: Step 4 maps `return 2;` to `System.Environment.ExitCode = 2; return Unit.Value;`. The Step 3 template is a non-async `ValueTask<Unit> Handle`, and the Step 3 rules say a synchronous body returns `Unit.ValueTask` while only an `async` body returns `Unit.Value`. `return Unit.Value` does not compile in the template. The docs page samples use `Unit.ValueTask`. The required-option bullet two lines later in Step 4 already returns `Unit.ValueTask`.
- Suggestion: Make Step 4 use `return Unit.ValueTask;` for the synchronous handler, and mention `return Unit.Value;` only for an `async` method.
- Source: general
- Disposition notes: Fixed. Skill Step 4 returns `Unit.ValueTask` in the synchronous template; `Unit.Value` only in an `async` `Handle`.

### M3 — Severity: bug — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:244
- Description: The `=` / alias normalizer walks every argument. The sentence after the sample says to stop at `--` when the tool forwards arguments (`migrating-from-system-commandline.md:263`). The skill tells the agent to stop at `--` and points at this sample (`SKILL.md:192-194`). The sample never checks for `--`. A declared name after the separator is rewritten (`--verbosity=quiet` becomes `--verbosity` and `quiet`) when that name is in the map. That is the passthrough case this task is for.
- Suggestion: Break the loop at `--`, copy `--` and the tail unchanged, and show that in the sample the skill links to.
- Source: general
- Disposition notes: Fixed. Normalizer sample now stops at `--` and copies `--` plus the tail unchanged; skill says the same.

### M4 — Severity: suggestion — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:55
- Description: Repeated options are documented as `IsRepeated = true` (concept map, the option-syntax table at line 217, the churn table at line 705, and `SKILL.md:103`). `ExtractOptionFromAttribute` never reads `IsRepeated`. `isRepeated` is `IsRepeatedOptionType` (array or collection). A `string[]` repeats with the flag omitted. `IsRepeated = true` on a `string` does nothing. The samples that use `string[]` still work.
- Suggestion: Say the property type is what repeats, and that the `IsRepeated` flag is not read on this beta. Leave the `string[]` examples.
- Source: general
- Disposition notes: Fixed. Guide (concept map, option-syntax table, churn table) and skill say the array/collection type repeats and `IsRepeated` is unread. Filed as task 591 item 12.

### M5 — Severity: suggestion — Status: fixed
- File: skills/tw-nuru-migrate-system-commandline/SKILL.md:164
- Description: The skill says `Version` is not a built-in type, grouped with `FileInfo` and `DirectoryInfo`. `GetTypeConstraintFromClrType` maps `System.Version` to `version` (`endpoint-extractor.properties.cs:408`) and does not map `FileInfo`, `DirectoryInfo`, or `IPAddress`. Fluent `{file:FileInfo}` still converts, because that path uses `TypeConversionMap`. `glossary.md` defines "built-in type" as that map, including `FileInfo`, `DirectoryInfo`, and `Version`. The guide's "not built-in parameter types" sentence (`migrating-from-system-commandline.md:164`) is right for an endpoint `FileInfo` property and uses the glossary term for a narrower set.
- Suggestion: Tell agents that an endpoint `Version` property binds, and that `FileInfo` / `DirectoryInfo` endpoint properties do not (bind `string`, or use a fluent constraint). Add one sentence to `glossary.md` so "built-in type" (route constraint / `TypeConversionMap`) is distinct from the endpoint-property subset.
- Source: general
- Disposition notes: Fixed. Skill and guide list the endpoint-property type set (incl. `Version`; not `FileInfo`/`DirectoryInfo`/`IPAddress`); glossary `built-in type` row notes the narrower endpoint subset. Filed as task 591 item 13.

### M6 — Severity: nit — Status: fixed
- File: documentation/user/guides/migrating-from-system-commandline.md:740
- Description: Behavior-difference item 9 ends with a stray `**` after "directives". That opens emphasis through "An enum cannot hold a hyphenated value..." and consumes the opening `**` of item 10, so the `--version` heading is not bold.
- Suggestion: Drop the extra `**`. Split the enum sentence out of the `-h` item if it stays.
- Source: general
- Disposition notes: Fixed. Stray `**` removed; enum sentence split into its own paragraph under item 9.

## Duplicates / conflicts

- Single reviewer. No overlaps to collapse.
