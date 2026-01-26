# Fix source generator incorrectly errors on missing optional int options

## Description

Fix bug in Nuru source generator where commands with optional `int` options cause routing failures instead of skipping to next route candidate when option not provided. This prevents ANY command from working if there's a prior route with optional int option.

GitHub Issue: #152

## Checklist

- [ ] Analyze current generated routing code for optional int options
- [ ] Identify the root cause in source generator logic
- [ ] Fix the condition to only validate when flag was actually found
- [ ] Create test case to reproduce the issue
- [ ] Verify fix doesn't break existing functionality
- [ ] Test with various scenarios: optional int, missing vs provided values
- [ ] Ensure route skipping works correctly for all affected patterns

## Notes

## Implementation Plan

### Primary Bug Location
- File: `source/timewarp-nuru-analyzers/SourceGenerators/RouteMatcherEmitter.cs`
- Lines 161-166: Type validation logic incorrectly errors on null values for optional options
- Impact: All built-in numeric types (int, long, double, decimal, etc.), Uri, custom converters, enum converters

### Root Cause Analysis
The type validation logic doesn't distinguish between:
- "Option not provided" → should skip route
- "Option provided but invalid" → should error (or skip for better UX)

Current code checks `rawVarName is null` and returns error, but for optional options this should trigger route skipping instead.

### Fix Strategy
1. **Remove null checks from validation conditions** for optional options
2. **Rely on existing logic** (lines 470-482) to handle route skipping when flag not found
3. **Apply fix to all affected type families**:
   - Built-in numeric types
   - Uri type
   - Custom type converters
   - Enum converters

### Implementation Steps
1. **Locate validation patterns** in `RouteMatcherEmitter.cs`
2. **Remove `|| rawVarName is null` conditions** from optional option validations
3. **Ensure consistency** across all type families
4. **Test comprehensive scenarios** to verify fix works

### Testing Strategy
1. **Create reproduction test case** with:
   - Command with optional int option
   - Command with similar route prefix
   - Verify second command executes when first's option is missing
2. **Test validation scenarios**:
   - Missing optional option → route should skip
   - Invalid int value → route should skip (better UX than error)
   - Valid int value → route should execute
3. **Regression testing**:
   - Required options still error appropriately
   - Existing functionality unchanged

### Risk Mitigation
- Minimal code changes (remove conditions, don't add new logic)
- Preserve existing behavior for required options
- Comprehensive test coverage before and after fix

### Files to Modify
- `source/timewarp-nuru-analyzers/SourceGenerators/RouteMatcherEmitter.cs`

### Expected Outcome
Commands with optional options will correctly skip to next route candidate when options not provided, allowing normal CLI functionality without breaking existing behavior.

### Affected Repository
- Repository: `TimeWarpEngineering/timewarp-ganda`
- Generated File: `artifacts/generated/timewarp-ganda/TimeWarp.Nuru.Analyzers/TimeWarp.Nuru.Generators.NuruGenerator/NuruGenerated.g.cs`

### Example Scenario
- `WorkspaceCommitsCommand` has optional `--days` int option
- `RepoSetupCommand` has route `repo setup`
- Running `ganda repo setup --dry-run` fails with error about `--days` instead of executing repo setup

### Impact
This bug breaks routing for any command following a route with optional int options, making the CLI unusable in affected scenarios.
