# Design issue: 454-012 plan assumes IParameterSymbol availability at all IsServiceType call sites

## Description

The implementation plan for 454-012 (M8) assumes all `IsServiceType` call sites have `IParameterSymbol`/`ITypeSymbol` available. This is false — the lambda extraction path (`handler-extractor.cs:103,263`) only has `RoslynParameterSyntax` nodes and derives `typeName` via string operations. Replacing the string heuristic requires first binding lambda parameters to symbols via `SemanticModel.GetSymbolInfo`/`GetTypeInfo` — a non-trivial refactor, not a simple signature change. This is an architectural mismatch between the plan's assumption and the actual dual-path design (syntax-based lambda extraction vs symbol-based method extraction).

## Checklist

- [x] Document root cause and affected call sites — see Root-Cause Analysis
- [x] Identify required refactor scope for lambda binding — see Refactor Scope

## Notes

Created as blocker for 454-012. See handler-extractor.cs lines 103 and 263 for the syntax-only lambda path.

## Root-Cause Analysis (reviewer, 2026-07-13)

**Verdict: the "non-trivial refactor / architectural mismatch" framing is overstated. M8's
`IsServiceType` fix is NOT blocked. Downgrade this from a blocker to a scoped design note.**

The premise ("the lambda extraction path only has `RoslynParameterSyntax` and derives
`typeName` via strings") is true only of a *fallback branch that runs on broken/partial
code*, not the path valid code takes. `handler-extractor.cs` has a two-tier design per
handler shape:

- **Parenthesized lambda** (`ExtractFromParenthesizedLambda`, ~78-119): calls
  `semanticModel.GetSymbolInfo(lambda)`; when `.Symbol is IMethodSymbol` (the normal case
  for well-formed code) it delegates to **`ExtractFromMethodSymbol`** (371-406), which
  iterates `IParameterSymbol param` and already has `param.Type` (an `ITypeSymbol`) in
  hand. The syntax-only `foreach` at **103** is reached ONLY when the symbol does not
  resolve to an IMethodSymbol — i.e. partial/uncompilable code.
- **Anonymous method** (`delegate {...}`, ~236-277): identical structure — delegates to
  `ExtractFromMethodSymbol` at **241**; the syntax-only `foreach` at **263** is the same
  broken-code fallback.
- **Method group / member access** (~377-459): fully symbol-based already.

So every `IsServiceType` call site that runs on VALID code already flows through
`ExtractFromMethodSymbol` with a symbol available. The two string-only sites (103, 263)
are unreachable for code that compiles.

### The four `IsServiceType` call sites
| Line | Path | Has symbol? | Runs on |
|------|------|-------------|---------|
| 386 | `ExtractFromMethodSymbol` | YES (`param.Type`) | all valid lambdas/anon/method-group/member-access |
| 442 | second symbol-based extractor | YES (`param.Type`) | valid code |
| 103 | parenthesized-lambda fallback | no (string only) | partial/uncompilable code only |
| 263 | anonymous-method fallback | no (string only) | partial/uncompilable code only |

The actual defect M8 targets — the `shortName[0]=='I' && char.IsUpper(shortName[1])`
name heuristic at line 632 (over-matching `IData`, needing a hand-maintained IPAddress
exclusion at 677) — lives in the shared `IsServiceType(string)` helper. Replacing it with
`TypeKind.Interface` on the symbol fixes the sites that matter (386, 442) with a trivial
signature change, exactly as the plan assumed.

## Refactor Scope (reviewer, 2026-07-13)

1. **Add symbol overloads (the real fix).**
   - `IsServiceType(ITypeSymbol type)`: `if (IsBuiltInRouteBindableType(type)) return false;`
     then a service iff `type.TypeKind == TypeKind.Interface` (plus known-service checks by
     namespace/OriginalDefinition for `Microsoft.Extensions.*`, `ILogger`,
     `IServiceProvider`). This drops the "I+uppercase" name heuristic entirely — `IPAddress`
     is `TypeKind.Class`, so it self-excludes and the manual entry at 677 becomes redundant.
   - `IsBuiltInRouteBindableType(ITypeSymbol)`: prefer `SpecialType` for primitives +
     `SymbolEqualityComparer` against the known CLR types, instead of string switch.
2. **Point the two symbol sites at the symbol overload:** at 386 and 442, pass
   `param.Type` instead of the `typeName` string. This is the "simple signature change."
3. **The two syntax-only fallbacks (103, 263) — pick one:**
   - **(A) Minimal / recommended default:** keep the `IsServiceType(string)` overload on
     these two branches. They fire only on code that does not compile, and post-454-011 the
     interpreter/extractor is fail-soft, so a misclassification there cannot crash the host
     or affect valid builds. Add a comment noting valid code never reaches here.
   - **(B) Thorough:** the `semanticModel` IS in scope at both fallback sites, so bind each
     parameter's type via `semanticModel.GetTypeInfo(param.Type, ct).Type` (or
     `GetDeclaredSymbol(param)?.Type`) and use the symbol overload when non-null, string
     overload when null. ~3-4 lines per site. Closes the gap fully but only benefits broken
     code.
   Recommend (A) unless we specifically want correct classification inside incomplete code.

**Net for 454-012:** unblock it. The IsServiceType portion of M8 is a symbol overload + two
one-line call-site edits (option A). Only escalate if we choose option B and want the
lambda-parameter symbol-binding — and even that is ~8 lines, not an architectural rewrite.
The `isRepeated` half of M8 (endpoint-extractor) was never blocked: `property.Type` is
already an `ITypeSymbol` there.
