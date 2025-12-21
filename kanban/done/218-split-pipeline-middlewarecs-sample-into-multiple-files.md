# Split pipeline-middleware.cs sample into multiple files

## Description

The `pipeline-middleware.cs` sample file (696 lines) demonstrates multiple middleware concepts in a single file. For better educational value and readability, it should be split into focused sample files, each demonstrating a specific middleware concept.

**Location:** `samples/pipeline-middleware/pipeline-middleware.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### Analysis
- [x] Review file to identify distinct middleware concepts demonstrated
- [x] Determine logical split points for separate samples
- [x] Ensure each split file is a complete, runnable example

### File Creation
- [x] Create focused sample files for each middleware concept
- [x] Update `samples/pipeline-middleware/overview.md` to describe each sample
- [x] Update `samples/examples.json` with new sample entries

### Verification
- [x] Each sample compiles and runs independently
- [x] Samples demonstrate concepts clearly
- [x] Documentation is updated

## Implementation Summary

Split the original 697-line file into 5 focused samples:

| File | Lines | Concept |
|------|-------|---------|
| `pipeline-middleware-basic.cs` | 198 | Logging + Performance (before/after pattern) |
| `pipeline-middleware-authorization.cs` | 195 | Authorization with IRequireAuthorization marker |
| `pipeline-middleware-retry.cs` | 205 | Retry with exponential backoff, IRetryable marker |
| `pipeline-middleware-exception.cs` | 198 | Exception handling with categorized errors |
| `pipeline-middleware-telemetry.cs` | 199 | OpenTelemetry Activity spans |

The original `pipeline-middleware.cs` was retained as a complete reference implementation.

Also fixed API compatibility issue: updated all samples to use the new fluent API:
- Old: `.Map<Command>(pattern: "...", description: "...")`
- New: `.Map<Command>("...").WithDescription("...")`

## Notes

### Potential Split Points

Based on typical middleware concepts:
1. **Basic middleware** - Simple before/after pattern
2. **Exception handling middleware** - Try/catch wrapping
3. **Logging middleware** - Request/response logging
4. **Validation middleware** - Input validation
5. **Timing middleware** - Performance measurement
6. **Authorization middleware** - Permission checking

### Sample File Convention

Follow existing samples structure:
```
samples/pipeline-middleware/
├── overview.md
├── pipeline-middleware-basic.cs
├── pipeline-middleware-logging.cs
├── pipeline-middleware-validation.cs
└── ...
```

Or consider if concepts are better combined in groups.

### Educational Value

Each sample should:
- Be self-contained and runnable
- Include comments explaining the concept
- Show best practices for that middleware type
