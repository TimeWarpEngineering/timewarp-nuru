# Phase 2a: Verify Group Support with Block Interpreter

## Description

Ensure all Phase 2 group tests pass with the new block-based interpreter. This is a verification step - no new features, just confirming the refactor didn't break group support.

## Parent

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Depends On

- #283 Phase 1a: Migrate Interpreter to Block-Based Processing

## Scope

- Update all 6 Phase 2 tests in `dsl-interpreter-group-test.cs` to use new API
- Fix any issues that arise from the refactor
- No new features

## Checklist

- [ ] Update `dsl-interpreter-group-test.cs` to use `Interpret(BlockSyntax)`
- [ ] Run all 6 Phase 2 tests
- [ ] Fix any issues
- [ ] All 6 Phase 2 tests pass

## Files to Modify

| File | Change |
|------|--------|
| `tests/.../interpreter/dsl-interpreter-group-test.cs` | Update API usage |

## Success Criteria

1. All 6 Phase 2 group tests pass
2. All 10 tests total pass (4 Phase 1 + 6 Phase 2)
