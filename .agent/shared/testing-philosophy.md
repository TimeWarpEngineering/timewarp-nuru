# Testing Philosophy

## The Point of Tests

Tests exist to **expose bugs**. A failing test is doing its job.

## What NOT To Do

- **Never skip a failing test to make CI green** - that hides bugs
- **Never work around test failures** - fix the actual bug
- **Never delete tests because they fail** - they're telling you something is broken

## What To Do

When a test fails:

1. **Keep the test** - it documents expected behavior
2. **Create a kanban task** - track the bug it exposed
3. **Fix the bug** - in the source code, not the test

## Build Failures vs Test Failures

- **Test failure**: Code runs but assertion fails → fix the bug
- **Build failure**: Code won't compile → fix the generator/source causing bad output

Both are valuable signals. Neither should be silenced.

## The Only Valid Reasons to Skip

- Test requires infrastructure not available (external service, specific OS)
- Test is for a feature intentionally not yet implemented (mark with issue #)
- Test is flaky due to timing - but prefer fixing the flakiness
