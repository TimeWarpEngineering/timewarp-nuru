# Round 2 — general
**Date:** 2026-10-18
**Scope reviewed:** fix delta d769f8b1..e316fd32 plus re-verification of the M-ids above

## Summary
Most round-1 claims hold at e316fd32: the dispatch selection (M2, M4), the typed HttpClient registration (M3), the wider scalar set (M5), the mediator-kind gate (M7), dedup (M9), the scan cache (M10), renderer precedence (M11), the required-JSON check placement (M13), and the per-dispatch missing-handler check (M18) are in the code as described. The fix delta adds new defects. Three inputs make the generator emit code that does not compile: a JSON member whose lowercased name is a C# keyword, a positional record or a `required ... { get; init; }` member, and a `double.NaN` or const-enum default (the last one breaks existing `[NuruRoute]` apps that compiled on 3.0). The `default!` sentinel also replaces a `= new()` initializer with null at runtime. M12 and M20 are only partly fixed.

## Prior findings
| ID | Status (fixed/open) | Evidence |
|----|---------------------|----------|
| M2 | fixed | handler-invoker-emitter.cs:301-302 chooses direct invoke when `NestedHandlerTypeName is not null && (useDirectInvoke \|\| !hasGeneratedMediator)`. generator-model.cs:101-113 uses the same rule. Every caller passes `hasGeneratedMediator`: route-matcher-emitter.cs:254,260,369,375, json-args-emitter.cs:1266,1282, interceptor-emitter.cs:1203. The changelog `### Changed` "Dispatch" line records the default. |
| M3 | fixed | interceptor-emitter.cs:50-57 collects source-gen typed clients. interceptor-emitter.cs:489-499 registers `AddTransient<TService>(services, static _ => new TImpl(__httpClient_X))` before `AddGeneratedMediator`. The static field comes from EmitHttpClientFields (interceptor-emitter.cs:953-1004). |
| M4 | fixed | generator-model.cs:88-95 returns true for any Command route whose `NestedHandlerTypeName` is null, so `__GetMediator()` is emitted even when every app calls `UseDirectInvoke()`. |
| M5 | fixed | endpoint-extractor.cs:1546-1552 maps sbyte, ushort, uint, ulong, DateOnly, TimeOnly, and BigInteger. type-conversion-map.cs:34-37,49-50,54 has a TryParse for each one. Repeated elements go through operation-endpoint-extractor.cs:727. The runtime converter is default-type-converters.cs:205-212,303. |
| M6 | fixed (new issue below) | endpoint-extractor.cs:1082-1106 checks enum members first, then constants through FormatConstantLiteral (1116-1158), then field/property symbols, and returns `default!` for object creation, invocation, or a method group. capabilities-emitter.cs:331 omits `default!`. The listed cases (decimal, float, const bool, `= new()`) behave as claimed. See Issue 3 for constants this formatter still gets wrong. |
| M7 | fixed | endpoint-extractor.cs:91-93 returns Empty when the type has no `[NuruRoute]` and InferMessageType is `"Unspecified"`. See Issue 6 for the ordering nit. |
| M9 | fixed | nuru-generator.cs:57 returns null from the `[Operation]` transform when the type also has `[NuruRoute]` (nuru-generator.cs:314-326). |
| M10 | fixed | operation-endpoint-extractor.cs:477-499 caches one MediatorScan per Compilation in a ConditionalWeakTable. `GetValue` is thread-safe, the key is an immutable Compilation (so it cannot go stale), the weak key does not root old compilations, and the FAWMN transform reruns on every compilation, so cross-file handler visibility is not stale. Framework assemblies are skipped (501-505). |
| M11 | fixed | handler-invoker-emitter.cs:439-480: runtime DI tries `GetService<IResponseRenderer<T>>` first, then the visible renderer, then the output strategy. Source-gen tries the registered service (TryResolveRegisteredService, 490-512), then the visible renderer, then the output strategy. |
| M12 | partially fixed | nuru-generator.cs:306-309 accepts record classes, and generator-55 / `T508RecordQuery` covers a record with `{ get; set; }`. Positional records and `init` members, the usual record shape, still fail. See Issue 2. |
| M13 | fixed | route-matcher-emitter.cs:1619-1635 only sets a flag during option parsing. route-matcher-emitter.cs:240 and :359 call EmitJsonMemberAfterMatch after the literal match, and it writes both errors (1641-1664). See Issue 1 for a compile break in the flag name. |
| M14 | fixed (new issue below) | operation-endpoint-extractor.cs:234-254 (bindings) and endpoint-extractor.cs:821-848 (segments) walk base types and skip `[NuruRouteGroup]` bases. See Issue 4 for the name-match edge case. |
| M18 | fixed | interceptor-emitter.cs:1218-1256 is now EmitMissingHandlerOnDispatch. It is called after the match at route-matcher-emitter.cs:244 and :361, and json-args-emitter.cs:375. The call at the start of ExecuteRouteAsync is gone (interceptor-emitter.cs:1172-1176). |
| M19 | fixed | operation-endpoint-extractor.cs:92 uses `routeDescription ?? operationDescription`. `NamedString` returns null when the named argument is absent. |
| M20 | partially fixed | operation-endpoint-extractor.cs:303,669-672 strips only a leading `global::`. `T508Note[]` passes, but `List<T508Note>` or `Dictionary<string, Ns.T>` still reports `json System.Collections.Generic.List<global::T508Note>`. The test (operation-endpoint-derivation.cs:306) covers only an array. |

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1624,1651,1666
- Description: The new argv-reject flag is built from the keyword-escaped local name, `__jsonArgvRejected_{escapedVarName}_{routeIndex}`. When a JSON member's lowercased name is a C# keyword or contextual keyword (CSharpIdentifierUtils.IsKeyword checks both), the flag becomes `__jsonArgvRejected_@params_0`. An `@` inside an identifier does not compile. Concrete input: `[Operation("x")] class X : IQuery<string> { public Dictionary<string,string> Params { get; set; } = []; }`. The same happens with JSON members named `Event`, `Object`, `Group`, `Where`, `Select`, `Field`, `Type`, `Base`, and similar names. Before the fix, the escaped name was used only as the standalone local, where `@params` is valid.
- Suggestion: Build the flag from the unescaped name (as `flagFoundVar` does at route-matcher-emitter.cs:535), or strip a leading `@`. Add a derivation test with a JSON member named `Params`.
- Status: open

