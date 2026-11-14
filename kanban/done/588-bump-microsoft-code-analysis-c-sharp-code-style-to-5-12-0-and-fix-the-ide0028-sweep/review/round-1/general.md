# Round 1 — general
**Date:** 2026-11-15
**Scope reviewed:** branch `task/588-bump-microsoft-code-analysis-c-sharp-code-style-to` vs `origin/master` (HEAD `12b05e82`), product diff and kitchen `## Results`

## Summary

The branch pins `Microsoft.CodeAnalysis.CSharp.CodeStyle` at 5.12.0 and rewrites the IDE0028 sites to C# 15 collection-expression arguments (`[with(...)]`). Comparer, capacity, and copy-constructor arguments are the same arguments the previous `new(...)` calls passed, including the dictionary copies in the key-binding builder and custom profile. The diff adds no `#pragma`, `NoWarn`, or `.editorconfig` severity change, and the goal restatement matches the task. Recorded build, test, audit, and `ganda nuget outdated` results cover the change; `git grep` of `\[with\(` under `source`, `tools`, and `tests` is 61 lines, and `origin/master` has 0.

## Issues

None.
