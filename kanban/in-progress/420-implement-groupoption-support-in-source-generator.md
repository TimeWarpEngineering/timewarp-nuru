# Implement GroupOption support in source generator

## Description

The GroupOption feature is documented in SKILL.md and the GroupOptionAttribute exists in the core library, but the source generator does not extract or emit code for GroupOptions.

## Checklist

- [ ] Extend endpoint-extractor.cs to walk base class hierarchy (similar to ExtractGroupPrefix)
- [ ] Find properties with [GroupOption] attributes on base classes
- [ ] Convert GroupOption properties to OptionDefinition segments
- [ ] Ensure generated code includes GroupOptions in option matching
- [ ] Bind GroupOption values to handler parameters or command properties
- [ ] Generate help text including GroupOptions
- [ ] Run existing test cases (tests/timewarp-nuru-tests/group-options/group-options-01-basic.cs - 18 test cases)
- [ ] Verify behavior against sample implementation (samples/endpoints/14-group-options/)

## Notes

**Files to modify:**
- source/timewarp-nuru-analyzers/generators/extractors/endpoint-extractor.cs (main change)
- May need updates to emitters for help text and option binding

**Priority:** High - feature is documented but non-functional

**References:**
- SKILL.md documentation for GroupOption feature
- GroupOptionAttribute in core library
- Test file: tests/timewarp-nuru-tests/group-options/group-options-01-basic.cs
- Sample: samples/endpoints/14-group-options/
