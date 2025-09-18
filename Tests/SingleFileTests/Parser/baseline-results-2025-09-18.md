# Parser Test Baseline Results
**Date:** 2025-09-18
**Purpose:** Establish baseline before implementing optional/repeated syntax

## Test Files Run

1. test-analyzer-patterns.cs
2. test-hanging-patterns.cs
3. test-hanging-patterns-fixed.cs
4. test-specific-hanging.cs
5. test-parser-errors.cs

## Summary

### Issues Found
1. **test-analyzer-patterns.cs**: Pattern 'build config release' incorrectly parsed (should fail)
2. **test-analyzer-patterns.cs**: Pattern 'deploy {env?} {version}' incorrectly parsed (should fail)
3. **All tests**: Had wrong project paths (fixed)

### Test Status
- ✅ test-analyzer-patterns.cs - Runs with 2 unexpected passes
- ✅ test-hanging-patterns.cs - Runs successfully
- ✅ test-hanging-patterns-fixed.cs - Runs successfully
- ✅ test-specific-hanging.cs - Runs successfully
- ✅ test-parser-errors.cs - Runs successfully

## Full Results

See `baseline-output.txt` for complete output.