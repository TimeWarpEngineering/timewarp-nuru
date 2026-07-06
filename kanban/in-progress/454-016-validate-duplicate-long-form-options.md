# Validate Duplicate Long Form Options

Parent: 454 (2026-07-06 full code review). Severity: MEDIUM (M13).

## Description

`source/timewarp-nuru-parsing/validation/semantic-validator.cs:280-302` —
`ValidateDuplicateOptionAliases` only inspects `ShortForm`. Verified: `build -v -v` is
rejected, but `build --verbose --verbose` succeeds and compiles to two identical
OptionMatchers. Bare boolean options have no value parameter to collide on, so
duplicate-parameter detection misses it too.

Related dead code in the same area (also see sibling task 454-029): the dup check builds
its own local `seen` dict while `ValidationContext.OptionAliases`
(validation-context.cs:16, populated at semantic-validator.cs:82-83) is write-only.

## Checklist

- [ ] Extend duplicate validation to long forms
- [ ] Test: `build --verbose --verbose` produces a duplicate-option diagnostic
- [ ] Test: short-form rejection still works
- [ ] `ganda runfile cache --clear` + run CI tests

## Notes

### Implementation Plan (2026-07-06)

#### Decisions

| # | Question | Decision |
|---|----------|----------|
| 1 | Error message | Add `bool IsLongForm` to `DuplicateOptionAliasError`; `ToString` distinguishes "duplicate short form" vs "duplicate long form" |
| 2 | Dead `OptionAliases` dict | Remove `ValidationContext.OptionAliases` property + population code entirely (write-only dead code) |
| 3 | Seen dicts | Two separate dicts (`shortSeen`, `longSeen`), one iteration pass |
| 4 | Test file | New `tests/timewarp-nuru-tests/parser/parser-17-duplicate-option-aliases.cs` |

#### Step 1: Update `DuplicateOptionAliasError` record
File: `source/timewarp-nuru-parsing/parsing/validation/semantic-error.cs:72-81`
- Add `bool IsLongForm` positional parameter
- Branch `ToString`: "duplicate short form" vs "duplicate long form" based on `IsLongForm`

#### Step 2: Update diagnostic descriptor
File: `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.semantic.cs:44-51`
- Change messageFormat from "duplicate short form '{0}'" to "duplicate alias '{0}'" (generic, covers both)
- Update description from "same short form" to "same alias"

#### Step 3: DSL interpreter mapping (no change needed)
File: `source/timewarp-nuru-analyzers/generators/interpreter/dsl-interpreter.cs:876-877`
- The pattern match reads only `e.Alias` and `e.ConflictingOptions`; the new `IsLongForm` positional is unused at the analyzer site. No edit required.

#### Step 4: Remove `OptionAliases` dead code
- `source/timewarp-nuru-parsing/parsing/validation/validation-context.cs:13-16` — delete `OptionAliases` property + doc comment
- `source/timewarp-nuru-parsing/parsing/validation/semantic-validator.cs:81-83` — delete the `// Track aliases` block

#### Step 5: Extend `ValidateDuplicateOptionAliases` to long forms
File: `source/timewarp-nuru-parsing/parsing/validation/semantic-validator.cs:279-302`
- Replace local `seen` dict with two dicts: `shortSeen` and `longSeen`
- One iteration: check ShortForm against shortSeen, check LongForm against longSeen
- Emit `DuplicateOptionAliasError` with `IsLongForm: false` for short, `IsLongForm: true` for long

#### Step 6: Create `tests/timewarp-nuru-tests/parser/parser-17-duplicate-option-aliases.cs`
5 tests using `PatternParser.Parse(...)` + `Should.Throw<PatternException>`:
1. `Should_reject_duplicate_short_form_options` — `"build -v -v"` → DuplicateOptionAliasError, IsLongForm false
2. `Should_reject_duplicate_long_form_options` — `"build --verbose --verbose"` → DuplicateOptionAliasError, IsLongForm true
3. `Should_reject_duplicate_long_form_with_value_parameter` — `"build --config {cfg} --config {cfg}"` → at least one DuplicateOptionAliasError with IsLongForm true
4. `Should_allow_distinct_short_and_long_forms` — `"build -v --verbose"` → Should.NotThrow
5. `Should_allow_unique_long_forms` — `"build --verbose --quiet"` → Should.NotThrow

NOTE: Confirm whether `OptionSyntax.ShortForm`/`LongForm` store dashes (`"--verbose"`) or bare names (`"verbose"`) before writing assertions. Inspect the parser/factory.

#### Step 7: JSON serializer context (no change needed)
`[JsonSerializable(typeof(DuplicateOptionAliasError))]` — adding a positional parameter doesn't break it.

#### Step 8: Verify
1. `ganda runfile cache --clear`
2. `dotnet run tests/timewarp-nuru-tests/parser/parser-17-duplicate-option-aliases.cs` (standalone)
3. `dotnet run tests/ci-tests/run-ci-tests.cs` (full CI)

#### Files touched
- Edit: `semantic-error.cs` (add IsLongForm to DuplicateOptionAliasError)
- Edit: `diagnostic-descriptors.semantic.cs` (generic "alias" message)
- Edit: `validation-context.cs` (remove dead OptionAliases)
- Edit: `semantic-validator.cs` (remove dead population code + extend validator)
- Create: `parser-17-duplicate-option-aliases.cs` (5 tests)

#### Risk assessment
- User-visible IDE diagnostic changes from "duplicate short form" → "duplicate alias" for existing short-form cases (acceptable, more accurate)
- No generator changes (parser validation only)
- `DuplicateOptionAliasError` record gains a positional param — only one construction site (the validator), which is updated
