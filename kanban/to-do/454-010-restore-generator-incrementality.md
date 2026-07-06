# Restore Generator Incrementality

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M4, M5).

## Description

Two issues substantially defeat incremental-generator caching, so the heavy emit stage
re-runs on every keystroke in the IDE:

- **M4**: `source/timewarp-nuru-analyzers/generators/nuru-generator.cs:121` —
  `RegisterSourceOutput(...Combine(context.CompilationProvider), ...)` feeds the raw
  `Compilation` (changes on every edit) into the output stage, forcing
  `InterceptorEmitter.Emit` to re-run regardless of model changes. The Compilation is
  only used for REPL enum resolution — narrow the input to just what that needs (e.g. a
  dedicated provider that extracts the enum info into an equatable model).
- **M5**: Pipeline model records hold `ImmutableArray<T>` fields with no equatable
  wrapper (`models/generator-model.cs:13-19`, `app-model.cs`, `route-definition.cs`,
  etc.). Record equality uses ImmutableArray's reference equality; every transform
  produces fresh arrays, so `.Collect()/.Combine().Select()` stages rarely compare equal
  and recompute even when content is unchanged. Standard fix: an `EquatableArray<T>`
  wrapper (sequence equality + cached hash).

## Requirements

- Introduce EquatableArray<T> (or equivalent) and use it in all pipeline model records.
- Remove/narrow the CompilationProvider combine at nuru-generator.cs:121.
- Ensure no ISymbol/SyntaxNode ends up captured in pipeline models while refactoring.

## Checklist

- [ ] EquatableArray<T> added and applied to all models
- [ ] CompilationProvider dependency narrowed
- [ ] Verify cacheability (e.g. incremental generator cachability tests / step-reason asserts)
- [ ] `ganda runfile cache --clear` + run CI tests
