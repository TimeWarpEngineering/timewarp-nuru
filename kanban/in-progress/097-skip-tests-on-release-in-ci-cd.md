# Skip Tests on Release in CI/CD

## Description

The CI/CD workflow runs the full test suite 3 times for the same code: on PR, on push to master, and on release. Since the release tag points to a commit that was already tested when pushed to master, running tests again on release is wasteful.

Split the "Build and Test" step into separate "Build" and "Test" steps, with the Test step skipped on release events.

## Requirements

- Tests must still run on `pull_request` and `push` events
- Tests must be skipped on `release` events
- Build must still run on all events (to produce artifacts with correct commit SHA)

## Checklist

### Implementation
- [ ] Split "Build and Test" step into separate "Build" and "Test" steps
- [ ] Add `if: github.event_name != 'release'` condition to Test step
- [ ] Verify workflow syntax is valid

## Notes

Current workflow (lines 52-62 in `.github/workflows/ci-cd.yml`):
```yaml
- name: Build and Test
  run: |
    echo "Running build script..."
    dotnet ${{ github.workspace }}/scripts/build.cs

    echo "Running Nuru unit tests (lexer, parser, routing)..."
    dotnet ${{ github.workspace }}/tests/scripts/run-nuru-tests.cs

    echo "Running integration tests (Delegate vs Mediator)..."
    cd tests
    ./test-both-versions.sh
```

Change to:
```yaml
- name: Build
  run: |
    echo "Running build script..."
    dotnet ${{ github.workspace }}/scripts/build.cs

- name: Test
  if: github.event_name != 'release'
  run: |
    echo "Running Nuru unit tests (lexer, parser, routing)..."
    dotnet ${{ github.workspace }}/tests/scripts/run-nuru-tests.cs

    echo "Running integration tests (Delegate vs Mediator)..."
    cd tests
    ./test-both-versions.sh
```

Analysis document: `.agent/workspace/2025-12-01T00-00-00_ci-cd-redundant-test-analysis.md`
