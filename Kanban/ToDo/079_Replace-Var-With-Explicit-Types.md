# Replace var With Explicit Types

## Description

Convert all `var` keyword usage to explicit type declarations throughout the codebase. This eliminates ambiguity for both developers and AI assistants when reading code.

## Requirements

- All `var` usages replaced with explicit types
- Code compiles without errors
- EditorConfig updated to enforce explicit types going forward

## Checklist

### Implementation
- [ ] Update `.editorconfig` to set `csharp_style_var_when_type_is_apparent = false:suggestion`
- [ ] Use IDE bulk fix ("Fix all occurrences in Solution") to convert all `var` to explicit types
- [ ] Verify solution builds successfully
- [ ] Change `.editorconfig` to `csharp_style_var_when_type_is_apparent = false:warning` to enforce

## Notes

~816 `var` usages identified in `.cs` files.

**Process:**
1. Set severity to `suggestion` first (allows compilation with `TreatWarningsAsErrors`)
2. IDE can still offer bulk fixes for suggestions
3. After all fixed, change to `warning` so future `var` usage breaks build

**EditorConfig changes (all three settings):**
```editorconfig
csharp_style_var_elsewhere = false:warning
csharp_style_var_for_built_in_types = false:warning
csharp_style_var_when_type_is_apparent = false:warning  # was true:warning
```

**Why not other approaches:**
- `sed`/regex can't determine types
- Manual is too slow for 816 occurrences
- IDE has Roslyn type information and can do bulk replacement correctly
