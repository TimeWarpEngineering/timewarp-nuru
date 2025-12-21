# Split pipeline-middleware.cs sample into multiple files

## Description

The `pipeline-middleware.cs` sample file (696 lines) demonstrates multiple middleware concepts in a single file. For better educational value and readability, it should be split into focused sample files, each demonstrating a specific middleware concept.

**Location:** `samples/pipeline-middleware/pipeline-middleware.cs`

## Parent

204-review-large-files-for-refactoring-opportunities

## Checklist

### Analysis
- [ ] Review file to identify distinct middleware concepts demonstrated
- [ ] Determine logical split points for separate samples
- [ ] Ensure each split file is a complete, runnable example

### File Creation
- [ ] Create focused sample files for each middleware concept
- [ ] Update `samples/pipeline-middleware/overview.md` to describe each sample
- [ ] Update `samples/examples.json` with new sample entries

### Verification
- [ ] Each sample compiles and runs independently
- [ ] Samples demonstrate concepts clearly
- [ ] Documentation is updated

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
