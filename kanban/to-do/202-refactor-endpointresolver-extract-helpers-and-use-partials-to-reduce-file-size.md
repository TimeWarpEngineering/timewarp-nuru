# Refactor EndpointResolver - extract helpers and use partials to reduce file size

## Description

The `EndpointResolver` class in `source/timewarp-nuru-core/resolution/endpoint-resolver.cs` is 754 lines and difficult to navigate. Refactor to improve maintainability.

## Checklist

- [ ] Analyze the file structure and identify logical groupings
- [ ] Extract option matching logic to a separate partial or helper class
- [ ] Extract segment validation logic to a separate partial or helper class
- [ ] Extract repeated option handling to a separate partial or helper class
- [ ] Consider extracting `MatchRoute` and `MatchSegments` to partials
- [ ] Ensure all tests pass after refactoring
- [ ] Update any documentation if needed

## Notes

File location: `source/timewarp-nuru-core/resolution/endpoint-resolver.cs`

Current structure includes:
- `Resolve()` - main entry point
- `MatchRoute()` - iterates endpoints and finds best match
- `MatchSegments()` - matches all segments in a route
- `MatchOptionSegment()` - handles option flags
- `MatchRepeatedOptionWithIndices()` - handles repeated options
- `MatchRegularSegment()` - matches literals and parameters
- `HandleCatchAllSegment()` - handles catch-all parameters
- `ValidateSegmentAvailability()` - validates arg availability
- `CheckRequiredOptions()` - validates required options
- `SetDefaultOptionValue()` - sets defaults for optional options
- `IsDefinedOption()` - checks if arg is a defined option
- `IsOptionalParameter()` - checks if parameter is optional
- `LogExtractedValues()` - logging helper

Consider grouping:
1. **Main resolution** - `Resolve`, `MatchRoute`
2. **Segment matching** - `MatchSegments`, `MatchRegularSegment`, `ValidateSegmentAvailability`
3. **Option handling** - `MatchOptionSegment`, `MatchRepeatedOptionWithIndices`, `CheckRequiredOptions`, `SetDefaultOptionValue`
4. **Helpers** - `IsDefinedOption`, `IsOptionalParameter`, `HandleCatchAllSegment`, `LogExtractedValues`
