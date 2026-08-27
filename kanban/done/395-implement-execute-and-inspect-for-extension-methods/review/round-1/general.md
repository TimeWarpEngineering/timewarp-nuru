# Round 1 — general
**Date:** 2026-08-27
**Scope reviewed:** PR #227 — `task/395-implement-execute-and-inspect-for-extension-method` vs `origin/master` (decompile-and-lower, not execute-and-inspect)

## Summary

The change follows in-project `AddX` via syntax and decompiles referenced methods from the NuGet `lib/` implementation (not `ref/` stubs), then fail-closed-lowers pure public closed `Add*` / `TryAdd*` scripts into the existing Phase 3 `ServiceDefinition` model. That matches the task: no compile-time `Invoke`, no `IServiceCollection` replay, hatch unchanged, NURU052 on the whole user-facing call when purity/decompile/builder/factory fail. Dominant risk is emit of types the generator can *see* after lowering but must not `new` (internal impls), plus `lib/` TFM selection when the compile stub’s TFM has no sibling.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs:428
- Description: The new `!s.IsInternalType` filter on `ResolveServiceForCommand` is undone by the `global::` fallback. After lowering, a library `AddX` that registers `AddSingleton<IFoo, InternalImpl>` is a `ServiceDefinition` with `IsInternalType: true` (previously the call was opaque and never entered the model). The first lookup skips it; the fallback matches the same descriptor by stripping `global::` and then emits `new {ImplementationTypeName}` (transient) or a `__svc_*` field that `InterceptorEmitter` did not emit (singleton/scoped). Delegate handlers go through `ServiceResolverEmitter.FindService`, which skips internals on both loops, so generator-42’s NURU054 case (`WithHandler(() => "ok")`, no injection of `IEm42Hidden`) does not hit this path.
- Suggestion: Apply `!s.IsInternalType` on the fallback `FirstOrDefault` (same as `ServiceResolverEmitter.FindService`). Extend generator-42 so a Command handler (or any path that calls `ResolveServiceForCommand`) injects the public interface registered to the internal impl, and assert the generated source still has no `new` of that impl and no `__svc_` field for it.
- Status: open

### Issue 2 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/extractors/referenced-method-decompiler.cs:163
- Description: When `ref/{tfm}/Foo.dll` has no `lib/{tfm}/` sibling, `FindLibSibling` picks `Directory.GetFiles(lib, …, AllDirectories)` ordered by full path ordinal-ignore-case descending. That prefers `lib/netstandard2.0/` over `lib/net6.0/` (`'s' > '6'`). NuGet’s compile/runtime asset graph prefers the nearest compatible TFM, so the decompiled body can be a different implementation than the one the app will load (or a purity fail on a body the runtime would not use). Same-TFM hits are fine; this is only the fallback.
- Suggestion: Prefer the `lib/` TFM closest to the compile stub’s TFM (or to `compilation`’s target framework), not lexicographic path order. Keep fail-closed if nothing in `lib/` has a real body.
- Status: open
