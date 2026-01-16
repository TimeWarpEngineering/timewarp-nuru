# Cleanup Debug Diagnostics

## Description

Remove temporary debug diagnostics and restore build settings that were added during #364 investigation.

## Files to Clean Up

### Debug Diagnostics to Remove

- `source/timewarp-nuru-analyzers/generators/extractors/app-extractor.cs`
  - NURU_DEBUG: Field assignment tracking
  - NURU_DEBUG2: ExtractFromBuildCall entry
  - NURU_DEBUG3: Interpreter result
  - NURU_DEBUG4: Block info

- `source/timewarp-nuru-analyzers/generators/nuru-generator.cs`
  - Any related debug code

### Build Settings to Revert

- `source/Directory.Build.props` - Re-enable TreatWarningsAsErrors
- `tests/Directory.Build.props` - Re-enable TreatWarningsAsErrors
- `tests/timewarp-nuru-tests/Directory.Build.props` - Re-enable TreatWarningsAsErrors

## Notes

These diagnostics were helpful for debugging cross-method field tracking (#364). Keep them available as a pattern for future debugging but remove from production code.

## Checklist

- [ ] Remove NURU_DEBUG diagnostics from app-extractor.cs
- [ ] Remove debug code from nuru-generator.cs
- [ ] Revert TreatWarningsAsErrors in source/Directory.Build.props
- [ ] Revert TreatWarningsAsErrors in tests/Directory.Build.props
- [ ] Revert TreatWarningsAsErrors in tests/timewarp-nuru-tests/Directory.Build.props
- [ ] Verify CI tests still pass after cleanup
