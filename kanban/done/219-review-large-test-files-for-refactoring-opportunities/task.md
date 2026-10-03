# Review large test files for refactoring opportunities

## Description

Multiple test files exceed 500 lines. While test files can be larger than production code due to test data, files over 600+ lines should be reviewed for potential splitting by test category or feature area.

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

Regenerated 2026-09-28 (triage 477) from `wc -l` over `tests/**/*.cs`; the 2025 list named six files that no
longer exist. Review each; split only where the guidelines below apply.

- [x] `tests/timewarp-nuru-tests/generator/generator-26-constructor-dependency-resolution.cs` (729) — kept; subjects stay inline above the scenario classes
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

### Possible follow-up

These files crossed 500 lines after the 2026-09-28 list. This round did not review them:

- `tests/timewarp-nuru-tests/help/help-01-per-route-help.cs` (551)
- `tests/timewarp-nuru-tests/routing/routing-33-json-args.cs` (544)
- `tests/timewarp-nuru-tests/repl/repl-46-key-binding-config-loader.cs` (510)

## Send-back 2026-10-03 (human gate on PR #280)

PR #280 was reviewed at the merge gate and sent back for one change. Keep everything else.

**Revert the generator-26 extraction.** The per-folder `tests/timewarp-nuru-tests/generator/Directory.Build.props`
with a `Compile Include` guarded by `'$(MSBuildProjectName)' == 'generator-26-constructor-dependency-resolution.cs'`
is build plumbing for a single test, keyed on an undocumented file-based-app naming detail, and fails silently on
a rename. A shorter file is not worth that. The lexer-folder props is not a precedent: it shares a helper every
lexer test uses, unconditionally.

- [x] Delete `tests/timewarp-nuru-tests/generator/Directory.Build.props`
- [x] Delete `tests/timewarp-nuru-tests/generator/generator-26-constructor-dependency-fixtures.cs`
- [x] Restore the subjects inline in `generator-26-constructor-dependency-resolution.cs` (729 lines is accepted).
      A section banner between subjects and scenarios is welcome; no other restructuring.
- [x] Regenerate the three `internals-visible-to.g.cs` files (`runfiles/generate-internals-visible-to.cs`) so the
      fixtures stem and the `generator` directory entry are dropped again; keep the other new stems.
- [x] Re-run smoke: `dotnet run tests/timewarp-nuru-tests/generator/generator-26-constructor-dependency-resolution.cs`
      (expect 10 passed) and `dotnet build tests/ci-tests/run-ci-tests.cs`
- [x] Update `## Results` (generator-26 is now a "kept" entry) and the `## Checklist` line above
- [x] Push to the same task branch; PR #280 stays open — do not open a new PR

**Keep:** the routing-05 three-way split, the internals-visible-to refresh of pre-existing missing stems, and the
nine "kept" decisions.

**Not requested this round:** reviewing `help-01-per-route-help.cs` (551), `routing-33-json-args.cs` (544), and
`repl-46-key-binding-config-loader.cs` (510), which crossed 500 lines after the 2026-09-28 list was generated.
Record them under Notes as a possible follow-up only.

## Session

- Implementer: Grok session 01a1015d-d3f0-7710-aa84-ac2484e5b9ec (2026-10-03)
- Implementer: Grok session 01a1022c-a940-73f2-a2c5-3ff9233e748c (2026-10-03) — send-back: restore generator-26 subjects inline
- Review oracle: Claude Opus 5.5 (2026-10-03), effort 3, roster general (subagent af0976436bd743e3b)
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-03T11:11:02Z
- Review oracle: review by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 200 — 2026-10-03T14:26:08Z

## Results

One file met the split guidelines. The other ten stay. Each of those is one feature, and section banners or scenario class names already identify the tests.

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
- `generator-15-runtime-di.cs` — nine runtime-DI scenarios. The subjects stay in the same file.
- `generator-26-constructor-dependency-resolution.cs` (729) — one constructor-dependency surface. The service graphs the source generator compiles stay in this runfile, above the ten scenario classes. The `JARIBU TESTS` banner separates subjects from scenarios. The merge gate on PR #280 rejected a sibling fixtures file plus a project-name `Compile` include, so that extraction is gone.

`runfiles/generate-internals-visible-to.cs` refreshed the three `internals-visible-to.g.cs` files. That drops `routing-05-option-matching` and adds the routing-05 runfiles. It also adds stems that were already on disk and missing from the attributes: `generator-50-json-args-reserved`, `generator-51-repl-service-scopes`, `repl-46-key-binding-config-loader`, `repl-47-key-binding-catalog`, and `routing-33-json-args`. Regeneration after the send-back drops `generator-26-constructor-dependency-fixtures` and the `generator` directory entry.

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
- `dotnet build tests/ci-tests/run-ci-tests.cs` exits 0, so the multi-mode assembly compiles generator-26 with its subjects inline and compiles the three routing files.

### Review disposition

- Rounds: 1. Effort 3 (by-diff), roster: general. That round reviewed the fixtures extraction. The human gate on PR #280 then required the subjects back inline, which this send-back does.
- Final counts: bug 0, suggestion 0, nit 1 (wontfix). 0 open.
- Disposition: **accepted-exceptions** for that round. M1 (nit) said the `MSBuildProjectName` condition could not be checked statically. The send-back removes that condition and the fixtures file, so M1 no longer applies to the tree.
- Artifacts: `review/review-framework.md`, `review/round-1/general.md`, `review/round-1/merged.md`, `review/disposition.md`.
