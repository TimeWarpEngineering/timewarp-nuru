# Optional Flag Alias Syntax - Design Decision

## Problem Statement

When combining option aliases (long and short forms) with the optionality modifier (`?`), there was no specification for where the `?` should be placed or how it applies to aliased options.

## Ambiguity Examples

Without clear specification, multiple interpretations were possible:

```csharp
// Where does the ? go?
--output?,-o {file}   // ? on long form only?
--output,-o? {file}   // ? on short form only?
--output?,-o? {file}  // ? on both (redundant)?
(--output,-o)? {file} // Grouping syntax (adds complexity)?
```

## Design Decision

### Canonical Syntax

**The `?` modifier is placed AFTER the alias group:**

```csharp
--output,-o? {file}
```

**Semantics:**
- The `?` applies to the **entire option** (both long and short forms)
- Both `--output` and `-o` are optional
- If either form is used, it must be followed by the parameter (unless parameter is also optional)

### Rationale

1. **Consistency with Parameter Syntax**: Just as `{param?}` puts the modifier after the name, `--flag,-f?` puts the modifier after the alias group
2. **Parser Implementation**: The parser reads the entire alias group first, then checks for modifiers
3. **Simplicity**: Single `?` applies to both forms - no need for redundant syntax
4. **No New Grammar**: Doesn't require introducing grouping parentheses

### Examples

#### Optional Boolean Flag with Alias
```csharp
builder.Map("build --verbose,-v?", (bool verbose) => ...);

// Valid invocations:
// build              → verbose = false
// build --verbose    → verbose = true
// build -v           → verbose = true
```

#### Optional Flag with Required Value
```csharp
builder.Map("backup {source} --output,-o? {file}",
    (string source, string? file) => ...);

// Valid invocations:
// backup /data                    → file = null
// backup /data --output result.tar → file = "result.tar"
// backup /data -o result.tar       → file = "result.tar"

// Invalid:
// backup /data --output            → Error: --output requires value
// backup /data -o                  → Error: -o requires value
```

#### Optional Flag with Optional Value
```csharp
builder.Map("build --config,-c? {mode?}",
    (string? mode) => ...);

// Valid invocations:
// build                → mode = null
// build --config       → mode = null (flag present, value omitted)
// build --config debug → mode = "debug"
// build -c             → mode = null
// build -c release     → mode = "release"
```

## Rejected Alternatives

### Alternative 1: `?` on Long Form Only
```csharp
--output?,-o {file}  // ❌ Rejected
```

**Problems:**
- Ambiguous: Does `-o` also become optional?
- Asymmetric: Why would only long form be optional?
- Confusing to users

### Alternative 2: Redundant `?` on Both Forms
```csharp
--output?,-o? {file}  // ❌ Rejected
```

**Problems:**
- Redundant syntax
- More verbose with no benefit
- What if only one has `?`? Creates more ambiguity

### Alternative 3: Grouping with Parentheses
```csharp
(--output,-o)? {file}  // ❌ Rejected
```

**Problems:**
- Adds new syntax (parentheses) to the grammar
- Inconsistent with parameter modifier placement
- More complex to parse and validate
- Overkill for a simple concept

## Grammar Update

### Before (Ambiguous)
```ebnf
Option = OptionName [ "," OptionAlias ] [ "{" Parameter "}" ] [ "?" ]
```

**Problem:** Order of `?` and parameter was unclear with aliases

### After (Clear)
```ebnf
Option = OptionName [ "," OptionAlias ] [ "?" ] [ "{" Parameter "}" ]
```

**Clarification:** When alias is present, `?` appears after the alias and applies to both forms

## Implementation Notes

### Parser Behavior

The parser (Source/TimeWarp.Nuru.Parsing/Parsing/Parser/Parser.cs:245-273):

```csharp
private OptionSyntax ParseOption()
{
    // 1. Parse option prefix (-- or -)
    // 2. Parse option name
    // 3. Parse alias if present (,short-form)
    // 4. Check for optionality modifier (?)  ← After alias group
    // 5. Parse description if present (|description)
    // 6. Parse parameter if present ({param})
}
```

This implementation means `--output,-o?` is parsed as:
1. Long form: `--output`
2. Alias: `,` then `-o`
3. Optionality: `?` applies to entire option
4. Parameter: `{file}` if present

### Runtime Behavior

During route matching, the `IsOptional` flag on `OptionSyntax` controls whether the option must be present:

- **`IsOptional = false`** (required): Route only matches if `--output` or `-o` is present
- **`IsOptional = true`** (optional): Route matches whether option is present or absent

Both long and short forms share the same `IsOptional` flag.

## Test Coverage

The following test coverage ensures this behavior is validated:

### Parser Tests
- `Should_parse_optional_flag_with_alias_boolean()` - Pattern: `--verbose,-v?`
- `Should_parse_optional_flag_with_alias_and_value()` - Pattern: `--output,-o? {file}`
- `Should_parse_optional_flag_with_alias_and_optional_value()` - Pattern: `--config,-c? {mode?}`

### Routing Tests
- Match with long form: `cmd --output file.txt`
- Match with short form: `cmd -o file.txt`
- Match with flag omitted: `cmd` → binds `null`
- Match with flag but no value: `cmd --output` or `cmd -o`

## Migration Impact

### Existing Code
This design decision **codifies existing behavior** - no breaking changes:

```csharp
// Already works (undocumented)
.Map("build --verbose,-v", (bool verbose) => ...)

// Now documented and tested
.Map("build --verbose,-v?", (bool verbose) => ...)
```

### Future Analyzer Rule (NURU011)

A future analyzer rule could warn about potentially confusing patterns:

```csharp
// Potential warning: ? placement before alias
.Map("build --verbose?,-v", ...)
// Suggestion: Place ? after alias group: --verbose,-v?
```

## Summary

**Canonical Syntax:** `--long,-short? {param}`

**Key Points:**
- ✅ `?` after alias applies to both forms
- ✅ Consistent with parameter modifier placement
- ✅ No new grammar constructs needed
- ✅ Matches current parser implementation
- ✅ Simple and unambiguous
