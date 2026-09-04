# Use GetSymbolInfo for Map Implements AddBehavior type args

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M7).

## Description

`ExtractGenericTypeArgument` (`dsl-interpreter.cs:764`) used by `Map<TEndpoint>()` resolves the type argument with only `SemanticModel.GetTypeInfo(typeSyntax).Type`. Repo convention (and the 454-012 AddTypeConverter fix at `dsl-interpreter.cs:1483-1491`) is `GetSymbolInfo` first because `GetTypeInfo().Type` may be null for types from referenced projects.

When that happens, `DispatchMapEndpoint` throws; fail-soft converts it to a diagnostic and the endpoint is dropped — silent miss for multi-project apps.

Same pattern without GetSymbolInfo fallback:
- `dsl-interpreter.cs:1289-1290` (`AddBehavior(typeof(...))`)
- `implements-extractor.cs:62-65` (`Implements<T>()`)
- `service-extractor.cs:541-555` and `:1027-1036` (`AddSingleton` / `AddHttpClient` type args)

## Requirements

- Mirror AddTypeConverter: try GetSymbolInfo, then GetTypeInfo, reject TypeKind.Error.
- Apply to Map<T>, Implements<T>, AddBehavior typeof, and service/HttpClient generic type-argument extraction.
- Generator-hosted regression if a referenced-project fixture is practical.

## Checklist

- [ ] GetSymbolInfo fallback at all cited sites
- [ ] Tests
- [ ] `ganda runfile cache --clear` + CI tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M7.
