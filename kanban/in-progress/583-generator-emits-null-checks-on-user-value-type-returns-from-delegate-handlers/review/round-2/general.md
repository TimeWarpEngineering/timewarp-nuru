# Round 2 — general
**Date:** 2026-11-14
**Scope reviewed:** Delta since round 1 base `7a3c4f6a` (`bec98676`, kitchen disposition only). M1 re-checked against `origin/feature/generator-fixes-2026-11-14` (`75b4475f`) ... `HEAD`. Read against `task.md` ## Goal (implementer restatement) and the original description. Recorded gates in ## Results were taken as the build, test, and audit record. No full build, test, or audit was run.

## Summary

`bec98676` does not change product code. It records M1 as wontfix: this branch stacks on `feature/generator-fixes-2026-11-14`, so `master` was the wrong review base. That claim holds. `git diff --name-only origin/feature/generator-fixes-2026-11-14...HEAD` lists the value-kind model, extractors, emitters, generator-75, routing-39, and this task's kanban files. It does not list task 586's files (`type-conversion-map.cs`, `routing-38-string-type-constraint.cs`, or the other string-constraint emitters). Merge-base with `origin/feature/generator-fixes-2026-11-14` is `75b4475f`.

The value-kind diff matches the restatement. `ResultValueKinds.Of` marks `Nullable<T>` as `MaybeNull` and a non-nullable enum as `Enum`. `EmitResultOutput` skips `is not null` and `?.ToString()` when the kind is not `MaybeNull`. `GetOutputStrategy` sends a non-nullable enum to raw output, and capabilities map that to `Primitive`. A user struct stays on the JSON strategy. generator-75 compiles the enum and struct delegate, the async enum delegate, and the enum method group. routing-39 runs those plus `IQuery<VtrShade>` and a null `VtrShade?`. ## Results records build, test, and audit as pass, including the fix-pass rerun. The implementer restatement matches the original task. No misread.

## Issues

<!-- No new issues. M1 stays wontfix; see merged.md. -->
