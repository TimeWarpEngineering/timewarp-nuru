# Flatten Namespaces to TimeWarp.Nuru

## Description

Flatten all sub-namespaces into `TimeWarp.Nuru` so users only need a single `using TimeWarp.Nuru;` to access everything when the package is included.

**Keep separate:** `TimeWarp.Nuru.Mcp` (standalone AI teaching tool)

**Flatten into `TimeWarp.Nuru`:**
- `TimeWarp.Nuru.Parsing`
- `TimeWarp.Nuru.Completion`
- `TimeWarp.Nuru.Repl`
- `TimeWarp.Nuru.Repl.Input`
- `TimeWarp.Nuru.Repl.Logging`
- `TimeWarp.Nuru.Analyzers`
- `TimeWarp.Nuru.Analyzers.Diagnostics`

## Checklist

### Preparation (Complete)
- [x] Simplify tab-completion test namespace (commit 3e35dae)

### Implementation - Incremental Migration
**Approach:** Change one namespace at a time, run full test suite after each change.

- [ ] Flatten `TimeWarp.Nuru.Parsing` → `TimeWarp.Nuru` (run tests)
- [ ] Flatten `TimeWarp.Nuru.Completion` → `TimeWarp.Nuru` (run tests)
- [ ] Flatten `TimeWarp.Nuru.Repl` → `TimeWarp.Nuru` (run tests)
- [ ] Flatten `TimeWarp.Nuru.Repl.Input` → `TimeWarp.Nuru` (run tests)
- [ ] Flatten `TimeWarp.Nuru.Repl.Logging` → `TimeWarp.Nuru` (run tests)
- [ ] Flatten `TimeWarp.Nuru.Analyzers` → `TimeWarp.Nuru` (run tests)
- [ ] Flatten `TimeWarp.Nuru.Analyzers.Diagnostics` → `TimeWarp.Nuru` (run tests)

### Cleanup
- [ ] Remove unnecessary `using` statements that reference flattened namespaces
- [ ] Update `GlobalUsings.cs` files to remove flattened namespace imports
- [ ] Update remaining test namespaces to match (e.g., `TimeWarp.Nuru.Tests`)

### Final Verification
- [ ] Verify build succeeds
- [ ] Verify all tests pass (112/113 baseline, repl-23-key-binding-profiles.cs known failure)

### Documentation
- [ ] Update any documentation referencing the old namespaces

## Notes

Decision made to simplify user experience. If a package is included, users have opted in - no reason to require multiple `using` statements. This follows the pattern of libraries like Dapper, MediatR, and FluentValidation that use flat namespaces.

MCP stays separate because it's a standalone application that teaches AI how to use Nuru, not a library consumed by user code.

### Attempt History

**Attempt 1 (stashed):** Tried to change all namespaces at once. Failed due to:
- Type conflicts (e.g., `LoggerMessages`, `TokenType` needed renaming)
- Test helper file using `new NuruAppBuilder()` which became internal

**Attempt 2 (current):** Incremental approach - one namespace at a time with test verification.

### Test Baseline
- Commit 3e35dae: 112/113 tests pass (99.1%)
- Known failure: `repl-23-key-binding-profiles.cs` (tracked in task 056)
- Run tests with: `./tests/scripts/run-all-tests.cs`
