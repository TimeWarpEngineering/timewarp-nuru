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
