# Standardize Testing Samples

## Description

Fix testing samples to use correct builder pattern and working project paths. Testing samples demonstrate output capture and terminal injection for unit testing CLI applications.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Fix incorrect `#:project` directive paths (case sensitivity)
- Keep `new NuruAppBuilder()` for testing scenarios (provides ITerminal injection without requiring Mediator)
- Add header comments explaining the builder choice
- Ensure all samples compile and run correctly

## Checklist

### Implementation
- [x] Fix `#:project` paths in `samples/testing/test-colored-output.cs`
- [x] Fix `#:project` paths in `samples/testing/test-terminal-injection.cs`
- [x] Fix `#:project` paths in `samples/testing/test-output-capture.cs`
- [x] Add explanatory comments about builder choice
- [x] Add ITerminal to LightweightServiceProvider for testing support
- [x] Verify all samples compile successfully
- [x] Verify all samples run correctly with expected output

## Notes

Testing samples use `new NuruAppBuilder()` which is the correct approach for testing:
- Provides ITerminal injection for testable output capture
- Does not require Mediator registration (unlike `CreateBuilder`)
- Works with `LightweightServiceProvider` which now resolves ITerminal

Changes made:
- Fixed `#:project` paths from `../../Source/TimeWarp.Nuru/` to `../../source/timewarp-nuru/`
- Added ITerminal resolution to LightweightServiceProvider
- Added header comments explaining the testing builder choice

Files updated:
- `samples/testing/test-colored-output.cs`
- `samples/testing/test-terminal-injection.cs`
- `samples/testing/test-output-capture.cs`
- `source/timewarp-nuru-core/services/lightweight-service-provider.cs`
