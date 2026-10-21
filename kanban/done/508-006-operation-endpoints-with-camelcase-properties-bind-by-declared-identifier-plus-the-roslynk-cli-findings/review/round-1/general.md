# Round 1 — general
**Date:** 2026-10-19
**Scope reviewed:** branch `task/508-006-operation-endpoints-with-camelcase-properties-bind` vs `master` (commits `d7428936`, `b47130b2`)

## Summary

`CommandConstructionEmitter` writes each request member with `EscapeIfKeyword(ParameterName)` so a camelCase property such as `solutionId` and a keyword property such as `@class` compile. Argv, `--json-args`, and `TryBind` all construct the request through that emitter. `GenerateNuruJsonContextTask` registers a problem pair's success type (nothing when a renderer is present) and `JsonContextTypeNames` gives every STJ name collision, including types reached through members, a `TypeInfoPropertyName` from the full name. Nested `--json-args` members match ignoring case. Finding 2 is documented. Findings 3, 4, and 6 are tasks 508-011, 508-012, and 508-013 on master.

The new runfile reports Passed: 6. Generated `NuruGenerated.g.cs` assigns `solutionId = solutionid` and `@class = @class`. The source-generated context resolves both `Response` types, both `Item` types, and both `List<Item>` types under distinct property names, and `NuruJsonMemberContext` sets `PropertyNameCaseInsensitive = true`. `command-class-emitter.cs` and the delegate branch of `behavior-emitter.cs` still Pascal-case members of the generated `__NuruCommand_N` class; that class is not an `[Operation]` request.

## Issues

<!-- None. -->
