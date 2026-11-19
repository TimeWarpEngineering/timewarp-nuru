# Round 1 — general
**Date:** 2026-11-19
**Scope reviewed:** `90af1d6a..a90c3da2`, kitchen excluded. Read the surrounding call sites:
`GetTypeConstraintFromClrType` callers (`endpoint-extractor.properties.cs`, `endpoint-extractor.groups.cs`,
`operation-endpoint-extractor.bindings.cs`, `RepeatedElementConstraint`), `IsJsonMember`/`IsScalar`,
`TypeConversionMap`, `BuiltInElementParse`, `route-matcher-emitter.type-conversions.cs`,
`json-args-emitter.helpers.cs`, and the test props (`tests/timewarp-nuru-tests/Directory.Build.props`,
`tests/ci-tests/Directory.Build.props`).

## Summary

The change maps `FileInfo`, `DirectoryInfo`, and `IPAddress` onto the `fileinfo`, `directoryinfo`, and
`ipaddress` constraint names that `TypeConversionMap` and the scalar emitters already convert, and adds the
three types to the `[Operation]` scalar set. The fix is small and lands on the right seam. The new runfile
uses `.Map<T>()` (no NURU_A003 risk), relies on the `System.Net` global using that both the standalone and the
CI multi-mode props provide, and covers each type on `[NuruRoute]` and on `[Operation]`. The docs, the migrate
skill, and the glossary match the new behavior and name 4.0.0-beta.14. The goal restatement matches the task.

One gap: the `[Operation]` scalar check strips the nullable annotation with an exact-string special case for
the three new types only. That branch has no test, and it leaves `Uri?` and `Version?` operation properties
classified as JSON members, so the scalar set now behaves differently for nullable and non-nullable members of
the same list.

## Issues

### Issue 1 — Severity: suggestion
- File: source/timewarp-nuru-analyzers/generators/extractors/operation-endpoint-extractor.constructors.cs:268
- Description: `IsScalar` compares `type.ToDisplayString()`, which includes the `?` for an annotated
  reference type. The change adds
  `if (display is "System.IO.FileInfo?" or "System.IO.DirectoryInfo?" or "System.Net.IPAddress?") display = display[..^1];`
  so only those three nullable types reach the scalar list. `System.Uri?` and `System.Version?` are in the
  same list but still fall through to `IsJsonMember == true`, so a `public Uri? Endpoint { get; set; }`
  on an `[Operation]` binds as a JSON member while `public FileInfo? Input` binds from argv. The task's
  `## Results` names this choice ("Other nullable displays are unchanged"), but the new branch is not
  covered either: every `[Operation]` property in `routing-44` is `required` and non-nullable, so no test
  reaches the strip.
- Suggestion: drop the per-type string special case. Compare the unannotated display for every reference
  type, for example
  `ITypeSymbol bare = type.NullableAnnotation == NullableAnnotation.Annotated ? type.WithNullableAnnotation(NullableAnnotation.NotAnnotated) : type;`
  and then `bare.ToDisplayString()` against the list. Add an `[Operation]` case to `routing-44` with nullable
  `FileInfo?`, `DirectoryInfo?`, `IPAddress?` (and `Uri?`) properties that binds from argv when given and stays
  unset when omitted.
- Status: open
