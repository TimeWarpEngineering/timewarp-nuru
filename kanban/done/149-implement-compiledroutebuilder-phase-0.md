# Implement CompiledRouteBuilder (Phase 0)

## Description

Create the internal `CompiledRouteBuilder` class that provides a fluent API for constructing `CompiledRoute` instances. This is the foundation for all subsequent phases - the builder becomes the canonical mechanism for route construction.

**Goal:** Validate that fluent builder produces identical `CompiledRoute` instances to what `PatternParser.Parse()` produces today.

## Parent

148-generate-command-and-handler-from-delegate-map-calls

## Checklist

### Implementation
- [x] Create `CompiledRouteBuilder.cs` in `source/timewarp-nuru-core/`
- [x] Keep visibility `internal` (becomes public in Phase 4)
- [x] Add `[InternalsVisibleTo("timewarp-nuru-core-tests")]` to assembly (auto-generated)
- [x] Implement `WithLiteral(string value)` - adds literal segment
- [x] Implement `WithParameter(string name, string? type, string? description)` - required positional
- [x] Implement `WithOptionalParameter(string name, string? type, string? description)` - optional positional
- [x] Implement `WithOption(...)` - flag or valued option with all parameters (extended with parameterType, parameterIsOptional)
- [x] Implement `WithCatchAll(string name, string? type, string? description)` - catch-all parameter
- [x] Implement `Build()` - returns `CompiledRoute`
- [x] Use same specificity constants as existing `Compiler`:
  - `SpecificityLiteralSegment = 100`
  - `SpecificityRequiredOption = 50`
  - `SpecificityOptionalOption = 25`
  - `SpecificityTypedParameter = 20`
  - `SpecificityUntypedParameter = 10`
  - `SpecificityOptionalParameter = 5`
  - `SpecificityCatchAll = 1`

### Testing
- [x] Create test file `compiled-route-builder-01-basic.cs` in `tests/timewarp-nuru-core-tests/`
- [x] Test: Simple literal route (`"greet"`)
- [x] Test: Literal + parameter (`"greet {name}"`)
- [x] Test: Multiple literals (`"git commit"`)
- [x] Test: Optional parameter (`"greet {name?}"`)
- [x] Test: Typed parameter (`"add {x:int} {y:int}"`)
- [x] Test: Boolean flag option (`"deploy --force"`)
- [x] Test: Option with short form (`"deploy --force,-f"`)
- [x] Test: Option with value (`"deploy --config {file}"`)
- [x] Test: Catch-all (`"exec {*args}"`)
- [x] Test: Complex route (`"deploy {env} --force,-f --config,-c {file?}"`)
- [x] Test: Typed option value (`"server --port {port:int}"`)
- [x] Test: Optional flag (`"deploy --verbose?"`)
- [x] Test: Multiple catch-all throws exception
- [x] Each test compares `PatternParser.Parse(pattern)` with equivalent builder chain

### Validation
- [x] All tests pass (13/13)
- [x] Builder produces identical `CompiledRoute.Segments` arrays
- [x] Builder produces identical `CompiledRoute.Specificity` values
- [x] Builder produces identical `CompiledRoute.CatchAllParameterName`

## Notes

### Reference Files

- **Design doc:** `kanban/in-progress/148-nuru-3-unified-route-pipeline/fluent-route-builder-design.md` (updated with extended WithOption signature)
- **Existing Compiler:** `source/timewarp-nuru-parsing/parsing/compiler/compiler.cs` (specificity constants)
- **CompiledRoute:** `source/timewarp-nuru-parsing/parsing/runtime/compiled-route.cs`
- **PatternParser:** `source/timewarp-nuru-parsing/parsing/pattern-parser.cs`
- **Matchers:** `source/timewarp-nuru-parsing/parsing/runtime/matchers/` (LiteralMatcher, ParameterMatcher, OptionMatcher)

### Key Design Decisions

1. **Location: `timewarp-nuru-core`** - The builder is runtime infrastructure, not parsing. The analyzer doesn't need it. Core is where `NuruRouteRegistry` and route registration live.
2. **Internal visibility** - Not exposed to consumers until Phase 4
3. **Parallel code paths** - Existing `Compiler` stays as-is; builder is new code
4. **Test-driven** - Tests prove equivalence with parser output
5. **No runtime behavior changes** - This is infrastructure only

### Builder API Signature (Final)

The API was consolidated for simplicity:
- `WithParameter` and `WithOptionalParameter` merged into single `WithParameter` with `isOptional` flag
- `WithOption` extended to support all option scenarios

```csharp
internal sealed class CompiledRouteBuilder
{
    public CompiledRouteBuilder WithLiteral(string value);
    public CompiledRouteBuilder WithParameter(
        string name, 
        string? type = null, 
        string? description = null,
        bool isOptional = false);           // Consolidated: replaces WithOptionalParameter
    public CompiledRouteBuilder WithOption(
        string longForm,
        string? shortForm = null,
        string? parameterName = null,
        bool expectsValue = false,
        string? parameterType = null,
        bool parameterIsOptional = false,
        string? description = null,
        bool isOptionalFlag = false,
        bool isRepeated = false);
    public CompiledRouteBuilder WithCatchAll(string name, string? type = null, string? description = null);
    public CompiledRoute Build();
}
```

### Not In Scope

- Public API exposure (Phase 4)
- Source generator integration (Phase 1-2)
- Refactoring existing `Compiler` to use builder (Phase 3)
