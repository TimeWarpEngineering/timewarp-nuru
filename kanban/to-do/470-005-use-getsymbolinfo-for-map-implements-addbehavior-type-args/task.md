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

- [x] GetSymbolInfo fallback at all cited sites
- [x] Tests
- [x] `ganda runfile cache --clear` + CI tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M7.

## Session

- Implementer: grok session 01a0c854-9d49-7e03-a6cf-e76ec33db687 (2026-09-22)

## Results

DSL type-argument extraction now follows the AddTypeConverter order: `GetSymbolInfo`, then `GetTypeInfo`, and `TypeKind.Error` is treated as unresolved. `Map<T>`, `Implements<T>`, `AddBehavior(typeof)`, and the syntactic `AddSingleton` / `AddHttpClient` generic-argument paths share `TypeSyntaxResolver`. `AddTypeConverter` calls that helper too, so the order cannot drift.

A referenced-assembly fixture (`generator-44`) emits `global::M7Symbols.M7Behavior`, `global::M7Symbols.IM7Filter`, `global::M7Symbols.M7Service`, and `__httpClient_IM7Client`. `AddSingleton` / `AddHttpClient` are left unbound so extraction uses the syntactic path. An unresolvable `Map<T>` argument yields `NURU_S999` and is not emitted.

### Files changed

- `source/timewarp-nuru-analyzers/generators/type-syntax-resolver.cs` (new)
- `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/implements-extractor.cs` (type argument and the property-type fallback that re-reads `Implements<T>`)
- `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs`
- `tests/timewarp-nuru-tests/generator/generator-44-referenced-type-arguments.cs`
- `tests/ci-tests/Directory.Build.props` and `tests/ci-tests/run-ci-tests.cs` (standalone phase; CS0433 excludes it from the multi-mode assembly)

### Key decisions

- One resolver for every cited site, including the existing AddTypeConverter path.
- `service-extractor` `typeof(...)` registrations were already outside the cited generic-argument lines and stay as they were.
- Referenced `[NuruRoute]` types are still discovered only from syntax in the compilation. The fixture proves `Map<T>` resolves the referenced type (no `requires a type argument` diagnostic) and that the other three sites emit fully qualified names.

### Test outcomes

- `generator-44-referenced-type-arguments.cs`: 2 passed
- `generator-36-m8-typeconverter-fqn.cs` (AddTypeConverter still on the shared resolver): 2 passed, inside the CI standalone phase
- `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs`: exit 0. Multi-mode 1643 passed, 7 skipped, 0 failed. Standalone phase passed, including generator-44.

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-470-005-use-getsymbolinfo-for-map-implements-addbehavior-t
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-44-referenced-type-arguments.cs
```

**Expect**

- 2 passed
- `Should_emit_fully_qualified_names_for_referenced_type_arguments`: generated source contains `global::M7Symbols.M7Behavior`, `global::M7Symbols.IM7Filter`, `global::M7Symbols.M7Service`, and `__httpClient_IM7Client`, and no `NURU_S999` whose message contains `requires a type argument`
- `Should_not_emit_unresolvable_map_type_argument`: one `NURU_S999` for `Map<M7Missing.DoesNotExist>()`, and generated source does not contain `DoesNotExist`

**Automated gate**

```bash
ganda runfile cache --clear
dotnet run tests/ci-tests/run-ci-tests.cs
# expect: exit 0; multi-mode 1643 passed, 7 skipped, 0 failed; standalone phase includes generator-44 (2 passed) and generator-36 (2 passed)
```
