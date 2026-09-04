# Round 1 — analyzers-generators
**Date:** 2026-09-04
**Scope reviewed:** `source/timewarp-nuru-analyzers/` (interpreter, extractors, emitters, validators, locators, models, ir-builders, diagnostics, `nuru-generator.cs`); regression tests under `tests/timewarp-nuru-tests/generator/` and `tests/timewarp-nuru-tests/help/`. Pinned product tree SHA `648369f6` / origin-home `38480f57`, version `3.0.0-beta.77`.

## Summary

454 analyzer/generator remediations checked in this tree are **not regressing**: help brace/string escaping is centralized, interpreter cycle guard and fail-soft catches are present, H002 named-argument false positives are filtered, incrementality is split into Stage A/B with `EquatableArray` + precomputed `EnumInfo`, string heuristics for collections/loggers were replaced with symbol checks (with a documented syntax-only fallback), and R003 required signatures include effectively-required flags. Post-454 work for keywords (460), unicode newlines (465), WithExample literals (464), extension-method lowering (395), and capabilities group/search filters is present and covered by tests.

New defects found: several type-resolution sites still use `GetTypeInfo().Type` alone (repo convention requires `GetSymbolInfo` fallback for referenced-project types); service constructor default-value emission under-escapes strings/enums relative to the endpoint path; source-gen DI still cannot resolve `IEnumerable<T>` multi-implementation constructor deps (epic 391 leftover, runtime-DI-only today).

## 454 regression check

| 454 ID | Still present? | Evidence |
|--------|----------------|----------|
| 454-002 H2 help braces | **No** | Shared `EmitterStringUtils.EscapeForStringLiteral` used by help/route-help/capabilities (`source/timewarp-nuru-analyzers/generators/emitters/emitter-string-utils.cs:18-27`, `help-emitter.cs:60,115`). Descriptions emit plain `"..."` literals, not generated `$"..."` holes. Regression: `tests/timewarp-nuru-tests/help/help-07-description-special-chars.cs`. |
| 454-003 H3 StackOverflow cycle | **No** | Null sentinel pre-cached before initializer eval in `ResolveIdentifier` (`dsl-interpreter.cs:453-463`). Regression: `generator-28-interpreter-cycle-guard.cs`. |
| 454-004 H4 NURU_H002 named args | **No** | `DetectClosures` / anonymous path skip `NameColonSyntax` and `NameEqualsSyntax` (`handler-validator.cs:290-300`, `413-419`). Regressions: `generator-29`, `generator-38`. |
| 454-010 M4+M5 incrementality | **No** | Stage A reports diagnostics; Stage B emits from equatable `GeneratorModel` + `EquatableArray<EnumInfo>` without Compilation (`nuru-generator.cs:118-174`). Intercept sites store precomputed attribute syntax (`intercept-site-model.cs:8-44`). Locations use equatable `LocationInfo` (`location-info.cs:5-37`). Regression: `generator-37-incrementality-caching.cs`. |
| 454-011 M6/M7/M9 | **No** | Fail-soft `catch` on interpret paths (`dsl-interpreter.cs:67-74`, `95-102`); `TryDoneRoute` catches `HandlerParameterMismatchException` and continues (`1684-1697`); `IsDslBuilderMethod` gates fluent dispatch (`655-676`). Regressions: `generator-31/32/33`. |
| 454-012 M8 string heuristics | **No** | Symbol-based `IsServiceType(ITypeSymbol)` / `IsCollectionInterface` / `IsLoggerOrServiceProvider` (`handler-extractor.cs:636-711`); endpoint `IsRepeatedOptionType` uses `OriginalDefinition` (`endpoint-extractor.cs:1366-1408`); `AddTypeConverter` uses `GetSymbolInfo` then `GetTypeInfo` (`dsl-interpreter.cs:1483-1519`). String `IsServiceType` kept only as documented partial-code fallback (`721-719`). Regressions: `generator-34/35/36`. |
| 454-013 M10 false NURU_R003 | **No** | `ComputeRequiredSignature` includes effectively-required unbound flags (`overlap-validator.cs:312-320`). Regression: `generator-30-nuru-r003-overlap.cs`. |
| 454-028 LOW sweep | **No** | Shared escape helper; method-group `IsStatic` (`handler-validator.cs:181,220`); non-generic `ValueTask` recognized (`handler-extractor.cs:500-503`); multi-word group alias math (`endpoint-extractor.cs:155-176`); no `NURU_DEBUG*` leftovers under analyzers. Dead syntax-only `ExtractPropertyDefaultValue` still exists with TODO but is unused (`endpoint-extractor.cs:998-1010`) — not a live defect. |

