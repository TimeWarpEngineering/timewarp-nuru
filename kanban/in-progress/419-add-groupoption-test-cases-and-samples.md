# Add GroupOption test cases and samples

## Description

Create comprehensive test cases and sample implementations for the GroupOption feature. GroupOption allows shared options across route groups via base class properties.

## Requirements

- Create test cases in tests/timewarp-nuru-tests/ verifying GroupOption functionality
- Create sample in samples/endpoints/ demonstrating GroupOption usage
- Verify GroupOptionAttribute works with [NuruRouteGroup] base classes
- Test that options are inherited by all routes in the group
- Test help text generation for group options
- Test option parsing and binding for group-level options

## Checklist

- [x] Create test cases for GroupOption inheritance (created but failing)
- [x] Create test cases for GroupOption parsing and binding (created but failing)
- [x] Create test cases for GroupOption help text generation (created but failing)
- [x] Create sample demonstrating GroupOption with NuruRouteGroup (created)
- [ ] Wait for #420 to implement generator support
- [ ] Re-run tests after generator fix

## Notes

## BLOCKER NOTICE - Generator Implementation Missing

**This task is currently BLOCKED.** The GroupOption feature is documented and the test cases have been created, but the source generator does not yet implement GroupOption support. The tests fail because the generated code does not recognize GroupOptions on route group base classes.

**Blocker:** [#420 - Implement GroupOption support in source generator](../backlog/420-implement-groupoption-support-in-generator.md)

Once #420 is complete, re-run the tests to verify GroupOption functionality works correctly.

---

Documentation reference: See SKILL.md lines 202-213 and source/timewarp-nuru/attributes/group-option-attribute.cs

The GroupOptionAttribute allows defining shared options on base classes with [NuruRouteGroup]:

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase
{
  [GroupOption("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }
}
```

All routes inheriting from DockerGroupBase automatically get --verbose/-v option.

## Implementation Plan

### Test Cases (tests/timewarp-nuru-tests/group-options/group-options-01-basic.cs)

1. **Basic GroupOption Inheritance**
   - Test single GroupOption binding with long form
   - Test short form binding
   - Test default values when not provided
   - Test matching behavior

2. **Multiple GroupOptions**
   - Test multiple GroupOptions on same base class
   - Test combined usage with short and long forms
   - Test mixed form usage

3. **GroupOption with Route-Level Options**
   - Test GroupOptions coexist with [Option] on route class
   - Test combined usage in help text

4. **Nested Route Groups**
   - Test inheritance through nested groups
   - Test merging options from multiple ancestors

5. **Help Text Generation**
   - Test GroupOptions appear in route help
   - Test proper description display
   - Test combination with route options

6. **Typed GroupOptions**
   - Test string and numeric types
   - Test nullable typed options

### Sample Implementation (samples/endpoints/14-group-options/)

Create a Git CLI sample demonstrating:
- Base class with multiple GroupOptions (verbose, dry-run, config)
- Commands: status, commit (with route-level options), clone
- Nested group example: git remote add/remove

### Edge Cases

- GroupOption without NuruRouteGroup (analyzer warning expected)
- Conflicting option names (analyzer warning expected)
- Nullable typed options behavior

### Files to Create

| File | Purpose |
|------|---------|
| tests/timewarp-nuru-tests/group-options/group-options-01-basic.cs | Test cases |
| samples/endpoints/14-group-options/group-options.cs | Sample entry |
| samples/endpoints/14-group-options/endpoints/git-group-base.cs | Base with GroupOptions |
| samples/endpoints/14-group-options/endpoints/status-command.cs | Simple command |
| samples/endpoints/14-group-options/endpoints/commit-command.cs | Command with route options |
