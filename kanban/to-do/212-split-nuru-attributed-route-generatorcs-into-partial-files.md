# Split nuru-attributed-route-generator.cs into partial files

## Description

The `nuru-attributed-route-generator.cs` file (854 lines) handles attribute extraction, code generation, and pattern building for `[NuruRoute]` attributed classes. These distinct responsibilities should be split into focused partial files.

**Location:** `source/timewarp-nuru-analyzers/analyzers/nuru-attributed-route-generator.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Creation
- [ ] Create `nuru-attributed-route-generator.extraction.cs` - Attribute extraction methods
- [ ] Create `nuru-attributed-route-generator.codegen.cs` - Code generation methods
- [ ] Create `nuru-attributed-route-generator.patterns.cs` - Pattern string building

### Documentation
- [ ] Add `<remarks>` to main file listing all partial files
- [ ] Add XML summary to each new partial file

### Verification
- [ ] All analyzer tests pass
- [ ] Source generation works correctly
- [ ] Build succeeds

## Notes

### Proposed Split

| New File | ~Lines | Content |
|----------|--------|---------|
| `.extraction.cs` | ~200 | `ExtractRouteInfo()`, `ExtractParameterInfo()`, `ExtractOptionInfo()`, `ExtractGroupOptionInfo()` |
| `.codegen.cs` | ~270 | `GenerateRegistrationCode()`, `GenerateRouteConstant()`, `GenerateOptionCall()`, `GenerateRegistrationCall()`, `GenerateAliasRouteConstant()`, `GenerateAliasRegistrationCall()` |
| `.patterns.cs` | ~100 | `BuildPatternString()`, `BuildAliasPatternString()`, `BuildOptionPatternPart()`, `InferMessageType()` |
| Main file | ~280 | `Initialize()`, syntax detection, record types, utility methods |

### Record Types

Keep these in the main file (or extract to `.types.cs` if beneficial):
- `RouteInfo`, `ParameterInfo`, `OptionInfo`, `GroupOptionInfo`

### Utility Methods

These are used across concerns - keep in main file:
- `EscapeString()`, `MakeSafeName()`, `ToCamelCase()`
- Constant attribute names

### Reference Pattern

Follow established codebase conventions with XML documentation listing all partials.
