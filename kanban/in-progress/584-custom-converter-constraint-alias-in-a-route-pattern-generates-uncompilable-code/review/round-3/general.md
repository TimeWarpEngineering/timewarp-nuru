# Round 3 — general
**Date:** 2026-11-14
**Scope reviewed:** Product delta since round-2 base `fc349ead` (`review/round-2/base.sha`): `280434b0` (list element conversion) and the kitchen record `c1755d50`. Checked against `task.md` ## Goal (implementer restatement), the round-2 fix-pass paragraph, and the original task. ## Results records the fix-pass build, test, and audit. No full build, test, or audit was run. No targeted command was run.

## Summary

`280434b0` closes M2. `IsKnownListElement` treats every `TypeConversionMap` built-in as a known list element, so `uri`, `FileInfo`, and `DirectoryInfo` no longer report `NURU_S010`. `BuiltInElementParse` converts those three per element on the catch-all, the repeated option, and the `--json-args` list path: `Uri.TryCreate` (RelativeOrAbsolute) and the `FileInfo` / `DirectoryInfo` constructor inside `catch (Exception)`, the same construction the scalar emitters use. generator-76 locks `{*files:FileInfo}`, `{*urls:uri}`, `--dir {dir:DirectoryInfo}*`, and `--link {link:Uri}*` (no `NURU_S010`, generated compilation has no errors). routing-16 binds the first three and checks the element values. `glossary.md` defines built-in type as that set. No new issues. The restatement still matches the original task. The generator-75 note in ## Learnings is unchanged.