### Issue 2 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:666; source/timewarp-nuru-analyzers/generators/emitters/handler-invoker-emitter.cs:309; behavior-emitter.cs:194
- Description: M12 made record classes endpoints, but the command is always built as `T __command = new() { ... }`. A positional record `[Operation("get-user")] public sealed record GetUserQuery(int Id) : IQuery<User>;`, the common contract shape, has no parameterless constructor, so the generated code fails with CS7036. The fix also added `IsInitOnly: false` to IsDerivableProperty. An `init` member is therefore never derived or assigned, so `public required string Name { get; init; }` (on a class or a record) leaves a required member unset in the object initializer, and the build fails with CS9035. Before this delta, init members were derived, and the object initializer can set them. Non-required `init` members now silently disappear from the CLI.
- Suggestion: Drop the `IsInitOnly: false` restriction, because object initializers can set init-only members. For a type with no accessible parameterless constructor, either skip it with a diagnostic or construct it through the primary constructor, binding parameters by name. Add tests for a positional record and a `required init` member.
- Status: open

### Issue 3 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1082-1088,1116-1158
- Description: The constant value now wins over the symbol for anything that is not a direct enum member. ExtractPropertyDefaultValueFromSymbol also serves existing `[Option]`/`[Parameter]` properties (endpoint-extractor.cs:592,945 at the merge base), so this breaks apps that compiled on 3.0. The failing inputs:
  - `[Option] public double Threshold { get; set; } = double.NaN;` emits `NaN`. `float.NaN` emits `NaN.0f`, and `double.PositiveInfinity` emits `Infinity`. 3.0 emitted `global::System.Double.NaN`. All three fail with CS0103 or a syntax error.
  - `[Option] public Env Env { get; set; } = Defaults.Env;`, where `Defaults.Env` is a `const Env`, now emits the underlying number (for example `1`). 3.0 emitted `global::Ns.Defaults.Env`. The result is CS0266.
  - `= Color.Red | Color.Blue` and `= (Color)2` also emit a bare integer and fail with CS0266. These were broken before too, but FormatConstantLiteral already receives `propertyType` and ignores enums.
