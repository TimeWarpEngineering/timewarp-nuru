# Round 1 — general
**Date:** 2026-11-19
**Scope reviewed:** `7496e7ce..HEAD` (fix `43d0e297` + kanban land), plus surrounding call sites in
`source/timewarp-nuru-analyzers/generators/` (route-matcher option parsing, positional type conversions,
service resolver, handler extractors).

## Summary

The change adds `CSharpIdentifierUtils.Bare` and uses it wherever an option or `IOptions<T>` local name
(possibly `@type`) is spliced into a longer generated identifier, and it switches every handler return-type
emission from `ShortTypeName` to the stored `global::`-qualified `FullTypeName` / `UnwrappedTypeName`.
Both fixes are correct and match the restated goal.

Verified by reading:

- Every compound built from a possibly-escaped `varName` in the option-conversion and enum-array paths now
  goes through `Bare`. The option-parsing, repeated-list (`__{varName}_list_`), flag-found, and positional
  (`__{varName}_{routeIndex}`, `param.CamelCaseName`) paths already used the raw, unescaped name, so they
  were never affected. Constructor-dependency locals use `ParameterName` from the symbol (unescaped).
- No emitter reads `HandlerReturnType.ShortTypeName` any more (`rg "\.ShortTypeName" generators/emitters`
  is empty); no `MinimallyQualifiedFormat` string reaches generated code.
- Nullability is unchanged: neither `FullyQualifiedFormat` nor `MinimallyQualifiedFormat` includes the
  nullable-reference modifier, and both render `int?` for nullable value types.
- Async-no-value delegates: lambda extraction returns `HandlerReturnType.Task` for an `async` lambda with no
  value return, so `GetReturnTypeName` now yields `global::System.Threading.Tasks.Task` where it yielded
  `Task`. Same type, now unambiguous.
- `Unit` keeps `global::TimeWarp.Mediator.Unit` because that is the stored unwrapped name; the special case
  removal is safe and generator-78 locks it.
- generator-78 exercises enum, `int?`, and enum-array option properties named `Type`, and colliding `Mode`
  enums through delegate option conversion, delegate returns, a method-group return, and an async return,
  with a regex guard against any short `Mode` type use in the generated text.
- Docs and skill updates match the checklist and record the beta.14 failure.

No bugs found. Two nits on the validation record and test coverage.

## Issues

### Issue 1 — Severity: nit
- File: kanban/in-progress/591-006-generated-code-breaks-on-some-member-and-type-names/task.md (Checklist,
  `### How to validate`)
- Description: The checklist item "`ganda runfile cache --clear`, then CI tests green" is checked, but the
  recorded results show only the build, generator-78, and the audit; Results say "The full suite runs on the
  pull request." The return-type change touches every delegate handler's emitted signature, so the full CI
  suite is the meaningful regression gate and it is not in the record. generator-25 (the original Unit
  regression) could not run standalone (pre-existing RCS1163) and was replaced by a generator-78 case, which
  is disclosed.
- Suggestion: Record a CI-tests run, or leave that checklist line unchecked and point at the PR's CI.
- Status: open

### Issue 2 — Severity: nit
- File: source/timewarp-nuru-analyzers/generators/emitters/service-resolver-emitter.cs:328;
  route-matcher-emitter.option-conversions.cs:47,100,251
- Description: `Bare` was also applied to the delegate `IOptions<T>` value/validator locals, the custom
  converter option path (`__converter_`), and the optional `Uri` option path. generator-78 does not cover
  a keyword-named parameter on those paths, so they are untested hardening beyond the two reported items.
- Suggestion: Optionally add a keyword-named `IOptions<T>` delegate parameter and a custom-converter option
  named `Type` to generator-78.
- Status: open
