# Split help-provider.cs into partial files

## Description

The `help-provider.cs` file (643 lines) is a static class handling help text generation with multiple concerns: route filtering, pattern formatting, and ANSI color formatting. These should be split into focused partial files.

**Location:** `source/timewarp-nuru-core/help/help-provider.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### File Creation
- [x] Create `help-provider.filtering.cs` - Route filtering and grouping
- [x] Create `help-provider.formatting.cs` - Pattern formatting logic
- [x] Create `help-provider.ansi.cs` - ANSI color formatting helpers

### Documentation
- [x] Add `<remarks>` to main file listing all partial files
- [x] Add XML summary to each new partial file

### Verification
- [x] All tests pass
- [x] Build succeeds

## Notes

### Proposed Split

| New File | ~Lines | Content |
|----------|--------|---------|
| `.filtering.cs` | ~100 | `FilterRoutes()`, `ShouldFilter()`, `MatchesWildcard()`, `GroupByDescription()`, `AppendGroup()`, `EndpointGroup` class |
| `.formatting.cs` | ~280 | `FormatCommandPattern()`, `FormatPlainPattern()`, complex pattern conversion logic |
| `.ansi.cs` | ~90 | Section headers, usage formatting, description formatting, message type indicators |
| Main file | ~170 | `FormatHelpText()` orchestration, core entry points |

### Current Structure

```
help-provider.cs (643 lines)
├── Core formatting (FormatHelpText, orchestration)
├── Route filtering (FilterRoutes, ShouldFilter, MatchesWildcard)
├── Endpoint grouping (GroupByDescription, EndpointGroup)
├── Pattern formatting (FormatCommandPattern, FormatPlainPattern) ← Largest section
├── #region Formatting Helpers (line 440)
└── EndpointGroup nested class
```

### Pattern Formatting Complexity

The pattern formatting section (~280 lines) converts internal `{x}` syntax to display `<x>` or `[x]` syntax with ANSI colors. This is the most complex and self-contained section.

### Reference Pattern

Follow established partial class conventions with XML documentation.

## Implementation Summary

Split the 643-line `help-provider.cs` into 4 partial files:

| File | Lines | Content |
|------|-------|---------|
| `help-provider.cs` | 87 | Core `GetHelpText` orchestration with `<remarks>` documentation |
| `help-provider.filtering.cs` | 133 | `FilterRoutes`, `ShouldFilter`, `MatchesWildcard`, `GroupByDescription`, `EndpointGroup` class |
| `help-provider.formatting.cs` | 276 | `AppendGroup`, `FormatCommandPattern`, `FormatPlainPattern` |
| `help-provider.ansi.cs` | 107 | `GetDefaultAppName`, `FormatSectionHeader`, `FormatUsage`, `FormatDescription`, `FormatDefaultMarker`, `FormatMessageTypeIndicator`, `FormatMessageTypeLegend` |

All 1694 tests pass. Build succeeds with no errors.
