# Round 1 — general
**Date:** 2026-11-17
**Scope reviewed:** branch `task/596-consumer-check-blames-a-shouldly-assertion-in-a` vs merge-base `26c48c11` (`master...HEAD`). Product diff is `skills/tw-nuru-verify/scripts/nuru-consumer-check.sh` and `documentation/user/guides/migrating-from-system-commandline.md`. Compared with `## Goal (implementer restatement)` and the original task. No misread.

## Summary

`nuru_errors` now drops `CS1061` when the quoted receiver is one of the listed C# keyword types, optionally followed by `?` and `[]`, and still blames `CS1061` / `CS0117` on any other receiver except the existing host extension points. The migration guide's `## Tests` section names Shouldly and the snippet starts with `using Shouldly;`. That matches the task. The recorded gates cover build, test, and audit, and `/tmp/nv596` matches the recorded docs run (0 doc defects, `migrating-from-system-commandline.md#L626` is `fragment`). The verify skill still describes the old missing-member rule.

The live log for that snippet does not contain `CS1061`. Its errors are `NURU_S999: Map<T>() requires a type argument` (unresolved `DeployCommand`, same diagnostic the generator emits for a missing map type argument) and `CS0246` on `Shouldly`. `CS0246` sets the undefined-name flag, so the generator error is treated as a cascade and the snippet is a fragment. The new filter is what the task requires for a `bool` receiver; this run does not exercise it. The regex itself matches the acceptance cases: a closing `'` after the optional `?` / `[]` run, so `boolean` stays blamed, and `string[]`, `int?`, `int[]?`, and `string?[]` are dropped. `object` and `dynamic` stay blamed, and Results says why.

## Issues

### Issue 1 — Severity: suggestion
- File: skills/tw-nuru-verify/SKILL.md:105
- Description: The skill still says `CS1061` and `CS0117` are broken unless the receiver is a host extension point (`IServiceCollection`, `ILoggingBuilder`). The script now also drops `CS1061` whose receiver is a built-in keyword type (`bool`, `byte`, `sbyte`, `char`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `nint`, `nuint`, `float`, `double`, `decimal`, `string`, optionally `?` or `[]`). `CS0117` on those types is unchanged. `object` and `dynamic` stay blamed on purpose. Someone following the skill will treat a Shouldly-style `CS1061` on `bool` as a doc defect, which is the false blame this task removes. The script comment at `nuru-consumer-check.sh:320` already states the new rule. The skill is the other copy of that contract, and this diff does not update it.
- Suggestion: Update the `broken` bullet so it matches `nuru_errors`. Host extension points still cover `CS1061` and `CS0117`. Built-in keyword receivers cover `CS1061` only. Say that `object` and `dynamic` stay blamed.
- Status: open
