# Escape service constructor defaults in source-gen DI

Parent: 470 (2026-09-04 full-repo review). Severity: bug (M8). Suggestion folded: M21.

## Description

`GetDefaultValueExpression` (`service-extractor.cs:409-419`) emits constructor optional defaults into generated `new T(...)`. Strings are wrapped without escaping `"`, `\`, or newlines — a default like `"a\"b"` produces non-compiling interceptor code. The `_ => defaultValue.ToString()` arm emits unqualified enum member names.

Endpoint property defaults already use `SymbolDisplay.FormatLiteral` (`endpoint-extractor.cs:977-980`).

M21: emitted `FileInfo` / `DirectoryInfo` conversions catch only `ArgumentException` (`route-matcher-emitter.cs:771-778`); constructors can also throw `PathTooLongException` / `NotSupportedException`.

## Requirements

- Use SymbolDisplay.FormatPrimitive / FormatLiteral for strings and chars; fully-qualified enum members.
- Generator-hosted regression with a string default containing quotes/backslashes and an enum optional parameter.
- Catch the documented FileInfo/DirectoryInfo constructor exception set (or Exception) so conversion stays fail-soft (M21).

## Checklist

- [ ] Escape defaults (M8)
- [ ] FileInfo/DirectoryInfo catch (M21)
- [ ] Tests
- [ ] `ganda runfile cache --clear` + CI tests

## Notes

Evidence: parent 470 `review/round-1/merged.md` M8, M21.
