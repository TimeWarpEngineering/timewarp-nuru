# Refactor EndpointResolver - extract helpers and use partials to reduce file size

## Description

The `EndpointResolver` class in `source/timewarp-nuru-core/resolution/endpoint-resolver.cs` was 697 lines and difficult to navigate. Refactored into 4 partial class files to improve maintainability.

## Checklist

- [x] Analyze the file structure and identify logical groupings
- [x] Extract option matching logic to a separate partial or helper class
- [x] Extract segment validation logic to a separate partial or helper class
- [x] Extract repeated option handling to a separate partial or helper class
- [x] Consider extracting `MatchRoute` and `MatchSegments` to partials
- [x] Ensure all tests pass after refactoring
- [x] Update any documentation if needed

## Solution

Split `EndpointResolver` into 4 partial class files:

| File | Lines | Contents |
|------|-------|----------|
| `endpoint-resolver.cs` | 179 | Core: `Resolve()`, `MatchRoute()`, `SelectBestMatch()`, `RouteMatch` |
| `endpoint-resolver.segments.cs` | 233 | Segment matching: `MatchSegments()`, `ValidateSegmentAvailability()`, `MatchRegularSegment()` |
| `endpoint-resolver.options.cs` | 234 | Option handling: `MatchOptionSegment()`, `MatchRepeatedOptionWithIndices()`, `SetDefaultOptionValue()` |
| `endpoint-resolver.helpers.cs` | 78 | Utilities: `IsDefinedOption()`, `HandleCatchAllSegment()`, `LogExtractedValues()` |

Added summary documentation to main file listing all partials. All 1694 tests pass.
