# Baseline Test Run and V2 Gap Analysis

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Run test suite with both V1 and V2 to identify what V2 needs to generate and which tests are V1-specific.

## Checklist

- [ ] Run `dotnet runfiles/test.cs` with V1 (baseline - all should pass)
- [ ] Run `dotnet runfiles/test.cs` with `UseNewGen=true`
- [ ] Document which tests fail and why
- [ ] Categorize failures:
  - Missing generated code (V2 needs to emit this)
  - V1-specific tests (candidates for deletion/update)
  - Behavior changes (needs investigation)
- [ ] Create summary report of test compatibility
