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
