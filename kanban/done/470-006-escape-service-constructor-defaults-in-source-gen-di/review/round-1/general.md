# Round 1 — general
**Date:** 2026-09-22
**Scope reviewed:** branch `task/470-006-escape-service-constructor-defaults-in-source-gen` vs `origin/master` (commits `d7d30104`, `9ec8c169`). Product files: `service-extractor.cs`, `route-matcher-emitter.cs`, `generator-45-constructor-default-literals.cs`, CI standalone include and `CiTestExcludes`, comment in `routing-23-uri-fileinfo-directoryinfo.cs`.

## Summary

Constructor optional defaults are emitted as compiling expressions. Strings and chars go through `SymbolDisplay.FormatLiteral`. Other primitives go through `SymbolDisplay.FormatPrimitive`, then `F`/`M`/`U`/`L`/`UL` where that API omits the suffix, with non-finite float and double values named before the suffix step. Enum defaults are fully-qualified members; a constant that is not one named member is a cast of the underlying literal. On Roslyn 5.6, `FormatPrimitive` does not already include those suffixes, enum `ExplicitDefaultValue` is the underlying integral, and `FormatLiteral` escapes U+2028, U+2029, and U+0085. All eight FileInfo/DirectoryInfo emit sites (required and optional, parameter and option) catch `Exception`. `GetBuiltInTryConversion` returns null for those constraints, so the special-case path is the one that emits, and the obsolete `GetBuiltInConversion` has no callers. `generator-45` passed 2/2.

## Issues

None.
