# Round 1 — general
**Date:** 2026-09-22
**Scope reviewed:** branch `task/470-005-use-getsymbolinfo-for-map-implements-addbehavior-t` vs `origin/master` (commits `7a013a68`, `6a4f8e4a`, `b6ea596f`). Product files: `type-syntax-resolver.cs`, `dsl-interpreter.cs` (`Map<T>`, `AddBehavior(typeof)`, `AddTypeConverter`), `implements-extractor.cs`, `service-extractor.cs` syntactic `AddSingleton` / `AddHttpClient` type arguments, `generator-44-referenced-type-arguments.cs`, CI standalone include and `CiTestExcludes`.

## Summary

Type-argument extraction now goes through `TypeSyntaxResolver`: `GetSymbolInfo`, then `GetTypeInfo`, rejecting `TypeKind.Error`. Call sites are `Map<T>`, `Implements<T>` (including the property-type fallback), `AddBehavior(typeof)`, the syntactic `AddSingleton` / `AddHttpClient` generic-argument paths, and `AddTypeConverter`. Risk is low: the helper matches the previous AddTypeConverter order, bound service registrations still use method type arguments, and `ExtractTypeOfArgumentWithSymbol` / `DiscoverEndpoints` are unchanged. `generator-44` and `generator-36` each passed 2/2. On Roslyn 5.6, ordinary metadata references return the same symbol from both APIs, so the happy-path assertions would also pass on `GetTypeInfo` alone; the unresolvable `Map<T>` test does lock the new error-type rejection.

## Issues

None.
