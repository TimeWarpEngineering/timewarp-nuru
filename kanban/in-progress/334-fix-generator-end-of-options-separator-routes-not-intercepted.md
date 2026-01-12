# Fix generator end-of-options separator routes not intercepted

## Summary

Routes containing the end-of-options separator (`--`) are not being intercepted by the generated code. The `RunAsync` call falls through to the non-intercepted path, causing "RunAsync was not intercepted" errors.

## Background

Discovered during task #332 when refactoring `routing-08-end-of-options.cs` tests to use TestTerminal pattern.

**Route pattern:** `git checkout -- {file}`
**Input:** `git checkout -- README.md`
**Expected:** Route matches and handler executes
**Actual:** "RunAsync was not intercepted" - generator doesn't recognize the route

## Root Cause Analysis

The `--` end-of-options separator is being parsed as a `LiteralSyntax` and converted to a `LiteralDefinition`. This is semantically incorrect:

1. **Parser:** `ParseEndOfOptions()` in `parser.segments.cs` returns `LiteralSyntax("--")`
2. **Extractor:** Converts to `LiteralDefinition(position, "--")`
3. **Emitter:** `EmitPositionalArrayConstruction` unconditionally skips `--` from positional args
4. **Result:** Route expects `--` as a literal to match, but it's been removed from the positional array

**Fix:** Create a semantically distinct `EndOfOptionsSeparatorDefinition` segment type so the emitter knows when a route explicitly includes `--` vs when `--` is just a runtime separator.

## Checklist

### Phase 1: Parsing Layer (timewarp-nuru-parsing)
- [ ] Create `EndOfOptionsSyntax` syntax type
- [ ] Update `ISyntaxVisitor<T>` interface with `VisitEndOfOptions` method
- [ ] Update `SyntaxVisitor<T>` base class with abstract method and dispatch
- [ ] Update `ParseEndOfOptions()` to return `EndOfOptionsSyntax`
- [ ] Update `Compiler` with `VisitEndOfOptions` method
- [ ] Update `SemanticValidator` to check for `EndOfOptionsSyntax` instead of literal `"--"`

### Phase 2: Generator Layer (timewarp-nuru-analyzers)
- [ ] Create `EndOfOptionsSeparatorDefinition` segment type
- [ ] Update `PatternStringExtractor.ConvertSyntaxSegment` to handle `EndOfOptionsSyntax`
- [ ] Update `RouteDefinition` with `HasEndOfOptions` property and `EffectivePattern` handling
- [ ] Update `RouteMatcherEmitter.EmitPositionalArrayConstruction` to conditionally skip `--`
- [ ] Update `HelpEmitter.BuildPatternDisplay` to handle new segment type
- [ ] Update `OverlapValidator` signature computation for new segment type

### Phase 3: Verification
- [ ] Run CI tests - verify `Should_match_docker_style_command` passes
- [ ] Verify `routing-08-end-of-options.cs` tests pass
- [ ] Verify no regressions in other tests

## Test Files

- `tests/timewarp-nuru-core-tests/routing/routing-08-end-of-options.cs`
- `tests/timewarp-nuru-core-tests/routing/routing-09-complex-integration.cs` (docker test)
- `tests/timewarp-nuru-core-tests/parser/parser-09-end-of-options.cs`

## Notes

- The `--` separator is a POSIX convention meaning "end of options, everything after is a positional argument"
- Common usage: `git checkout -- file.txt` (the `--` prevents `file.txt` from being interpreted as a branch name)
- Related to V2 Generator epic (#265)
- `EndOfOptionsSeparatorDefinition.SpecificityContribution` should be `0` (structural marker, not matchable)
- For overlap detection, `--` should be treated as a distinguishing element (signature includes it)

## Files to Modify

### New Files
- `source/timewarp-nuru-parsing/parsing/syntax/end-of-options-syntax.cs`

### Modified Files
- `source/timewarp-nuru-parsing/parsing/syntax/isyntax-visitor.cs`
- `source/timewarp-nuru-parsing/parsing/syntax/syntax-visitor.cs`
- `source/timewarp-nuru-parsing/parsing/parser/parser.segments.cs`
- `source/timewarp-nuru-parsing/parsing/compiler/compiler.cs`
- `source/timewarp-nuru-parsing/parsing/validation/semantic-validator.cs`
- `source/timewarp-nuru-analyzers/generators/models/segment-definition.cs`
- `source/timewarp-nuru-analyzers/generators/extractors/pattern-string-extractor.cs`
- `source/timewarp-nuru-analyzers/generators/models/route-definition.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/route-matcher-emitter.cs`
- `source/timewarp-nuru-analyzers/generators/emitters/help-emitter.cs`
- `source/timewarp-nuru-analyzers/validation/overlap-validator.cs`
