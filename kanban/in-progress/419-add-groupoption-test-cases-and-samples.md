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

- [ ] Create test cases for GroupOption inheritance
- [ ] Create test cases for GroupOption parsing and binding
- [ ] Create test cases for GroupOption help text generation
- [ ] Create sample demonstrating GroupOption with NuruRouteGroup
- [ ] Verify all tests pass
- [ ] Update documentation if needed

## Notes

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