## Issues

### Issue 1 — Severity: bug
- File: `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:764`
- Description: `ExtractGenericTypeArgument` (used by `Map<TEndpoint>()`) resolves the type argument with only `SemanticModel.GetTypeInfo(typeSyntax).Type`. Repo convention (`.agent/local/nuru-specific.md` Roslyn Best Practices; also the 454-012 AddTypeConverter fix at `dsl-interpreter.cs:1483-1491`) states `GetTypeInfo().Type` may be null for types from referenced projects while `GetSymbolInfo().Symbol` still resolves. When that happens, `DispatchMapEndpoint` throws (`739-742`), fail-soft converts it to a diagnostic, and the endpoint is dropped — silent functional miss for multi-project apps. Same pattern without `GetSymbolInfo` fallback at: `dsl-interpreter.cs:1289-1290` (`AddBehavior(typeof(...))`), `implements-extractor.cs:62-65` (`Implements<T>()`), `service-extractor.cs:541-555` and `1027-1036` (`AddSingleton`/`AddHttpClient` type args).
- Suggestion: Mirror the AddTypeConverter pattern: try `GetSymbolInfo` first, then `GetTypeInfo().Type`, reject `TypeKind.Error`, and only fail when both miss. Apply consistently to Map\<T\>, Implements\<T\>, AddBehavior typeof, and service/HttpClient generic type-argument extraction.
- Status: open

### Issue 2 — Severity: bug
- File: `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs:409-419`
- Description: `GetDefaultValueExpression` emits constructor optional defaults into generated DI `new T(...)` calls. Strings are wrapped as `$"\"{s}\""` with no escaping of `"`, `\`, or newlines — a default like `"a\"b"` or `"C:\tools"` produces non-compiling or wrong literals in the interceptor. The `_ => defaultValue.ToString()` arm is used for enums and other non-primitive constants, emitting an unqualified member name (e.g. `Prod`) that will not resolve in the generated file's namespace. Endpoint property defaults already use `SymbolDisplay.FormatLiteral` correctly (`endpoint-extractor.cs:977-980`); this service path was not brought to parity (454-028/012 covered property defaults, not service ctor defaults).
- Suggestion: Use `SymbolDisplay.FormatPrimitive` / `FormatLiteral` for strings and chars; for enum defaults emit `type.ToDisplayString(FullyQualifiedFormat) + "." + memberName` (or equivalent from `IParameterSymbol`). Add a generator-hosted regression with a string default containing quotes/backslashes and an enum optional parameter under source-gen DI.
- Status: open

### Issue 3 — Severity: suggestion
- File: `source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs:349-377`
- Description: Source-gen DI resolves a constructor dependency via `FindService(param.TypeName, ...)`, which returns at most one registration and has no special case for `IEnumerable<T>` / `IReadOnlyList<T>` aggregating all implementations. An `IEnumerable<IHandler>` dependency therefore falls through to `default! /* ERROR: Cannot resolve ... */` (or NURU050/051), while the same graph works under `UseMicrosoftDependencyInjection()` (documented TODO and all tests in `tests/timewarp-nuru-tests/generator/generator-27-ienumerable-dependency.cs` use runtime DI only). This is an epic 391 leftover after Phase 3 constructor graphs landed.
- Suggestion: Either emit `new T[] { impl1, impl2, ... }` / empty array for `IEnumerable<T>` constructor parameters in source-gen DI, or add an explicit diagnostic directing users to runtime DI when multi-impl collection deps are detected. Track under epic 391 rather than treating as fixed by generator-26.
- Status: open

### Issue 4 — Severity: suggestion
- File: `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:771-778`
- Description: Emitted `FileInfo` / `DirectoryInfo` conversions wrap `new FileInfo(...)` / `new DirectoryInfo(...)` in `catch (ArgumentException)` only (also `1359`, `1378`, `1400`, `1419`). Those constructors can also throw `PathTooLongException` and `NotSupportedException`, which escape the conversion block and become unhandled route failures instead of the intended "Invalid value" exit path. Built-in TryParse/Uri.TryCreate paths already fail soft.
- Suggestion: Catch `System.Exception` (or the documented constructor exception set) around these constructions, matching the soft-fail pattern used for TryParse conversions.
- Status: open
