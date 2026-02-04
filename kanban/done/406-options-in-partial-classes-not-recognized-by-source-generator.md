# Options in partial classes not recognized by source generator

## Description

The Nuru source generator fails to recognize `[Option]` attributes when they are defined in partial class files other than the main file containing the `[NuruRoute]` attribute. This causes a build error where the generated code references a lowercased property name that doesn't exist.

## Root Cause Analysis

**Issue:** GitHub Issue #164

**Symptoms:**
- Build error: `error CS0103: The name 'xmloutputpath' does not exist in the current context`
- Options defined in secondary partial class files are not included in generated binding code

**Root Cause:**
The source generator is only scanning properties in a single file (the one containing `[NuruRoute]`) rather than collecting properties from ALL partial class declarations that make up the type. When the generator emits binding code, it references property names from the full type, but the variable binding code was only generated for properties found in the primary file.

**Technical Details:**
1. The generator finds `[NuruRoute]` in `WorkfileImportEndpoint.cs` and starts processing
2. It scans for `[Option]` attributes in that file only
3. When it encounters `XmlOutputPath` defined in `WorkfileImportEndpoint.PathwaysXml.cs`, this file is NOT scanned
4. Generated code references `xmloutputpath` (lowercased property name) but never creates the binding variable

**Impact:**
- Users cannot organize their command classes across multiple partial files
- Forces all options to be in the same file as `[NuruRoute]`
- Breaks clean separation of concerns for large commands

## Related

- Source: GitHub issue #164
- Component: `timewarp-nuru-analyzers` (source generator)

## Checklist

- [ ] Investigate how the source generator discovers `[Option]` attributes
- [ ] Update generator to collect properties from all partial class files
- [ ] Add integration test with options in multiple partial class files
- [ ] Verify fix resolves the build error

## Notes

### Implementation Plan

**Root Cause:**
The bug is in `/source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` in the `ExtractSegmentsFromProperties` method (lines 225-246). The method uses a syntactic approach that only sees members from the single file containing `[NuruRoute]`, while `ExtractHandler` uses a semantic approach that correctly finds ALL properties including those in partial class files.

**Fix:**
Modify `ExtractSegmentsFromProperties` to use the semantic model to collect all properties from the type, similar to how `ExtractHandler` does it. Use `classSymbol.GetMembers().OfType<IPropertySymbol>()` to get ALL properties.

**Files to Modify:**
- `source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs` - Rewrite `ExtractSegmentsFromProperties` method

**Test:**
- Add test in `tests/timewarp-nuru-tests/generator/` that creates a partial class with options across multiple files

**Edge Cases:**
- Duplicate property names across partial files (Roslyn deduplicates)
- Properties without syntax references (skip)
- Default value extraction via `DeclaringSyntaxReferences`

**Validation:**
1. Build the solution
2. Run CI tests
3. Verify no regression

## Results

### What Was Implemented
Fixed bug #406 where options in partial classes were not recognized by the source generator. The issue was that `ExtractSegmentsFromProperties` used a syntactic approach (`classDeclaration.Members`) that only saw members from the single file containing `[NuruRoute]`, ignoring properties in other partial class files.

### Files Changed

1. **`source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs`**:
   - Modified `ExtractSegmentsFromProperties` to use semantic model (`classSymbol.GetMembers().OfType<IPropertySymbol>()`)
   - Added `ExtractSegmentFromPropertySymbol` method to work with `IPropertySymbol`
   - Added `ExtractPropertyDefaultValueFromSymbol` method to extract default values via `DeclaringSyntaxReferences`

2. **`tests/timewarp-nuru-tests/generator/generator-18-partial-class-options.cs`** (new):
   - 4 tests covering partial class options: basic recognition, default values, short forms, and mixed options from both files

### Key Decisions

- **Used semantic model for type resolution**: Instead of iterating `classDeclaration.Members` (syntax-only), now uses `semanticModel.GetDeclaredSymbol()` to get the type symbol, then iterates all properties via `GetMembers().OfType<IPropertySymbol>()`

- **Default value extraction via `DeclaringSyntaxReferences`**: For properties defined in partial classes, the syntax reference points to the actual source file where the property is defined, allowing correct default value extraction even when the property is in a different file from `[NuruRoute]`

### Test Results
- ✅ 4/4 new partial class tests pass
- ✅ 1065/1065 CI tests pass (no regressions)
