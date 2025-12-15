# Implement CompiledRouteBuilder (Phase 0)

## Description

Create the internal `CompiledRouteBuilder` class that provides a fluent API for constructing `CompiledRoute` instances. This is the foundation for all subsequent phases - the builder becomes the canonical mechanism for route construction.

**Goal:** Validate that fluent builder produces identical `CompiledRoute` instances to what `PatternParser.Parse()` produces today.

## Parent

148-generate-command-and-handler-from-delegate-map-calls

## Checklist

### Implementation
- [ ] Create `CompiledRouteBuilder.cs` in `source/timewarp-nuru-core/`
- [ ] Keep visibility `internal` (becomes public in Phase 4)
- [ ] Add `[InternalsVisibleTo("timewarp-nuru-core-tests")]` to assembly
- [ ] Implement `WithLiteral(string value)` - adds literal segment
- [ ] Implement `WithParameter(string name, string? type, string? description)` - required positional
- [ ] Implement `WithOptionalParameter(string name, string? type, string? description)` - optional positional
- [ ] Implement `WithOption(...)` - flag or valued option with all parameters
- [ ] Implement `WithCatchAll(string name, string? type, string? description)` - catch-all parameter
- [ ] Implement `Build()` - returns `CompiledRoute`
- [ ] Use same specificity constants as existing `Compiler`:
  - `SpecificityLiteralSegment = 100`
  - `SpecificityRequiredOption = 50`
  - `SpecificityOptionalOption = 25`
  - `SpecificityTypedParameter = 20`
  - `SpecificityUntypedParameter = 10`
  - `SpecificityOptionalParameter = 5`
  - `SpecificityCatchAll = 1`

### Testing
- [ ] Create test file `compiled-route-builder-tests.cs` in `tests/timewarp-nuru-core-tests/`
- [ ] Test: Simple literal route (`"greet"`)
- [ ] Test: Literal + parameter (`"greet {name}"`)
- [ ] Test: Multiple literals (`"git commit"`)
- [ ] Test: Optional parameter (`"greet {name?}"`)
- [ ] Test: Typed parameter (`"add {x:int} {y:int}"`)
- [ ] Test: Boolean flag option (`"deploy --force"`)
- [ ] Test: Option with short form (`"deploy --force,-f"`)
- [ ] Test: Option with value (`"deploy --config {file}"`)
- [ ] Test: Catch-all (`"exec {*args}"`)
- [ ] Test: Complex route (`"deploy {env} --force,-f --config,-c {file?}"`)
- [ ] Each test compares `PatternParser.Parse(pattern)` with equivalent builder chain

### Validation
- [ ] All tests pass
- [ ] Builder produces identical `CompiledRoute.Segments` arrays
- [ ] Builder produces identical `CompiledRoute.Specificity` values
- [ ] Builder produces identical `CompiledRoute.CatchAllParameterName`

## Notes

### Reference Files

- **Design doc:** `kanban/to-do/148-generate-command-and-handler-from-delegate-map-calls/fluent-route-builder-design.md` (lines 286-446)
- **Existing Compiler:** `source/timewarp-nuru-parsing/parsing/compiler/compiler.cs` (specificity constants)
- **CompiledRoute:** `source/timewarp-nuru-parsing/parsing/runtime/compiled-route.cs`
- **PatternParser:** `source/timewarp-nuru-parsing/parsing/pattern-parser.cs`
- **Matchers:** `source/timewarp-nuru-parsing/parsing/runtime/` (LiteralMatcher, ParameterMatcher, OptionMatcher)

### Key Design Decisions

1. **Location: `timewarp-nuru-core`** - The builder is runtime infrastructure, not parsing. The analyzer doesn't need it. Core is where `NuruRouteRegistry` and route registration live.
2. **Internal visibility** - Not exposed to consumers until Phase 4
3. **Parallel code paths** - Existing `Compiler` stays as-is; builder is new code
4. **Test-driven** - Tests prove equivalence with parser output
5. **No runtime behavior changes** - This is infrastructure only

### Builder API Signature (from design doc)

```csharp
internal class CompiledRouteBuilder
{
    public CompiledRouteBuilder WithLiteral(string value);
    public CompiledRouteBuilder WithParameter(string name, string? type = null, string? description = null);
    public CompiledRouteBuilder WithOptionalParameter(string name, string? type = null, string? description = null);
    public CompiledRouteBuilder WithOption(
        string longForm,
        string? shortForm = null,
        string? parameterName = null,
        bool expectsValue = false,
        string? description = null,
        bool isOptional = true,
        bool isRepeated = false);
    public CompiledRouteBuilder WithCatchAll(string name, string? type = null, string? description = null);
    public CompiledRoute Build();
}
```

### Not In Scope

- Public API exposure (Phase 4)
- Source generator integration (Phase 1-2)
- Refactoring existing `Compiler` to use builder (Phase 3)