- Suggestion: In FormatConstantLiteral, emit `double.NaN`, `double.PositiveInfinity`, and `double.NegativeInfinity` (and the float equivalents) explicitly. When `propertyType` (or its `Nullable<T>` argument) is an enum, emit `(global::Ns.Color)3`. Keep the symbol form for a non-enum-type const field whose type is an enum. Add regression tests for NaN and a const-enum default on an `[Option]` property.
- Status: open

### Issue 4 — Severity: bug
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1104-1106; source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs:1622; source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:936-943
- Description: The `default!` sentinel is also the local's initializer, and the generated object initializer always assigns that local to the property. When the option is omitted, the contract's own initializer is overwritten. `public Payload Body { get; set; } = new();` (non-nullable, so the contract promises non-null) arrives at the handler as null, and `public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);` arrives as `TimeSpan.Zero`. Both paths do this: argv (EmitJsonMemberGuard / option parsing) and `--json-args` (DefaultInitializer). The test `Should_keep_decimal_float_const_bool_and_skip_unrepresentable_defaults` asserts `items:null`, so the test locks in this data loss instead of catching it. Capabilities should omit the default, but runtime should not discard the initializer.
- Suggestion: Do not assign a member whose default is not representable unless the caller supplied it. Either track a "supplied" flag and set the property after construction, or emit target-typed `new()` for an ImplicitObjectCreationExpression. Change the test to expect `items:set`.
- Status: open

### Issue 5 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:833,861-868
- Description: The inherited-member walk skips a base property only if an existing segment's `LongForm` (option) or `Name` (parameter) equals the property name, compared ordinally. Attribute segments are lowercased or renamed (`ExtractParameterFromAttribute` uses `name.ToLowerInvariant()` at :969, and `[Option("name")]` sets the long form). Failing case: the base declares `public virtual string Title { get; set; }` and the `[Operation]` type declares `[Parameter] public override string Title { get; set; }`. The base walk adds a second derived option `--Title` with local `title`, which collides with the positional local `title` (PositionalCaptureVar, route-matcher-emitter.cs:442) and fails with CS0128. The bindings walk compares `ParameterName == property.Name` and is not affected, so the two walks disagree.
- Suggestion: Track the property names already bound (from the bindings or from the property symbols visited on the derived type), or skip a base property that the derived type overrides or hides, instead of matching segment names.
- Status: open

### Issue 6 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:78-93
- Description: The M7 gate runs after ValidateRoutePattern. An HTTP-only `[Operation]` with no mediator kind whose name is not a single literal (for example a name containing a space or `{id}`) still gets the InvalidNuruRoutePattern diagnostic, even though it is not an endpoint.
- Suggestion: Move the `!identity.HasNuruRoute && messageType == "Unspecified"` check before ValidateRoutePattern.
- Status: open

### Issue 7 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs:1130-1150; capabilities-emitter.cs:331-334
- Description: `--capabilities` `defaultValue` now carries C# suffixes (`"1.5m"`, `"2.0f"`, `"5u"`, `"10L"`). That applies to existing `[Option]` defaults too, which showed `"1.5"`/`"2"` before. Agents read this catalog, and the suffix is C# syntax, not a value.
- Suggestion: Keep the suffixed literal for code generation, and store a plain invariant string for the capabilities default.
- Status: open

### Issue 8 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.cs:303,669-672
- Description: M20 is only partly fixed. StripGlobalPrefix removes only a leading `global::`, so generic JSON members still show `global::` inside the type arguments.
- Suggestion: Use `ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted))`, and add a `List<T>` case to the test.
- Status: open
