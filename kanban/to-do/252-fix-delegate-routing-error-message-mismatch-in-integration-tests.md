# Fix delegate routing error message mismatch in integration tests

## Description

The delegate-based routing integration test for invalid integer conversion fails because the error message format changed.

**Test:** Invalid integer  
**Command:** `git log --max-count abc`  
**Expected:** `Cannot convert 'abc' to type System.Int32`  
**Actual:** `Error: The input string 'abc' was not in a correct format.`

This affects both JIT and AOT delegate tests. Mediator-based routing passes (44/44).

## Checklist

- [ ] Investigate where the error message is generated in delegate routing
- [ ] Determine if the expected message or actual message is correct
- [ ] Update either the error message generation or the test expectation
- [ ] Verify both JIT and AOT delegate tests pass

## Notes

Discovered during PR #128 CI run. The test file is likely in `tests/test-both-versions.sh` or related test app.
