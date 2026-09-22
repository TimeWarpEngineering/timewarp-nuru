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

- [x] Escape defaults (M8)
- [x] FileInfo/DirectoryInfo catch (M21)
- [x] Tests
- [x] `ganda runfile cache --clear` + CI tests
- [x] Review round 1 (general, effort 1) and disposition on this id

## Notes

Evidence: parent 470 `review/round-1/merged.md` M8, M21.

Review trail: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Session

- Implementer: grok session 01a0c887-e9be-70a2-9c90-e33d2f4642c1 (2026-09-22)
- Review: Grok session 01a0c897-3b40-7ec3-8305-0294d4448f04 (2026-09-22)

## Results

Service constructor optional defaults are emitted as compiling expressions. Strings and chars use `SymbolDisplay.FormatLiteral`. Other primitives use `SymbolDisplay.FormatPrimitive`, with the `F`/`M`/`U`/`L`/`UL` suffix and non-finite names (`float.NaN`, `double.PositiveInfinity`, and the rest) that `FormatPrimitive` does not include. Enum defaults are fully-qualified members (`global::Namespace.Enum.Member`). A constant that is not a single named member is a cast of the underlying literal.

`FileInfo` and `DirectoryInfo` conversions in the route matcher catch `Exception`. That covers the documented constructor set (`ArgumentException`, `PathTooLongException`, `NotSupportedException`, `UnauthorizedAccessException`, `SecurityException`), so those failures stay on the invalid-value exit. All eight emit sites (required and optional, positional parameter and option, both types) use that catch.

`generator-45` is the Roslyn-hosted regression. The string default is the value of `"a\"b\\c\n"` (quote, backslash, newline). The emitted call is `new global::Gen45Ns.Gen45Service("a\"b\\c\n", global::Gen45Ns.Gen45Mode.Prod)`. Each of the four `FileInfo` constructions and four `DirectoryInfo` constructions is followed by `catch (global::System.Exception)`.

### Files changed

- `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- `tests/timewarp-nuru-tests/generator/generator-45-constructor-default-literals.cs`
- `tests/timewarp-nuru-tests/routing/routing-23-uri-fileinfo-directoryinfo.cs` (comment only)
- `tests/ci-tests/Directory.Build.props` and `tests/ci-tests/run-ci-tests.cs` (standalone phase; CS0433 excludes it from the multi-mode assembly)

### Test outcomes

- `generator-45-constructor-default-literals.cs`: 2 passed
- `ganda runfile cache --clear` then `dotnet run tests/ci-tests/run-ci-tests.cs`: exit 0. Multi-mode 1643 passed, 7 skipped, 0 failed (total 1650). Standalone phase passed, including generator-45 (2 passed).

### How to validate

**Smoke**

```bash
cd /home/steve/worktrees/github.com/TimeWarpEngineering/timewarp-nuru/task-470-006-escape-service-constructor-defaults-in-source-gen
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/generator/generator-45-constructor-default-literals.cs
```

**Expect**

- 2 passed
- `Should_emit_escaped_string_and_qualified_enum_defaults`: generated source contains `new global::Gen45Ns.Gen45Service("a\"b\\c\n", global::Gen45Ns.Gen45Mode.Prod)`
- `Should_catch_fileinfo_and_directoryinfo_constructor_exceptions`: four `new global::System.IO.FileInfo(` and four `new global::System.IO.DirectoryInfo(` sites, each followed by `catch (global::System.Exception)` and not `catch (global::System.ArgumentException)`

**Automated gate**

```bash
ganda runfile cache --clear
dotnet run tests/ci-tests/run-ci-tests.cs
# expect: exit 0; multi-mode 1643 passed, 7 skipped, 0 failed; standalone phase includes generator-45 (2 passed)
```

### Review disposition

- **Outcome:** `clean`
- **Effort / roster:** 1, general only
- **Rounds:** 1 (`review/round-1/`)
- **Final counts:** 0 open / 0 fixed / 0 wontfix (bug 0, suggestion 0, nit 0)
- **Paths:** `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`
- No sibling apply-review task. No wontfix and no escalation.
