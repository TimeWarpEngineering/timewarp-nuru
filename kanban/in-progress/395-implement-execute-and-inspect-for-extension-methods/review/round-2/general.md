# Round 2 — general
**Date:** 2026-08-27
**Scope reviewed:** fix delta `b034c788`; re-verify M1/M2 on HEAD vs `origin/master`

## Summary

The command-handler `global::` fallback now skips `IsInternalType`, so a lowered internal impl is not re-selected after the exact-match filter. `FindLibSibling` ranks `lib/` TFMs by closeness to the compile stub (then compilation TFM) and walks nearest-first until a method has a real body, which prefers `net6.0` over `netstandard2.0` for a `net8.0` stub. No new defects on this delta.

## Prior findings

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs:429
- Description: `ResolveServiceForCommand` fallback includes `!s.IsInternalType`. With only the internal impl in the model, lookup returns null and emit is `default! /* ERROR … */`, not `new Internal` or a missing `__svc_*` field. generator-42 `Should_not_emit_new_or_field_for_internal_impl_on_command_handler` maps a Command with `Handler(IEm42HiddenCmd)` (the `ResolveServiceForCommand` path), expects NURU054, and asserts no `new` / `__svc_Em42HiddenCmdImpl`.
- Suggestion: (done)
- Status: fixed

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/extractors/referenced-method-decompiler.cs:174
- Description: Same-TFM `lib/` is used only when it has a real body. Otherwise candidates are ordered by `RankLibCandidate` against the stub TFM (parseable) or compilation TFM. `FrameworkRank` puts `net*` above `netcoreapp` above `netstandard`, so `lib/net6.0` ranks ahead of `lib/netstandard2.0` for a `ref/net8.0` stub. Unparseable folders sort last (`int.MaxValue`). Fail-closed if no candidate has a real body.
- Suggestion: (done)
- Status: fixed

## Issues
