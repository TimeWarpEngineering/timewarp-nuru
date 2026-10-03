# Review large test files for refactoring opportunities

## Description

Multiple test files exceed 500 lines. While test files can be larger than production code due to test data, files over 600+ lines should be reviewed for potential splitting by test category or feature area.

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

Regenerated 2026-09-28 (triage 477) from `wc -l` over `tests/**/*.cs`; the 2025 list named six files that no
longer exist. Review each; split only where the guidelines below apply.

- [x] `tests/timewarp-nuru-tests/generator/generator-26-constructor-dependency-resolution.cs` — subjects extracted (runfile 380, fixtures 361)
- [x] `tests/timewarp-nuru-tests/repl/repl-31-multiline-buffer.cs` (726) — kept; one sectioned `MultilineBuffer` surface
- [x] `tests/timewarp-nuru-tests/routing/routing-05-option-matching.cs` — split into modifier matrix, boolean/mixed/typed, and aliases
- [x] `tests/timewarp-nuru-tests/repl/repl-23-key-binding-profiles.cs` (659) — kept; one sectioned profile matrix
- [x] `tests/timewarp-nuru-tests/repl/repl-18-psreadline-keybindings.cs` (557) — kept; one sectioned binding surface
- [x] `tests/timewarp-nuru-tests/repl/repl-28-text-selection.cs` (525) — kept; one sectioned selection surface
- [x] `tests/timewarp-nuru-tests/generator/generator-01-intercept.cs` (523) — kept; one sectioned intercept list
- [x] `tests/timewarp-nuru-tests/repl/repl-33-yank-arguments.cs` (520) — kept; parse and yank are one sectioned feature
- [x] `tests/timewarp-nuru-tests/lexer/lexer-15-advanced-features.cs` (515) — kept; one advanced-tokenization list
- [x] `tests/timewarp-nuru-tests/generator/generator-15-runtime-di.cs` (507) — kept; nine scenarios, subjects do not bury them
- [x] `tests/timewarp-nuru-tests/repl/repl-29-word-operations.cs` (505) — kept; one sectioned word-operation surface

## Notes

### Test File Considerations

Test files are often larger because they contain:
- Test data/fixtures inline
- Multiple test methods covering edge cases
- Setup/teardown code
- Comprehensive coverage of a feature

### Splitting Strategies

1. **By test category** - Split unit/integration/edge case tests
2. **By feature subset** - Group related functionality tests
3. **Extract test data** - Move large test data to separate files
4. **Extract helpers** - Move test utilities to shared files

### Lower Priority

Test files are lower priority than production code because:
- They don't affect runtime performance
- They're less frequently read by users
- Larger test files don't increase cognitive load in the same way
- Test organization is more flexible

### Guidelines for Decision

Split a test file if:
- It covers multiple unrelated features
- Finding specific tests is difficult
- Test data could be externalized
- Shared test utilities could be extracted

## Session

- Implementer: Grok session 01a1015d-d3f0-7710-aa84-ac2484e5b9ec (2026-10-03)

## Results

Two files met the split guidelines. The other nine stay. Each of those is one feature, and section banners or scenario class names already identify the tests.

`generator-26-constructor-dependency-resolution.cs` was 729 lines because the service graphs the source generator compiles sat above the ten scenario classes. Those subjects are now `generator-26-constructor-dependency-fixtures.cs` (global namespace, no entry point). `tests/timewarp-nuru-tests/generator/Directory.Build.props` compiles that sibling when the project name is `generator-26-constructor-dependency-resolution.cs`, which is how file-based apps name the runfile. Multi-mode still picks the fixtures up through the `tests/**/*.cs` glob, and the include condition limits the extra compile to that runfile. The scenario runfile is 380 lines.

`routing-05-option-matching.cs` was 717 lines and 31 tests with no section breaks, and the method names differ by only the modifier under test. It is now three runfiles aligned with the option-matching matrix:

- `routing-05-option-modifier-matrix.cs` — required and optional flag/value combinations (11)
- `routing-05-boolean-mixed-typed-options.cs` — boolean flags, mixed options, typed values (7)
- `routing-05-option-aliases.cs` — long and short aliases (13)

Assertions are unchanged. `routing-05-option-matching.cs` is removed.

Kept:

- `repl-31-multiline-buffer.cs` — one `MultilineBuffer` API, sectioned by operation (construction, `SetText`, insert, delete, cursor, text, position).
- `repl-23-key-binding-profiles.cs` — one profile matrix. Shift+Enter cases sit together across Default, Emacs, Vi, and VSCode.
- `repl-18-psreadline-keybindings.cs`, `repl-28-text-selection.cs`, `repl-29-word-operations.cs`, `repl-33-yank-arguments.cs` — one REPL feature each, sectioned. Argument parsing in `repl-33` feeds yank.
- `generator-01-intercept.cs` — one intercept list, sectioned by route, parameter, option, and group.
- `lexer-15-advanced-features.cs` — advanced tokenization cases for one lexer. Names identify the pattern.
- `generator-15-runtime-di.cs` — nine runtime-DI scenarios. The subjects are in the same file and do not bury the list the way the constructor-dependency graphs did.

`runfiles/generate-internals-visible-to.cs` refreshed the three `internals-visible-to.g.cs` files. That drops `routing-05-option-matching` and adds the new runfiles, the fixtures file, and the `generator` directory name from the new `Directory.Build.props`. It also adds stems that were already on disk and missing from the attributes: `generator-50-json-args-reserved`, `generator-51-repl-service-scopes`, `repl-46-key-binding-config-loader`, `repl-47-key-binding-catalog`, and `routing-33-json-args`.

### How to validate

Smoke:

```bash
dotnet run tests/timewarp-nuru-tests/generator/generator-26-constructor-dependency-resolution.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-05-option-modifier-matrix.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-05-boolean-mixed-typed-options.cs
dotnet run tests/timewarp-nuru-tests/routing/routing-05-option-aliases.cs
dotnet build tests/ci-tests/run-ci-tests.cs
```

Expect:

- `generator-26-constructor-dependency-resolution.cs` exits 0. Grand total 10 passed, 0 failed, across `SingleDependency`, `MultipleDependencies`, `MultiLevelChain`, `DiamondPattern`, `OptionalParameter`, `MultipleConstructors`, `LifetimeMismatch`, `MixedBuiltInAndCustom`, `TransientWithDependencies`, and `CircularDependencyRuntimeDI`.
- `routing-05-option-modifier-matrix.cs` exits 0 with 11 passed.
- `routing-05-boolean-mixed-typed-options.cs` exits 0 with 7 passed.
- `routing-05-option-aliases.cs` exits 0 with 13 passed.
- `dotnet build tests/ci-tests/run-ci-tests.cs` exits 0, so the multi-mode assembly compiles the fixtures once beside the scenario runfile and compiles the three routing files.
