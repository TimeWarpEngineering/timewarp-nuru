# Analyzers and source generator (`source/timewarp-nuru-analyzers`)

Baseline `5a06e900`. Re-checked against the tree on 2026-10-05. The first-run log was a lead only; each item below was confirmed in source.

## A-1 — Roslyn `Location` sits on the emit cache key

- Disposition: **fix-now** (482-008)
- Evidence: `ExtensionMethodCall` stores a Roslyn `Location` (`generators/models/service-extraction-result.cs`). It is a field of the model the emit stage compares. `service-extractor.cs` still does `new ExtensionMethodCall(methodName, invocation.GetLocation())` for `AddLogging` and for any `AddX` that was not lowered. `ServiceDefinition.RegistrationLocation` was already converted to `LocationInfo`. This record was not.
- Why it matters: An app whose `ConfigureServices` calls an opaque extension method misses the emit cache on every edit. Generated behavior is unchanged. The incremental split does not hold for that edit.

## A-2 — Three parser errors never become diagnostics, and the route is still emitted

- Disposition: **fix-now** (482-006)
- Evidence: `MapParseErrorToDiagnostic` (`generators/interpreter/dsl-interpreter.cs`) returns null for anything it does not switch on. `InvalidIdentifierError`, `InvalidModifierCombinationError`, and `AdjacentParametersError` (`source/timewarp-nuru-parsing/parsing/parser/parse-error.cs`) have no arm and no descriptor. On failure, `PatternStringExtractor.ExtractSegmentsWithErrors` still returns a single `LiteralDefinition` of the raw pattern (`generators/extractors/pattern-string-extractor.cs`). The runtime parser rejects the same input.
- Why it matters: `.Map("{a}{b}")`, `.Map("{*name?}")`, and `.Map("{my-param}")` compile with no NURU diagnostic and generate a literal that will not match the arguments the user typed.

## A-3 — Built-in conversions the parser accepts are not emitted

- Disposition: **fix-now** (482-005, with R-5)
- Evidence: `GetClrTypeName` accepts `datetimeoffset` and `version` (`generators/emitters/type-conversion-map.cs`). `GetTypeConstraintFromClrType` maps `DateTimeOffset` to `datetimeoffset` (`generators/extractors/endpoint-extractor.cs`). `GetBuiltInTryConversion` has neither name, so `EmitTypeConversions` falls through to a comment (`// WARNING: No converter found`) and never declares the variable the command initializer uses (`generators/emitters/route-matcher-emitter.cs`).
- Catch-all and repeated options are R-5: those paths call `GetParseExpression`, whose switch is only `int`, `long`, `short`, `byte`, `double`, `float`, `decimal`, `bool`, `datetime`, and `guid`. The fallback expression is the raw string.
- Why it matters: `{*values:uint}` and an endpoint `DateTimeOffset` or `Version` parameter fail the user's build inside generated code, not with a NURU diagnostic.

## A-4 — Optional parameters render as square brackets

- Disposition: **fix-now** (482-007)
- Evidence: `ParameterDefinition.PatternSyntax` renders an optional parameter as `[{Name}]`, not `{name?}` (`generators/models/segment-definition.cs`). `EffectivePattern` is built from that. Overlap, duplicate, unreachable, and REPL-default lookups key on `EffectivePattern`, so they miss the source literal the user wrote. The diagnostic text then shows a pattern the parser would reject (`[` is not route syntax).
- Why it matters: Duplicate and overlap errors on ordinary optional routes do not squiggle the `Map(...)` string, and the message is not a pattern the user can paste back.

## A-5 — Endpoint attributes are matched by simple name

- Disposition: **post-3.0**
- Evidence: `[Parameter]` and `[Option]` are accepted when `AttributeClass.Name` equals the short name (`generators/extractors/endpoint-extractor.cs`, property-binding loop). Namespace and containing assembly are ignored. The same pattern is used for the other endpoint attributes in that extractor.
- Why it matters: A user attribute with the same simple name is treated as a Nuru attribute. Unlikely with these names. It is the type-identity rule the generator otherwise follows. Not a 3.0 functional break on the shipped attribute set.

## A-6 — Two diagnostics are not written for users

- Disposition: **post-3.0**
- Evidence: NURU052's format is `Nuru did not instantiate anything '{0}()' registered.` (`diagnostics/diagnostic-descriptors.service.cs`). NURU_H003 is reported with `handlerExpression.Kind().ToString()` (`validation/handler-validator.cs`), so the message says `InvocationExpression` rather than what the user wrote.
- Why it matters: Both are default-on. The warning still fires and names the method. Wording is the defect. No child was reserved. The fix-now set for 3.0 is 482-001 through 482-015. Message cleanup waits.

## A-7 — Many descriptors have no asserting test, and none have a help link

- Disposition: **post-3.0**
- Evidence: A search of `tests/` for these ids finds no asserting test: NURU_P004, NURU_P005, NURU_P006, NURU_P007, NURU_S005, NURU_A001, NURU_A002, NURU_H001, NURU_H003, NURU_H004, NURU_H006, NURU_H007, NURU_R002, NURU051. NURU_R001 and NURU055 appear only in comments (`routing/routing-07-route-selection.cs`, `generator/generator-26-constructor-dependency-resolution.cs`). No descriptor under `diagnostics/` sets `helpLinkUri`.
- Why it matters: Those rules can regress without a red test. Help links are the same class of analyzer-convention metadata as the shipped/unshipped split, which this task already excluded from the 3.0 criteria (Notes, 2026-10-04).

## Checked, not a 3.0 blocker

- `generators/models/` does not store `SemanticModel`, `Compilation`, `ISymbol`, or syntax nodes. `DslInterpreter` holds a `SemanticModel` only while it extracts. The cache hole that remains is A-1 (`ExtensionMethodCall.Location`), not the semantic model.
- Route patterns go through `PatternParser.TryParse` and `Parser.Parse`. The analyzer does not keep a second grammar. The behavior gaps are the three dropped parse errors (A-2) and `EffectivePattern` not round-tripping `{name?}` (A-4).
- `TypeConversionMap.GetBuiltInConversion` is obsolete. Emitters that need a non-throwing parse call `GetBuiltInTryConversion`. The throwing path that users still hit is `GetParseExpression` (A-3 / R-5).
