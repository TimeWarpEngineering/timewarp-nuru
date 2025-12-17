# Create runfiles/test.cs to run fast CI tests

## Description

Create a simple test runner runfile that executes the fast CI test suite. No arguments - just runs `tests/ci-tests/run-ci-tests.cs` and returns its exit code.

This is a stepping stone toward a unified `dev` CLI with attributed routes (after Task 150 completes).

## Checklist

- [ ] Create `runfiles/test.cs`
- [ ] Use `TimeWarp.Amuru.Shell` to run `tests/ci-tests/run-ci-tests.cs`
- [ ] Return exit code from test runner
- [ ] Verify `dotnet runfiles/test.cs` works
- [ ] Commit changes

## Notes

Depends on: Task 182 (rename scripts to runfiles)

Future: This will be absorbed into a `dev test ci` command when the attributed routes feature (Task 150) is ready.

Current test suite: ~1700 tests in ~12 seconds (CI multi-mode).
